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
        IChannelPool ChannelPool { get; }
        RabbitOptions Options { get; }
        ConsumerOptions ConsumerOptions { get; }
        bool Started { get; }

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

        public Task ChannelExecutionEngineAsync(
            Func<ReceivedData, Task<bool>> workBodyAsync,
            int maxDoP = 4,
            bool ensureOrdered = true,
            Func<bool, Task> postWorkBodyAsync = null,
            int boundedCapacity = 1000,
            TaskScheduler taskScheduler = null,
            CancellationToken token = default);

        ChannelReader<TFromQueue> GetConsumerBuffer();
        ValueTask<TFromQueue> ReadAsync();
        Task<IEnumerable<TFromQueue>> ReadUntilEmptyAsync();
        Task StartConsumerAsync();
        Task StopConsumerAsync(bool immediate = false);
        IAsyncEnumerable<TFromQueue> StreamOutUntilClosedAsync();
        IAsyncEnumerable<TFromQueue> StreamOutUntilEmptyAsync();
    }

    public class Consumer : IConsumer<ReceivedData>, IDisposable
    {
        private readonly ILogger<Consumer> _logger;
        private readonly SemaphoreSlim _conLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _executionLock = new SemaphoreSlim(1, 1);
        private IChannelHost _chanHost;
        private bool _disposedValue;
        private Channel<ReceivedData> _consumerChannel;
        private bool _shutdown;

        public RabbitOptions Options { get; }
        public ConsumerOptions ConsumerOptions { get; }

        public IChannelPool ChannelPool { get; }
        public bool Started { get; private set; }

        public Consumer(RabbitOptions options, string consumerName)
            : this(new ChannelPool(options), consumerName)
        { }

        public Consumer(IChannelPool channelPool, string consumerName)
            : this(
                  channelPool,
                  channelPool.Options.GetConsumerOptions(consumerName))
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));
        }

        public Consumer(IChannelPool channelPool, ConsumerOptions consumerOptions)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNull(consumerOptions, nameof(consumerOptions));

            _logger = LogHelper.GetLogger<Consumer>();
            Options = channelPool.Options;
            ChannelPool = channelPool;
            ConsumerOptions = consumerOptions;
        }

        public async Task StartConsumerAsync()
        {
            await _conLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (!Started && ConsumerOptions.Enabled)
                {
                    await SetChannelHostAsync().ConfigureAwait(false);
                    _shutdown = false;
                    _consumerChannel = Channel.CreateBounded<ReceivedData>(
                        new BoundedChannelOptions(ConsumerOptions.BatchSize!.Value)
                        {
                            FullMode = ConsumerOptions.BehaviorWhenFull!.Value
                        });

                    await Task.Yield();
                    bool success;
                    do
                    {
                        _logger.LogTrace(LogMessages.Consumers.StartingConsumerLoop, ConsumerOptions.ConsumerName);
                        success = await StartConsumingAsync().ConfigureAwait(false);
                    }
                    while (!success);

                    _logger.LogDebug(LogMessages.Consumers.Started, ConsumerOptions.ConsumerName);

                    Started = true;
                }
            }
            finally { _conLock.Release(); }
        }

        public async Task StopConsumerAsync(bool immediate = false)
        {
            await _conLock
                .WaitAsync()
                .ConfigureAwait(false);

            _logger.LogDebug(LogMessages.Consumers.StopConsumer, ConsumerOptions.ConsumerName);

            try
            {
                if (Started)
                {
                    _shutdown = true;
                    _consumerChannel.Writer.Complete();

                    if (immediate)
                    {
                        _chanHost.Close();
                    }

                    await _consumerChannel
                        .Reader
                        .Completion
                        .ConfigureAwait(false);

                    Started = false;
                    _logger.LogDebug(
                        LogMessages.Consumers.StoppedConsumer,
                        ConsumerOptions.ConsumerName);
                }
            }
            finally { _conLock.Release(); }
        }

        private async Task<bool> StartConsumingAsync()
        {
            if (_shutdown)
            { return false; }

            _logger.LogInformation(
                LogMessages.Consumers.StartingConsumer,
                ConsumerOptions.ConsumerName);

            if (Options.FactoryOptions.EnableDispatchConsumersAsync)
            {
                if (_asyncConsumer != null) // Cleanup operation, this prevents an EventHandler leak.
                {
                    _asyncConsumer.Received -= ReceiveHandlerAsync;
                    _asyncConsumer.Shutdown -= ConsumerShutdownAsync;
                }

                try
                {
                    _asyncConsumer = CreateAsyncConsumer();
                    if (_asyncConsumer == null) { return false; }

                    BasicConsume(_asyncConsumer);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception creating internal RabbitMQ consumer. Retrying...");
                    await Task.Delay(1000).ConfigureAwait(false);
                    await _chanHost.MakeChannelAsync().ConfigureAwait(false);
                    return false;
                }
            }
            else
            {
                if (_consumer != null) // Cleanup operation, this prevents an EventHandler leak.
                {
                    _consumer.Received -= ReceiveHandler;
                    _consumer.Shutdown -= ConsumerShutdown;
                }

                try
                {
                    _consumer = CreateConsumer();
                    if (_consumer == null) { return false; }

                    BasicConsume(_consumer);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception creating internal RabbitMQ consumer. Retrying...");
                    await Task.Delay(1000).ConfigureAwait(false);
                    await _chanHost.MakeChannelAsync().ConfigureAwait(false);
                    return false;
                }
            }

            _logger.LogInformation(
                LogMessages.Consumers.StartedConsumer,
                ConsumerOptions.ConsumerName);

            return true;
        }

        private void BasicConsume(IBasicConsumer consumer)
        {
            _chanHost
                .GetChannel()
                .BasicConsume(
                    ConsumerOptions.QueueName,
                    ConsumerOptions.AutoAck ?? false,
                    ConsumerOptions.ConsumerName,
                    ConsumerOptions.NoLocal ?? false,
                    ConsumerOptions.Exclusive ?? false,
                    null,
                    consumer);
        }

        private AsyncEventingBasicConsumer _asyncConsumer;
        private EventingBasicConsumer _consumer;

        private async Task SetChannelHostAsync()
        {
            if (ConsumerOptions.UseTransientChannels ?? true)
            {
                var autoAck = ConsumerOptions.AutoAck ?? false;
                _logger.LogTrace(LogMessages.Consumers.GettingTransientChannelHost, ConsumerOptions.ConsumerName);
                _chanHost = await ChannelPool
                    .GetTransientChannelAsync(!autoAck)
                    .ConfigureAwait(false);
            }
            else if (ConsumerOptions.AutoAck ?? false)
            {
                _logger.LogTrace(LogMessages.Consumers.GettingChannelHost, ConsumerOptions.ConsumerName);
                _chanHost = await ChannelPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                _logger.LogTrace(LogMessages.Consumers.GettingAckChannelHost, ConsumerOptions.ConsumerName);
                _chanHost = await ChannelPool
                    .GetAckChannelAsync()
                    .ConfigureAwait(false);
            }

            _logger.LogDebug(
                LogMessages.Consumers.ChannelEstablished,
                ConsumerOptions.ConsumerName,
                _chanHost?.ChannelId ?? 0ul);
        }

        private EventingBasicConsumer CreateConsumer()
        {
            EventingBasicConsumer consumer = null;

            _chanHost.GetChannel().BasicQos(0, ConsumerOptions.BatchSize!.Value, false);
            consumer = new EventingBasicConsumer(_chanHost.GetChannel());

            consumer.Received += ReceiveHandler;
            consumer.Shutdown += ConsumerShutdown;

            return consumer;
        }

        protected virtual async void ReceiveHandler(object _, BasicDeliverEventArgs bdea)
        {
            _logger.LogDebug(
                LogMessages.Consumers.ConsumerMessageReceived,
                ConsumerOptions.ConsumerName,
                bdea.DeliveryTag);

            await HandleMessage(bdea).ConfigureAwait(false);
        }

        private async void ConsumerShutdown(object sender, ShutdownEventArgs e)
        {
            await HandleUnexpectedShutdownAsync(e).ConfigureAwait(false);
        }

        private AsyncEventingBasicConsumer CreateAsyncConsumer()
        {
            AsyncEventingBasicConsumer consumer = null;

            _chanHost.GetChannel().BasicQos(0, ConsumerOptions.BatchSize!.Value, false);
            consumer = new AsyncEventingBasicConsumer(_chanHost.GetChannel());

            consumer.Received += ReceiveHandlerAsync;
            consumer.Shutdown += ConsumerShutdownAsync;

            return consumer;
        }

        protected virtual async Task ReceiveHandlerAsync(object _, BasicDeliverEventArgs bdea)
        {
            _logger.LogDebug(
                LogMessages.Consumers.ConsumerAsyncMessageReceived,
                ConsumerOptions.ConsumerName,
                bdea.DeliveryTag);

            await HandleMessage(bdea).ConfigureAwait(false);
        }

        protected async ValueTask<bool> HandleMessage(BasicDeliverEventArgs bdea)
        {
            if (!await _consumerChannel.Writer.WaitToWriteAsync().ConfigureAwait(false)) return false;

            try
            {
                await _consumerChannel
                    .Writer
                    .WriteAsync(new ReceivedData(_chanHost.GetChannel(), bdea, !(ConsumerOptions.AutoAck ?? false)))
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

        private async Task ConsumerShutdownAsync(object sender, ShutdownEventArgs e)
        {
            await HandleUnexpectedShutdownAsync(e).ConfigureAwait(false);
        }

        private async Task HandleUnexpectedShutdownAsync(ShutdownEventArgs e)
        {
            if (!_shutdown)
            {
                await Task.Yield();
                bool success;
                do
                {
                    success = await _chanHost.MakeChannelAsync().ConfigureAwait(false);

                    if (success)
                    {
                        _logger.LogWarning(
                            LogMessages.Consumers.ConsumerShutdownEvent,
                            ConsumerOptions.ConsumerName,
                            e.ReplyText);

                        success = await StartConsumingAsync().ConfigureAwait(false);
                    }
                }
                while (!_shutdown && !success);
            }
        }

        public ChannelReader<ReceivedData> GetConsumerBuffer() => _consumerChannel.Reader;

        public async ValueTask<ReceivedData> ReadAsync()
        {
            if (!await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false)) throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);

            return await _consumerChannel
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<ReceivedData>> ReadUntilEmptyAsync()
        {
            if (!await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false)) throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);

            var list = new List<ReceivedData>();
            await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (_consumerChannel.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                list.Add(message);
            }

            return list;
        }

        public async IAsyncEnumerable<ReceivedData> StreamOutUntilEmptyAsync()
        {
            if (!await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false)) throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);

            await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (_consumerChannel.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                yield return message;
            }
        }

        public async IAsyncEnumerable<ReceivedData> StreamOutUntilClosedAsync()
        {
            if (!await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false)) throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);

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
                while (await _consumerChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (_consumerChannel.Reader.TryRead(out var receivedData))
                    {
                        if (receivedData != null)
                        {
                            _logger.LogDebug(
                                LogMessages.Consumers.ConsumerDataflowQueueing,
                                ConsumerOptions.ConsumerName,
                                receivedData.DeliveryTag);

                            await dataflowEngine
                                .EnqueueWorkAsync(receivedData)
                                .ConfigureAwait(false);
                        }
                    }
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
                taskScheduler);

            await TransferDataToChannelBlockEngine(channelBlockEngine, token);
        }

        private async Task TransferDataToChannelBlockEngine(
            ChannelBlockEngine<ReceivedData, bool> channelBlockEngine,
            CancellationToken token = default)
        {
            await _executionLock.WaitAsync(2000, token).ConfigureAwait(false);

            try
            {
                while (await _consumerChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    var receivedData = await _consumerChannel.Reader.ReadAsync(token);
                    if (receivedData != null)
                    {
                        _logger.LogDebug(
                            LogMessages.Consumers.ConsumerDataflowQueueing,
                            ConsumerOptions.ConsumerName,
                            receivedData.DeliveryTag);

                        await channelBlockEngine
                            .EnqueueWorkAsync(receivedData)
                            .ConfigureAwait(false);
                    }
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
                _chanHost = null;
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
