using HouseofCat.Dataflows;
using HouseofCat.Logger;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static HouseofCat.RabbitMQ.LogMessages;

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
            if (!await _conLock.WaitAsync(0).ConfigureAwait(false)) return;

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
                        _logger.LogTrace(Consumers.StartingConsumerLoop, ConsumerOptions.ConsumerName);
                        success = await StartConsumingAsync().ConfigureAwait(false);
                    }
                    while (!success);

                    _logger.LogDebug(Consumers.Started, ConsumerOptions.ConsumerName);

                    Started = true;
                }
            }
            finally { _conLock.Release(); }
        }

        public async Task StopConsumerAsync(bool immediate = false)
        {
            if (!await _conLock.WaitAsync(0).ConfigureAwait(false)) return;

            _logger.LogDebug(Consumers.StopConsumer, ConsumerOptions.ConsumerName);

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
                        Consumers.StoppedConsumer,
                        ConsumerOptions.ConsumerName);
                }
            }
            finally { _conLock.Release(); }
        }

        private AsyncEventingBasicConsumer _asyncConsumer;
        private EventingBasicConsumer _consumer;

        private async Task<bool> StartConsumingAsync()
        {
            if (_shutdown)
            { return false; }

            _logger.LogInformation(
                Consumers.StartingConsumer,
                ConsumerOptions.ConsumerName);

            if (Options.FactoryOptions.EnableDispatchConsumersAsync)
            {
                if (_asyncConsumer != null) // Cleanup operation, this prevents an EventHandler leak.
                {
                    _asyncConsumer.ConsumerCancelled -= ConsumerUnregisteredAsync;
                    _asyncConsumer.Received -= ReceiveHandlerAsync;
                    _asyncConsumer.Registered -= ConsumerRegisterAsync;
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
                    _consumer.Registered -= ConsumerRegister;
                    _consumer.Shutdown -= ConsumerShutdown;
                    _consumer.Unregistered -= ConsumerUnregistered;
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
                Consumers.StartedConsumer,
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

        private async Task SetChannelHostAsync()
        {
            if (ConsumerOptions.UseTransientChannels ?? true)
            {
                var autoAck = ConsumerOptions.AutoAck ?? false;
                _logger.LogTrace(Consumers.GettingTransientChannelHost, ConsumerOptions.ConsumerName);
                _chanHost = await ChannelPool
                    .GetTransientChannelAsync(!autoAck)
                    .ConfigureAwait(false);
            }
            else if (ConsumerOptions.AutoAck ?? false)
            {
                _logger.LogTrace(Consumers.GettingChannelHost, ConsumerOptions.ConsumerName);
                _chanHost = await ChannelPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                _logger.LogTrace(Consumers.GettingAckChannelHost, ConsumerOptions.ConsumerName);
                _chanHost = await ChannelPool
                    .GetAckChannelAsync()
                    .ConfigureAwait(false);
            }

            _logger.LogDebug(
                Consumers.ChannelEstablished,
                ConsumerOptions.ConsumerName,
                _chanHost?.ChannelId ?? 0ul);
        }

        private EventingBasicConsumer CreateConsumer()
        {
            _chanHost.GetChannel().BasicQos(0, ConsumerOptions.BatchSize!.Value, false);
            var consumer = new EventingBasicConsumer(_chanHost.GetChannel());

            consumer.Received += ReceiveHandler;
            consumer.Registered += ConsumerRegister;
            consumer.Shutdown += ConsumerShutdown;
            consumer.Unregistered += ConsumerUnregistered;

            return consumer;
        }

        protected async ValueTask<bool> HandleMessage(BasicDeliverEventArgs bdea)
        {
            if (_consumerChannel is null || !await _consumerChannel.Writer.WaitToWriteAsync().ConfigureAwait(false))
            {
                return false;
            }

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
                    Consumers.ConsumerMessageWriteToBufferError,
                    ConsumerOptions.ConsumerName,
                    ex.Message);
                return false;
            }
        }

        protected virtual async void ReceiveHandler(object _, BasicDeliverEventArgs bdea)
        {
            _logger.LogDebug(
                Consumers.ConsumerMessageReceived,
                ConsumerOptions.ConsumerName,
                bdea.DeliveryTag);

            await HandleMessage(bdea).ConfigureAwait(false);
        }

        protected virtual void ConsumerRegister(object _, ConsumerEventArgs args)
        {
            var consumerTag = args.ConsumerTags.First();
            _logger.LogDebug(
                Consumers.ConsumerRegistered,
                ConsumerOptions.ConsumerName,
                consumerTag);
            if (_chanHost is IRecoveryAwareChannelHost recoveryAwareChannelHost)
            {
                recoveryAwareChannelHost.RecordConsumerTag(consumerTag);
            }
        }

        private async void ConsumerShutdown(object sender, ShutdownEventArgs e)
        {
            if (!_shutdown && await _conLock.WaitAsync(0))
            {
                try
                { await HandleUnexpectedShutdownAsync(e).ConfigureAwait(false); }
                finally
                { _conLock.Release(); }
            }
        }

        protected virtual void ConsumerUnregistered(object _, ConsumerEventArgs args)
        {
            var consumerTag = args.ConsumerTags.First();
            _logger.LogDebug(
                Consumers.ConsumerAsyncCancelled,
                ConsumerOptions.ConsumerName,
                consumerTag);
            if (_chanHost is IRecoveryAwareChannelHost recoveryAwareChannelHost)
            {
                recoveryAwareChannelHost.DeleteRecordedConsumerTag(consumerTag);
            }
        }

        private AsyncEventingBasicConsumer CreateAsyncConsumer()
        {
            _chanHost.GetChannel().BasicQos(0, ConsumerOptions.BatchSize!.Value, false);
            var consumer = new AsyncEventingBasicConsumer(_chanHost.GetChannel());

            consumer.Received += ReceiveHandlerAsync;
            consumer.Registered += ConsumerRegisterAsync;
            consumer.Shutdown += ConsumerShutdownAsync;
            consumer.Unregistered += ConsumerUnregisteredAsync;

            return consumer;
        }

        protected virtual async Task ReceiveHandlerAsync(object _, BasicDeliverEventArgs bdea)
        {
            _logger.LogDebug(
                Consumers.ConsumerAsyncMessageReceived,
                ConsumerOptions.ConsumerName,
                bdea.DeliveryTag);

            await HandleMessage(bdea).ConfigureAwait(false);
        }

        protected virtual async Task ConsumerRegisterAsync(object _, ConsumerEventArgs args)
        {
            var consumerTag = args.ConsumerTags.First();
            _logger.LogDebug(
                Consumers.ConsumerAsyncRegistered,
                ConsumerOptions.ConsumerName,
                consumerTag);
            if (_chanHost is IRecoveryAwareChannelHost recoveryAwareChannelHost)
            {
                await recoveryAwareChannelHost.RecordConsumerTagAsync(consumerTag).ConfigureAwait(false);
            }
        }

        private async Task ConsumerShutdownAsync(object sender, ShutdownEventArgs e)
        {
            if (!_shutdown && await _conLock.WaitAsync(0))
            {
                try
                { await HandleUnexpectedShutdownAsync(e).ConfigureAwait(false); }
                finally
                { _conLock.Release(); }
            }
        }

        protected virtual async Task ConsumerUnregisteredAsync(object _, ConsumerEventArgs args)
        {
            var consumerTag = args.ConsumerTags.First();
            _logger.LogDebug(
                Consumers.ConsumerAsyncCancelled,
                ConsumerOptions.ConsumerName,
                consumerTag);
            if (_chanHost is IRecoveryAwareChannelHost recoveryAwareChannelHost)
            {
                await recoveryAwareChannelHost.DeleteRecordedConsumerTagAsync(consumerTag).ConfigureAwait(false);
            }
        }

        private async Task HandleUnexpectedShutdownAsync(ShutdownEventArgs e)
        {
            await Task.Yield();
            bool success;
            do
            {
                success =
                    _chanHost is IRecoveryAwareChannelHost recoveryAwareChanHost
                        ? await recoveryAwareChanHost.RecoverChannelAsync(StartConsumingAsync).ConfigureAwait(false)
                        : await _chanHost.MakeChannelAsync().ConfigureAwait(false);
                if (!success)
                {
                    continue;
                }

                _logger.LogWarning(
                    Consumers.ConsumerShutdownEvent,
                    ConsumerOptions.ConsumerName,
                    e.ReplyText);

                if (_chanHost is not IRecoveryAwareChannelHost)
                {
                    success = await StartConsumingAsync().ConfigureAwait(false);
                }
            }
            while (!_shutdown && !success);
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
                                Consumers.ConsumerDataflowQueueing,
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
                    Consumers.ConsumerDataflowActionCancelled,
                    ConsumerOptions.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    Consumers.ConsumerDataflowError,
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
                    Consumers.ConsumerDataflowActionCancelled,
                    ConsumerOptions.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    Consumers.ConsumerDataflowError,
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
                while (await _consumerChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    var receivedData = await _consumerChannel.Reader.ReadAsync(token);
                    if (receivedData != null)
                    {
                        _logger.LogDebug(
                            Consumers.ConsumerDataflowQueueing,
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
                    Consumers.ConsumerDataflowActionCancelled,
                    ConsumerOptions.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    Consumers.ConsumerDataflowError,
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
