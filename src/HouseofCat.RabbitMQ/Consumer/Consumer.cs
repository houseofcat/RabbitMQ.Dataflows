using HouseofCat.Dataflows;
using HouseofCat.Logger;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ
{
    public interface IConsumer<TFromQueue>
    {
        RabbitOptions Options { get; }
        IConnectionPool ConnectionPool { get; }
        ConsumerOptions ConsumerOptions { get; }
        bool Started { get; }

        ChannelReader<TFromQueue> GetConsumerBuffer();
        Task StartConsumerAsync(CancellationToken cancellationToken = default);
        Task StopConsumerAsync(bool immediate = false);
        ValueTask<TFromQueue> ReadAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<TFromQueue>> ReadUntilEmptyAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<TFromQueue> StreamOutUntilEmptyAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<TFromQueue> StreamOutUntilClosedAsync();

        Task DataflowExecutionEngineAsync(
            Func<TFromQueue, Task<bool>> workBodyAsync,
            int maxDoP = 4,
            bool ensureOrdered = true,
            int boundedCapacity = 1000,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default);

        Task DataflowExecutionEngineAsync(
            Func<TFromQueue, Task<bool>> workBodyAsync,
            int maxDoP = 4,
            bool ensureOrdered = true,
            Func<TFromQueue, Task<TFromQueue>> preWorkBodyAsync = null,
            Func<bool, Task> postWorkBodyAsync = null,
            int boundedCapacity = 1000,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default);

        Task ChannelExecutionEngineAsync(
            Func<ReceivedData, Task<bool>> workBodyAsync,
            int maxDoP = 4,
            bool ensureOrdered = true,
            Func<bool, Task> postWorkBodyAsync = null,
            int boundedCapacity = 1000,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default);

        Task DirectChannelExecutionEngineAsync(
            Func<ReceivedData, Task<bool>> workBodyAsync,
            int maxDoP = 4,
            bool ensureOrdered = true,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default);
    }

    public class Consumer : IConsumer<ReceivedData>, IDisposable
    {
        public RabbitOptions Options { get; }
        public IConnectionPool ConnectionPool { get; }
        public ConsumerOptions ConsumerOptions { get; }
        public bool Started { get; private set; }

        private readonly ILogger<Consumer> _logger;
        private readonly SemaphoreSlim _conLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _executionLock = new SemaphoreSlim(1, 1);

        private IModel _channel;
        private Channel<ReceivedData> _consumerChannel;
        private bool _disposedValue;

        public Consumer(RabbitOptions options, string consumerName)
            : this(new ConnectionPool(options), consumerName)
        { }

        public Consumer(IConnectionPool connectionPool, string consumerName)
            : this(
                  connectionPool,
                  connectionPool.Options.GetConsumerOptions(consumerName))
        {
            Guard.AgainstNull(connectionPool, nameof(connectionPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));
        }

        public Consumer(IConnectionPool connectionPool, ConsumerOptions consumerOptions)
        {
            Guard.AgainstNull(connectionPool, nameof(connectionPool));
            Guard.AgainstNull(consumerOptions, nameof(consumerOptions));

            _logger = LogHelper.GetLogger<Consumer>();
            Options = connectionPool.Options;
            ConnectionPool = connectionPool;
            ConsumerOptions = consumerOptions;
        }

        // Add cancellation token
        public async Task StartConsumerAsync(CancellationToken cancellationToken = default)
        {
            if (!ConsumerOptions.Enabled) return;

            try
            {
                await _conLock.WaitAsync().ConfigureAwait(false);

                if (Started) return;

                // Always use a transient channel for consumers - its long running anyway
                // so needlessly depleting a pool impl
                var autoAck = ConsumerOptions.AutoAck ?? false;
                _channel = await ConnectionPool
                    .GetTransientChannelAsync(!autoAck, cancellationToken);

                // If we get a channel we are considered started
                Started = true;

                // Channels and consumers should recover, so should never
                // have to loop and create consumers/channels on connection errors
                _consumerChannel = Channel.CreateBounded<ReceivedData>(
                    new BoundedChannelOptions(ConsumerOptions.BatchSize!.Value)
                    {
                        FullMode = ConsumerOptions.BehaviorWhenFull!.Value
                    });

                if (Options.FactoryOptions.EnableDispatchConsumersAsync)
                {
                    _channel.BasicQos(0, ConsumerOptions.BatchSize!.Value, false);
                    var consumer = new AsyncEventingBasicConsumer(_channel);
                    consumer.Received += ReceiveHandlerAsync;
                    consumer.Shutdown += ConsumerShutdownAsync;

                    BasicConsume(consumer);
                }
                else
                {
                    _channel.BasicQos(0, ConsumerOptions.BatchSize!.Value, false);
                    var consumer = new EventingBasicConsumer(_channel);
                    consumer.Received += ReceiveHandler;
                    consumer.Shutdown += ConsumerShutdown;

                    BasicConsume(consumer);
                }

                _logger.LogInformation(
                    LogMessages.Consumers.StartedConsumer,
                    ConsumerOptions.ConsumerName);

                _logger.LogDebug(LogMessages.Consumers.Started, ConsumerOptions.ConsumerName);    
            }
            finally
            {
                _conLock.Release();
            }
        }

        private void BasicConsume(IBasicConsumer consumer)
        {
            _channel.BasicConsume(
                ConsumerOptions.QueueName,
                ConsumerOptions.AutoAck ?? false,
                ConsumerOptions.ConsumerName,
                ConsumerOptions.NoLocal ?? false,
                ConsumerOptions.Exclusive ?? false,
                null,
                consumer);
        }

        protected virtual async Task ReceiveHandlerAsync(object _, BasicDeliverEventArgs bdea)
        {
            _logger.LogDebug(
                LogMessages.Consumers.ConsumerAsyncMessageReceived,
                ConsumerOptions.ConsumerName,
                bdea.DeliveryTag);

            await HandleMessage(bdea).ConfigureAwait(false);
        }

        protected virtual async void ReceiveHandler(object _, BasicDeliverEventArgs bdea)
        {
            _logger.LogDebug(
                LogMessages.Consumers.ConsumerMessageReceived,
                ConsumerOptions.ConsumerName,
                bdea.DeliveryTag);

            await HandleMessage(bdea).ConfigureAwait(false);
        }

        protected async ValueTask<bool> HandleMessage(BasicDeliverEventArgs bdea)
        {
            try
            {
                await _consumerChannel
                    .Writer
                    .WriteAsync(new ReceivedData(_channel, bdea, !(ConsumerOptions.AutoAck ?? false)))
                    .ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumers.ConsumerMessageWriteToBufferError,
                    ConsumerOptions.ConsumerName,
                    ex.Message);
                return false;
            }
        }

        // Remove Unexpected Shutdown, Consumers should recover
        private async Task ConsumerShutdownAsync(object sender, ShutdownEventArgs e)
        {
            _logger.LogWarning(
                LogMessages.Consumers.ConsumerShutdownEvent,
                ConsumerOptions.ConsumerName);
        }

        private async void ConsumerShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogWarning(
                LogMessages.Consumers.ConsumerShutdownEvent,
                ConsumerOptions.ConsumerName);
        }

        public async Task StopConsumerAsync(bool immediate = false)
        {
            if (!ConsumerOptions.Enabled) return;

            try
            {
                await _conLock.WaitAsync().ConfigureAwait(false);

                if (!Started) return;

                Started = false;
                _consumerChannel.Writer.TryComplete();
                _channel.Dispose();
                _channel = null;
                
                _logger.LogDebug(LogMessages.Consumers.StopConsumer, ConsumerOptions.ConsumerName);

                if (immediate) return;

                // FIXME This creates a race condition if we try to start consuming again immediately
                await _consumerChannel
                    .Reader
                    .Completion
                    .ConfigureAwait(false);
            }
            finally { _conLock.Release(); }
        }

        public ChannelReader<ReceivedData> GetConsumerBuffer() => _consumerChannel.Reader;

        public async ValueTask<ReceivedData> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _consumerChannel
                    .Reader
                    .ReadAsync(cancellationToken)
                    .ConfigureAwait(false);;
            }
            catch
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }
        }

        // What is the intent here? do we want to wait, then why wait and then receive
        // only the dropped list? chances are you wont get a list. if you want a list,
        // just use TryRead till its false
        public async Task<IEnumerable<ReceivedData>> ReadUntilEmptyAsync(
            CancellationToken cancellationToken = default)
        {
            var list = new List<ReceivedData>();
            if (await _consumerChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_consumerChannel.Reader.TryRead(out var message))
                {
                    if (message == null) { continue; }
                    list.Add(message);
                }
            }

            return list;
        }

        // What is the intent here? do we want to wait, then why wait and then receive
        // only the dropped list? chances are you wont get a list. if you want a list,
        // just use TryRead till its false
        public async IAsyncEnumerable<ReceivedData> StreamOutUntilEmptyAsync(
            CancellationToken cancellationToken = default)
        {
            if (await _consumerChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_consumerChannel.Reader.TryRead(out var message))
                {
                    if (message == null) { continue; }
                    yield return message;
                }
            }
        }

        public async IAsyncEnumerable<ReceivedData> StreamOutUntilClosedAsync()
        {
            await foreach (var receivedData in _consumerChannel.Reader.ReadAllAsync())
            {
                yield return receivedData;
            }
        }

        public async Task DataflowExecutionEngineAsync(
            Func<ReceivedData, Task<bool>> workBodyAsync,
            int maxDoP = 4,
            bool ensureOrdered = true,
            int boundedCapacity = 1000,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default)
        {
            var dataflowEngine = new DataflowEngine<ReceivedData, bool>(workBodyAsync, maxDoP, ensureOrdered, null, null, boundedCapacity, taskScheduler);

            await TransferDataToDataflowEngine(dataflowEngine, token);
        }

        public async Task DataflowExecutionEngineAsync(
            Func<ReceivedData, Task<bool>> workBodyAsync,
            int maxDoP = 4,
            bool ensureOrdered = true,
            Func<ReceivedData, Task<ReceivedData>> preWorkBodyAsync = null,
            Func<bool, Task> postWorkBodyAsync = null,
            int boundedCapacity = 1000,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default)
        {
            var dataflowEngine = new DataflowEngine<ReceivedData, bool>(
                workBodyAsync,
                maxDoP,
                ensureOrdered,
                preWorkBodyAsync,
                postWorkBodyAsync,
                boundedCapacity,
                taskScheduler);

            await TransferDataToDataflowEngine(dataflowEngine, token);
        }

        private async Task TransferDataToDataflowEngine(
            DataflowEngine<ReceivedData, bool> dataflowEngine,
            CancellationToken token = default)
        {
            await _executionLock.WaitAsync(2000, token).ConfigureAwait(false);

            try
            {
                await foreach (var receivedData in _consumerChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    if (receivedData == null) continue;

                    _logger.LogDebug(
                        LogMessages.Consumers.ConsumerDataflowQueueing,
                        ConsumerOptions.ConsumerName,
                        receivedData.DeliveryTag);

                    await dataflowEngine
                        .EnqueueWorkAsync(receivedData)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    LogMessages.Consumers.ConsumerDataflowActionCancelled,
                    ConsumerOptions.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumers.ConsumerDataflowError,
                    ConsumerOptions.ConsumerName,
                    ex.Message);
            }
            finally { _executionLock.Release(); }
        }

        public async Task ChannelExecutionEngineAsync(
            Func<ReceivedData, Task<bool>> workBodyAsync,
            int maxDoP = 4,
            bool ensureOrdered = true,
            Func<bool, Task> postWorkBodyAsync = null,
            int boundedCapacity = 1000,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default)
        {
            var channelBlockEngine = new ChannelBlockEngine<ReceivedData, bool>(
                workBodyAsync,
                maxDoP,
                ensureOrdered,
                postWorkBodyAsync,
                boundedCapacity,
                taskScheduler,
                token);

            await TransferDataToChannelBlockEngine(channelBlockEngine, token);
        }

        public async Task DirectChannelExecutionEngineAsync(
            Func<ReceivedData, Task<bool>> workBodyAsync,
            int maxDoP = 4,
            bool ensureOrdered = true,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default)
        {
            _ = new ChannelBlockEngine<ReceivedData, bool>(
                _consumerChannel, workBodyAsync, maxDoP, ensureOrdered, taskScheduler, token);

            await _executionLock.WaitAsync(2000, token).ConfigureAwait(false);

            try
            {
                while (await _consumerChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    await Task.Delay(4, token); // sleep until channel is finished.
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    LogMessages.Consumers.ConsumerDataflowActionCancelled,
                    ConsumerOptions.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumers.ConsumerDataflowError,
                    ConsumerOptions.ConsumerName,
                    ex.Message);
            }
            finally { _executionLock.Release(); }
        }

        private async Task TransferDataToChannelBlockEngine(
            ChannelBlockEngine<ReceivedData, bool> channelBlockEngine,
            CancellationToken token = default)
        {
            await _executionLock.WaitAsync(2000, token).ConfigureAwait(false);

            try
            {
                await foreach (var receivedData in _consumerChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    if (receivedData == null) continue;

                    _logger.LogDebug(
                            LogMessages.Consumers.ConsumerDataflowQueueing,
                            ConsumerOptions.ConsumerName,
                            receivedData.DeliveryTag);

                    await channelBlockEngine
                        .EnqueueWorkAsync(receivedData)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    LogMessages.Consumers.ConsumerDataflowActionCancelled,
                    ConsumerOptions.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumers.ConsumerDataflowError,
                    ConsumerOptions.ConsumerName,
                    ex.Message);
            }
            finally { _executionLock.Release(); }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _executionLock.Dispose();
                    _conLock.Dispose();
                }

                _consumerChannel = null;
                _channel = null;
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
