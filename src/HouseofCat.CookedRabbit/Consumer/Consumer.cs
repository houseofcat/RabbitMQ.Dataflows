using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using HouseofCat.Logger;
using HouseofCat.Utilities.Errors;
using HouseofCat.Workflows;
using HouseofCat.Workflows.Pipelines;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CookedRabbit.Core
{
    public interface IConsumer<TFromQueue>
    {
        IChannelPool ChannelPool { get; }
        Config Config { get; }
        ConsumerOptions Options { get; }
        bool Started { get; }

        ReadOnlyMemory<byte> HashKey { get; set; }

        Task DataflowExecutionEngineAsync(Func<TFromQueue, Task<bool>> workBodyAsync, int maxDoP = 4, bool ensureOrdered = true, CancellationToken token = default);
        Task PipelineExecutionEngineAsync<TLocalOut>(IPipeline<TFromQueue, TLocalOut> pipeline, bool waitForCompletion, CancellationToken token = default);
        Task PipelineStreamEngineAsync<TOut>(IPipeline<ReceivedData, TOut> pipeline, bool waitForCompletion, CancellationToken token = default);

        ChannelReader<TFromQueue> GetConsumerBuffer();
        ValueTask<TFromQueue> ReadAsync();
        Task<IEnumerable<TFromQueue>> ReadUntilEmptyAsync();
        Task StartConsumerAsync(bool autoAck = false, bool useTransientChannel = true);
        Task StopConsumerAsync(bool immediate = false);
        IAsyncEnumerable<TFromQueue> StreamOutUntilClosedAsync();
        IAsyncEnumerable<TFromQueue> StreamOutUntilEmptyAsync();
    }

    public class Consumer : IConsumer<ReceivedData>, IDisposable
    {
        private readonly ILogger<Consumer> _logger;
        private readonly SemaphoreSlim _conLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _pipeExecLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _dataFlowExecLock = new SemaphoreSlim(1, 1);
        private IChannelHost _chanHost;
        private bool _disposedValue;
        private Channel<ReceivedData> _dataBuffer;
        private bool _shutdown { get; set; }

        public Config Config { get; }
        public ConsumerOptions Options { get; }

        public IChannelPool ChannelPool { get; }
        public bool Started { get; private set; }

        public ReadOnlyMemory<byte> HashKey { get; set; }

        public Consumer(Config config, string consumerName, byte[] hashKey = null)
        {
            Guard.AgainstNull(config, nameof(config));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            _logger = LogHelper.GetLogger<Consumer>();
            Config = config;
            ChannelPool = new ChannelPool(Config);
            HashKey = hashKey;

            Options = Config.GetConsumerSettings(consumerName);
        }

        public Consumer(IChannelPool channelPool, string consumerName, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            _logger = LogHelper.GetLogger<Consumer>();
            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;

            Options = Config.GetConsumerSettings(consumerName);
        }

        public Consumer(IChannelPool channelPool, ConsumerOptions consumerSettings, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNull(consumerSettings, nameof(consumerSettings));

            _logger = LogHelper.GetLogger<Consumer>();
            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;
            Options = consumerSettings;
        }

        public async Task StartConsumerAsync(bool autoAck = false, bool useTransientChannel = true)
        {
            await _conLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (!Started && Options.Enabled)
                {
                    _shutdown = false;
                    _dataBuffer = Channel.CreateBounded<ReceivedData>(
                    new BoundedChannelOptions(Options.BatchSize.Value)
                    {
                        FullMode = Options.BehaviorWhenFull.Value
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
                        _logger.LogTrace(LogMessages.Consumer.StartingConsumerLoop, Options.ConsumerName);
                        success = StartConsuming();
                    }
                    while (!success);

                    _logger.LogDebug(LogMessages.Consumer.Started, Options.ConsumerName);

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

            _logger.LogDebug(LogMessages.Consumer.StopConsumer, Options.ConsumerName);

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
                        LogMessages.Consumer.StoppedConsumer,
                        Options.ConsumerName);
                }
            }
            finally { _conLock.Release(); }
        }

        private bool StartConsuming()
        {
            if (_shutdown)
            { return false; }

            _logger.LogInformation(
                LogMessages.Consumer.StartingConsumer,
                Options.ConsumerName);

            if (Config.FactorySettings.EnableDispatchConsumersAsync)
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
                        Options.QueueName,
                        Options.AutoAck ?? false,
                        Options.ConsumerName,
                        Options.NoLocal ?? false,
                        Options.Exclusive ?? false,
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
                        Options.QueueName,
                        Options.AutoAck ?? false,
                        Options.ConsumerName,
                        Options.NoLocal ?? false,
                        Options.Exclusive ?? false,
                        null,
                        _consumer);
            }

            _logger.LogInformation(
                LogMessages.Consumer.StartedConsumer,
                Options.ConsumerName);
            return true;
        }

        private AsyncEventingBasicConsumer _asyncConsumer;
        private EventingBasicConsumer _consumer;

        private async Task SetChannelHostAsync()
        {
            if (Options.UseTransientChannels ?? true)
            {
                var autoAck = Options.AutoAck ?? false;
                _logger.LogTrace(LogMessages.Consumer.GettingTransientChannelHost, Options.ConsumerName);
                _chanHost = await ChannelPool
                    .GetTransientChannelAsync(!autoAck)
                    .ConfigureAwait(false);
            }
            else if (Options.AutoAck ?? false)
            {
                _logger.LogTrace(LogMessages.Consumer.GettingChannelHost, Options.ConsumerName);
                _chanHost = await ChannelPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                _logger.LogTrace(LogMessages.Consumer.GettingAckChannelHost, Options.ConsumerName);
                _chanHost = await ChannelPool
                    .GetAckChannelAsync()
                    .ConfigureAwait(false);
            }

            _logger.LogDebug(
                LogMessages.Consumer.ChannelEstablished,
                Options.ConsumerName,
                _chanHost?.ChannelId ?? 0ul);
        }

        private EventingBasicConsumer CreateConsumer()
        {
            EventingBasicConsumer consumer = null;

            try
            {
                _chanHost.GetChannel().BasicQos(0, Options.BatchSize.Value, false);
                consumer = new EventingBasicConsumer(_chanHost.GetChannel());

                consumer.Received += ReceiveHandler;
                consumer.Shutdown += ConsumerShutdown;
            }
            catch { }

            return consumer;
        }

        private async void ReceiveHandler(object _, BasicDeliverEventArgs bdea)
        {
            var rabbitMessage = new ReceivedData(_chanHost.GetChannel(), bdea, !(Options.AutoAck ?? false), HashKey);

            _logger.LogDebug(
                LogMessages.Consumer.ConsumerMessageReceived,
                Options.ConsumerName,
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
            await HandleUnexpectedShutdownAsync(e).ConfigureAwait(false);
        }

        private AsyncEventingBasicConsumer CreateAsyncConsumer()
        {
            AsyncEventingBasicConsumer consumer = null;

            try
            {
                _chanHost.GetChannel().BasicQos(0, Options.BatchSize.Value, false);
                consumer = new AsyncEventingBasicConsumer(_chanHost.GetChannel());

                consumer.Received += ReceiveHandlerAsync;
                consumer.Shutdown += ConsumerShutdownAsync;
            }
            catch { }

            return consumer;
        }

        private async Task ReceiveHandlerAsync(object o, BasicDeliverEventArgs bdea)
        {
            var rabbitMessage = new ReceivedData(_chanHost.GetChannel(), bdea, !(Options.AutoAck ?? false), HashKey);

            _logger.LogDebug(
                LogMessages.Consumer.ConsumerAsyncMessageReceived,
                Options.ConsumerName,
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
                        LogMessages.Consumer.ConsumerShutdownEvent,
                        Options.ConsumerName,
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
                                LogMessages.Consumer.ConsumerDataflowQueueing,
                                Options.ConsumerName,
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
                    LogMessages.Consumer.ConsumerDataflowActionCancelled,
                    Options.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumer.ConsumerDataflowError,
                    Options.ConsumerName,
                    ex.Message);
            }
            finally { _dataFlowExecLock.Release(); }
        }

        public async Task PipelineStreamEngineAsync<TOut>(IPipeline<ReceivedData, TOut> pipeline, bool waitForCompletion, CancellationToken token = default)
        {
            await _pipeExecLock
                .WaitAsync(2000)
                .ConfigureAwait(false);

            try
            {
                await foreach (var receivedData in _dataBuffer.Reader.ReadAllAsync(token))
                {
                    if (receivedData == null) { continue; }

                    _logger.LogDebug(
                        LogMessages.Consumer.ConsumerPipelineQueueing,
                        Options.ConsumerName,
                        receivedData.DeliveryTag);

                    await pipeline
                        .QueueForExecutionAsync(receivedData)
                        .ConfigureAwait(false);

                    if (waitForCompletion)
                    {
                        _logger.LogTrace(
                            LogMessages.Consumer.ConsumerPipelineWaiting,
                            Options.ConsumerName,
                            receivedData.DeliveryTag);

                        await receivedData
                            .Completion()
                            .ConfigureAwait(false);

                        _logger.LogTrace(
                            LogMessages.Consumer.ConsumerPipelineWaitingDone,
                            Options.ConsumerName,
                            receivedData.DeliveryTag);
                    }

                    if (token.IsCancellationRequested)
                    { return; }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    LogMessages.Consumer.ConsumerPipelineActionCancelled,
                    Options.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumer.ConsumerPipelineError,
                    Options.ConsumerName,
                    ex.Message);
            }
            finally { _pipeExecLock.Release(); }
        }

        public async Task PipelineExecutionEngineAsync<TOut>(IPipeline<ReceivedData, TOut> pipeline, bool waitForCompletion, CancellationToken token = default)
        {
            await _pipeExecLock
                .WaitAsync(2000)
                .ConfigureAwait(false);

            try
            {
                while (await _dataBuffer.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (_dataBuffer.Reader.TryRead(out var receivedData))
                    {
                        if (receivedData == null) { continue; }

                        _logger.LogDebug(
                            LogMessages.Consumer.ConsumerPipelineQueueing,
                            Options.ConsumerName,
                            receivedData.DeliveryTag);

                        await pipeline
                            .QueueForExecutionAsync(receivedData)
                            .ConfigureAwait(false);

                        if (waitForCompletion)
                        {
                            _logger.LogTrace(
                                LogMessages.Consumer.ConsumerPipelineWaiting,
                                Options.ConsumerName,
                                receivedData.DeliveryTag);

                            await receivedData
                                .Completion()
                                .ConfigureAwait(false);

                            _logger.LogTrace(
                                LogMessages.Consumer.ConsumerPipelineWaitingDone,
                                Options.ConsumerName,
                                receivedData.DeliveryTag);
                        }

                        if (token.IsCancellationRequested)
                        { return; }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    LogMessages.Consumer.ConsumerPipelineActionCancelled,
                    Options.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumer.ConsumerPipelineError,
                    Options.ConsumerName,
                    ex.Message);
            }
            finally { _pipeExecLock.Release(); }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _dataFlowExecLock.Dispose();
                    _pipeExecLock.Dispose();
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
