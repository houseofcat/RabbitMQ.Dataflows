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
        private readonly SemaphoreSlim _conLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _executionLock = new SemaphoreSlim(1, 1);
        private bool _disposedValue;
        private Channel<ReceivedData> _consumerChannel;
        private bool _shutdown;

        protected ILogger Logger { get; }
        protected IChannelHost ChanHost { get; private set; }

        public RabbitOptions Options { get; }
        public ConsumerOptions ConsumerOptions { get; }

        public IChannelPool ChannelPool { get; }
        public bool Started { get; private set; }

        public Consumer(RabbitOptions options, string consumerName) : this(new ChannelPool(options), consumerName)
        { }

        public Consumer(IChannelPool channelPool, string consumerName)
            : this(channelPool, channelPool.Options.GetConsumerOptions(consumerName))
        {
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));
        }

        public Consumer(IChannelPool channelPool, ConsumerOptions consumerOptions) :
            this(channelPool, consumerOptions, LogHelper.GetLogger<Consumer>())
        { }

        protected Consumer(IChannelPool channelPool, ConsumerOptions consumerOptions, ILogger logger)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNull(consumerOptions, nameof(consumerOptions));

            Logger = logger;
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
                        Logger.LogTrace(Consumers.StartingConsumerLoop, ConsumerOptions.ConsumerName);
                        success = await StartConsumingAsync().ConfigureAwait(false);
                    }
                    while (!success);

                    Logger.LogDebug(Consumers.Started, ConsumerOptions.ConsumerName);

                    Started = true;
                }
            }
            finally { _conLock.Release(); }
        }

        public async Task StopConsumerAsync(bool immediate = false)
        {
            if (!await _conLock.WaitAsync(0).ConfigureAwait(false)) return;

            Logger.LogDebug(Consumers.StopConsumer, ConsumerOptions.ConsumerName);

            try
            {
                if (Started)
                {
                    _shutdown = true;
                    _consumerChannel.Writer.Complete();

                    if (immediate)
                    {
                        ChanHost.Close();
                    }

                    await _consumerChannel
                        .Reader
                        .Completion
                        .ConfigureAwait(false);

                    Started = false;
                    Logger.LogDebug(
                        Consumers.StoppedConsumer,
                        ConsumerOptions.ConsumerName);
                }
            }
            finally { _conLock.Release(); }
        }

        private AsyncEventingBasicConsumer _asyncConsumer;
        private EventingBasicConsumer _consumer;

        private async ValueTask<bool> StartConsumingAsync()
        {
            if (_shutdown)
            { return false; }

            Logger.LogInformation(
                Consumers.StartingConsumer,
                ConsumerOptions.ConsumerName);

            if (Options.FactoryOptions.EnableDispatchConsumersAsync)
            {
                if (_asyncConsumer != null) // Cleanup operation, this prevents an EventHandler leak.
                {
                    RemoveAsyncEventHandlers(_asyncConsumer);
                }

                try
                {
                    _asyncConsumer = CreateAsyncConsumer();
                    if (_asyncConsumer == null) { return false; }

                    BasicConsume(_asyncConsumer);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Exception creating internal RabbitMQ consumer. Retrying...");
                    await Task.Delay(1000).ConfigureAwait(false);
                    await ChanHost.MakeChannelAsync().ConfigureAwait(false);
                    return false;
                }
            }
            else
            {
                if (_consumer != null) // Cleanup operation, this prevents an EventHandler leak.
                {
                    RemoveEventHandlers(_consumer);
                }

                try
                {
                    _consumer = CreateConsumer();
                    if (_consumer == null) { return false; }

                    BasicConsume(_consumer);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Exception creating internal RabbitMQ consumer. Retrying...");
                    await Task.Delay(1000).ConfigureAwait(false);
                    await ChanHost.MakeChannelAsync().ConfigureAwait(false);
                    return false;
                }
            }

            Logger.LogInformation(
                Consumers.StartedConsumer,
                ConsumerOptions.ConsumerName);

            return true;
        }

        private void BasicConsume(IBasicConsumer consumer)
        {
            ChanHost
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

        private async ValueTask SetChannelHostAsync()
        {
            if (ConsumerOptions.UseTransientChannels ?? true)
            {
                var autoAck = ConsumerOptions.AutoAck ?? false;
                Logger.LogTrace(Consumers.GettingTransientChannelHost, ConsumerOptions.ConsumerName);
                ChanHost = await ChannelPool
                    .GetTransientChannelAsync(!autoAck)
                    .ConfigureAwait(false);
            }
            else if (ConsumerOptions.AutoAck ?? false)
            {
                Logger.LogTrace(Consumers.GettingChannelHost, ConsumerOptions.ConsumerName);
                ChanHost = await ChannelPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                Logger.LogTrace(Consumers.GettingAckChannelHost, ConsumerOptions.ConsumerName);
                ChanHost = await ChannelPool
                    .GetAckChannelAsync()
                    .ConfigureAwait(false);
            }

            Logger.LogDebug(
                Consumers.ChannelEstablished,
                ConsumerOptions.ConsumerName,
                ChanHost?.ChannelId ?? 0ul);
        }

        private EventingBasicConsumer CreateConsumer()
        {
            ChanHost.GetChannel().BasicQos(0, ConsumerOptions.BatchSize!.Value, false);
            var consumer = new EventingBasicConsumer(ChanHost.GetChannel());

            AddEventHandlers(consumer);

            return consumer;
        }

        protected virtual void AddEventHandlers(EventingBasicConsumer consumer)
        {
            consumer.Received += ReceiveHandler;
            consumer.Shutdown += ConsumerShutdown;
        }

        protected virtual void RemoveEventHandlers(EventingBasicConsumer consumer)
        {
            consumer.Received -= ReceiveHandler;
            consumer.Shutdown -= ConsumerShutdown;
        }

        protected virtual async void ReceiveHandler(object _, BasicDeliverEventArgs bdea)
        {
            Logger.LogDebug(
                Consumers.ConsumerMessageReceived,
                ConsumerOptions.ConsumerName,
                bdea.DeliveryTag);

            await HandleMessage(bdea).ConfigureAwait(false);
        }

        private async void ConsumerShutdown(object _, ShutdownEventArgs e) =>
            await HandleUnexpectedShutdownAsync(e).ConfigureAwait(false);

        private AsyncEventingBasicConsumer CreateAsyncConsumer()
        {
            ChanHost.GetChannel().BasicQos(0, ConsumerOptions.BatchSize!.Value, false);
            var asyncConsumer = new AsyncEventingBasicConsumer(ChanHost.GetChannel());

            AddAsyncEventHandlers(asyncConsumer);

            return asyncConsumer;
        }

        protected virtual void AddAsyncEventHandlers(AsyncEventingBasicConsumer asyncConsumer)
        {
            asyncConsumer.Received += ReceiveHandlerAsync;
            asyncConsumer.Shutdown += ConsumerShutdownAsync;
        }

        protected virtual void RemoveAsyncEventHandlers(AsyncEventingBasicConsumer asyncConsumer)
        {
            asyncConsumer.Received -= ReceiveHandlerAsync;
            asyncConsumer.Shutdown -= ConsumerShutdownAsync;
        }

        protected virtual async Task ReceiveHandlerAsync(object _, BasicDeliverEventArgs bdea)
        {
            Logger.LogDebug(
                Consumers.ConsumerAsyncMessageReceived,
                ConsumerOptions.ConsumerName,
                bdea.DeliveryTag);

            await HandleMessage(bdea).ConfigureAwait(false);
        }

        protected async ValueTask<bool> HandleMessage(BasicDeliverEventArgs bdea)
        {
            if (!await _consumerChannel.Writer.WaitToWriteAsync().ConfigureAwait(false))
            {
                return false;
            }

            try
            {
                await _consumerChannel
                    .Writer
                    .WriteAsync(new ReceivedData(ChanHost.GetChannel(), bdea, !(ConsumerOptions.AutoAck ?? false)))
                    .ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    Consumers.ConsumerMessageWriteToBufferError,
                    ConsumerOptions.ConsumerName,
                    ex.Message);
                return false;
            }
        }

        private Task ConsumerShutdownAsync(object _, ShutdownEventArgs e) => HandleUnexpectedShutdownAsync(e);

        private async Task HandleUnexpectedShutdownAsync(ShutdownEventArgs e)
        {
            if (_shutdown)
            {
                return;
            }

            Logger.LogWarning(
                Consumers.ConsumerShutdownEvent,
                ConsumerOptions.ConsumerName,
                e.ReplyText);

            await Task.Yield();
            var success = false;
            do
            {
                try
                {
                    if (!await _conLock.WaitAsync(0).ConfigureAwait(false))
                    {
                        continue;
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                try
                {
                    success = await ChanHost.MakeChannelAsync(StartConsumingAsync).ConfigureAwait(false);
                }
                finally
                {
                    try
                    {
                        _conLock.Release();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            } while (!_shutdown && !success);
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
                            Logger.LogDebug(
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
                Logger.LogWarning(
                    Consumers.ConsumerDataflowActionCancelled,
                    ConsumerOptions.ConsumerName);
            }
            catch (Exception ex)
            {
                Logger.LogError(
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
                Logger.LogWarning(
                    Consumers.ConsumerDataflowActionCancelled,
                    ConsumerOptions.ConsumerName);
            }
            catch (Exception ex)
            {
                Logger.LogError(
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
                        Logger.LogDebug(
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
                Logger.LogWarning(
                    Consumers.ConsumerDataflowActionCancelled,
                    ConsumerOptions.ConsumerName);
            }
            catch (Exception ex)
            {
                Logger.LogError(
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

                ChanHost = null;
                _consumerChannel = null;
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
