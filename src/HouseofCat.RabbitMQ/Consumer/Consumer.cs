using HouseofCat.Logger;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities.Errors;
using HouseofCat.Workflows;
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
        Options Options { get; }
        ConsumerOptions ConsumerOptions { get; }
        bool Started { get; }

        ReadOnlyMemory<byte> HashKey { get; set; }

        Task DataflowExecutionEngineAsync(Func<TFromQueue, Task<bool>> workBodyAsync, int maxDoP = 4, bool ensureOrdered = true, CancellationToken token = default);

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
        private readonly SemaphoreSlim _dataFlowExecLock = new SemaphoreSlim(1, 1);
        private IChannelHost _chanHost;
        private bool _disposedValue;
        private Channel<ReceivedData> _dataBuffer;
        private bool _shutdown { get; set; }

        public Options Options { get; }
        public ConsumerOptions ConsumerOptions { get; }

        public IChannelPool ChannelPool { get; }
        public bool Started { get; private set; }

        public ReadOnlyMemory<byte> HashKey { get; set; }

        public Consumer(Options options, string consumerName, byte[] hashKey = null)
            : this(new ChannelPool(options), consumerName, hashKey)
        { }

        public Consumer(IChannelPool channelPool, string consumerName, byte[] hashKey = null)
            : this(
                  channelPool,
                  channelPool.Options.GetConsumerOptions(consumerName),
                  hashKey)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));
        }

        public Consumer(IChannelPool channelPool, ConsumerOptions consumerOptions, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNull(consumerOptions, nameof(consumerOptions));

            _logger = LogHelper.GetLogger<Consumer>();
            Options = channelPool.Options;
            ChannelPool = channelPool;
            HashKey = hashKey;
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
                    _shutdown = false;
                    _dataBuffer = Channel.CreateBounded<ReceivedData>(
                    new BoundedChannelOptions(ConsumerOptions.BatchSize.Value)
                    {
                        FullMode = ConsumerOptions.BehaviorWhenFull.Value
                    });

                    await _dataBuffer
                        .Writer
                        .WaitToWriteAsync()
                        .ConfigureAwait(false);

                    await SetChannelHostAsync()
                        .ConfigureAwait(false);

                    bool success;
                    do
                    {
                        _logger.LogTrace(LogMessages.Consumers.StartingConsumerLoop, ConsumerOptions.ConsumerName);
                        success = StartConsuming();
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
                    _dataBuffer.Writer.Complete();

                    if (immediate)
                    {
                        _chanHost.Close();
                    }

                    await _dataBuffer
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

        private bool StartConsuming()
        {
            if (_shutdown)
            { return false; }

            _logger.LogInformation(
                LogMessages.Consumers.StartingConsumer,
                ConsumerOptions.ConsumerName);

            if (Options.FactoryOptions.EnableDispatchConsumersAsync)
            {
                if (_asyncConsumer != null)
                {
                    _asyncConsumer.Received -= ReceiveHandlerAsync;
                    _asyncConsumer.Shutdown -= ConsumerShutdownAsync;
                }

                _asyncConsumer = CreateAsyncConsumer();
                if (_asyncConsumer == null) { return false; }

                _chanHost
                    .GetChannel()
                    .BasicConsume(
                        ConsumerOptions.QueueName,
                        ConsumerOptions.AutoAck ?? false,
                        ConsumerOptions.ConsumerName,
                        ConsumerOptions.NoLocal ?? false,
                        ConsumerOptions.Exclusive ?? false,
                        null,
                        _asyncConsumer);
            }
            else
            {
                if (_asyncConsumer != null)
                {
                    _consumer.Received -= ReceiveHandler;
                    _consumer.Shutdown -= ConsumerShutdown;
                }

                _consumer = CreateConsumer();
                if (_consumer == null) { return false; }

                _chanHost
                    .GetChannel()
                    .BasicConsume(
                        ConsumerOptions.QueueName,
                        ConsumerOptions.AutoAck ?? false,
                        ConsumerOptions.ConsumerName,
                        ConsumerOptions.NoLocal ?? false,
                        ConsumerOptions.Exclusive ?? false,
                        null,
                        _consumer);
            }

            _logger.LogInformation(
                LogMessages.Consumers.StartedConsumer,
                ConsumerOptions.ConsumerName);

            return true;
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

            try
            {
                _chanHost.GetChannel().BasicQos(0, ConsumerOptions.BatchSize.Value, false);
                consumer = new EventingBasicConsumer(_chanHost.GetChannel());

                consumer.Received += ReceiveHandler;
                consumer.Shutdown += ConsumerShutdown;
            }
            catch { }

            return consumer;
        }

        private async void ReceiveHandler(object _, BasicDeliverEventArgs bdea)
        {
            var rabbitMessage = new ReceivedData(_chanHost.GetChannel(), bdea, !(ConsumerOptions.AutoAck ?? false), HashKey);

            _logger.LogDebug(
                LogMessages.Consumers.ConsumerMessageReceived,
                ConsumerOptions.ConsumerName,
                bdea.DeliveryTag);

            if (await _dataBuffer
                    .Writer
                    .WaitToWriteAsync()
                    .ConfigureAwait(false))
            {
                await _dataBuffer
                    .Writer
                    .WriteAsync(rabbitMessage);
            }
        }

        private async void ConsumerShutdown(object sender, ShutdownEventArgs e)
        {
            await HandleUnexpectedShutdownAsync(e)
                .ConfigureAwait(false);
        }

        private AsyncEventingBasicConsumer CreateAsyncConsumer()
        {
            AsyncEventingBasicConsumer consumer = null;

            try
            {
                _chanHost.GetChannel().BasicQos(0, ConsumerOptions.BatchSize.Value, false);
                consumer = new AsyncEventingBasicConsumer(_chanHost.GetChannel());

                consumer.Received += ReceiveHandlerAsync;
                consumer.Shutdown += ConsumerShutdownAsync;
            }
            catch { }

            return consumer;
        }

        private async Task ReceiveHandlerAsync(object o, BasicDeliverEventArgs bdea)
        {
            var rabbitMessage = new ReceivedData(_chanHost.GetChannel(), bdea, !(ConsumerOptions.AutoAck ?? false), HashKey);

            _logger.LogDebug(
                LogMessages.Consumers.ConsumerAsyncMessageReceived,
                ConsumerOptions.ConsumerName,
                bdea.DeliveryTag);

            if (await _dataBuffer
                .Writer
                .WaitToWriteAsync()
                .ConfigureAwait(false))
            {
                await _dataBuffer
                    .Writer
                    .WriteAsync(rabbitMessage);
            }
        }

        private async Task ConsumerShutdownAsync(object sender, ShutdownEventArgs e)
        {
            await HandleUnexpectedShutdownAsync(e)
                .ConfigureAwait(false);
        }

        private async Task HandleUnexpectedShutdownAsync(ShutdownEventArgs e)
        {
            if (!_shutdown)
            {
                await Task.Yield();
                bool success;
                do
                {
                    await _chanHost.MakeChannelAsync();

                    _logger.LogWarning(
                        LogMessages.Consumers.ConsumerShutdownEvent,
                        ConsumerOptions.ConsumerName,
                        e.ReplyText);

                    success = StartConsuming();
                }
                while (!_shutdown && !success);
            }
        }

        public ChannelReader<ReceivedData> GetConsumerBuffer() => _dataBuffer.Reader;

        public async ValueTask<ReceivedData> ReadAsync()
        {
            if (!await _dataBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }

            return await _dataBuffer
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<ReceivedData>> ReadUntilEmptyAsync()
        {
            if (!await _dataBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }

            var list = new List<ReceivedData>();
            await _dataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (_dataBuffer.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                list.Add(message);
            }

            return list;
        }

        public async IAsyncEnumerable<ReceivedData> StreamOutUntilEmptyAsync()
        {
            if (!await _dataBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }

            await _dataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (_dataBuffer.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                yield return message;
            }
        }

        public async IAsyncEnumerable<ReceivedData> StreamOutUntilClosedAsync()
        {
            if (!await _dataBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }

            await foreach (var receivedData in _dataBuffer.Reader.ReadAllAsync())
            {
                yield return receivedData;
            }
        }
        public async Task DataflowExecutionEngineAsync(Func<ReceivedData, Task<bool>> workBodyAsync, int maxDoP = 4, bool ensureOrdered = true, CancellationToken token = default)
        {
            await _dataFlowExecLock.WaitAsync(2000).ConfigureAwait(false);

            try
            {
                var dataflowEngine = new DataflowEngine<ReceivedData, bool>(workBodyAsync, maxDoP, ensureOrdered);

                while (await _dataBuffer.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (_dataBuffer.Reader.TryRead(out var receivedData))
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
            finally { _dataFlowExecLock.Release(); }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _dataFlowExecLock.Dispose();
                    _conLock.Dispose();
                }

                _dataBuffer = null;
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
