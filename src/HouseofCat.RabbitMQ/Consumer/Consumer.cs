using HouseofCat.Dataflows;
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
using HouseofCat.Utilities.Helpers;

namespace HouseofCat.RabbitMQ;

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
        Func<ReceivedMessage, Task<bool>> workBodyAsync,
        int maxDoP = 4,
        bool ensureOrdered = true,
        Func<bool, Task> postWorkBodyAsync = null,
        int boundedCapacity = 1000,
        TaskScheduler taskScheduler = null,
        CancellationToken token = default);

    Task DirectChannelExecutionEngineAsync(
        Func<ReceivedMessage, Task<bool>> workBodyAsync,
        int maxDoP = 4,
        bool ensureOrdered = true,
        TaskScheduler taskScheduler = null,
        CancellationToken token = default);

    ChannelReader<TFromQueue> GetConsumerBuffer();
    ValueTask<TFromQueue> ReadAsync();
    Task<IEnumerable<TFromQueue>> ReadUntilEmptyAsync();
    Task StartConsumerAsync();
    Task StopConsumerAsync(bool immediate = false);
    IAsyncEnumerable<TFromQueue> StreamUntilConsumerStopAsync();
    IAsyncEnumerable<TFromQueue> StreamUntilQueueEmptyAsync();
}

public class Consumer : IConsumer<ReceivedMessage>, IDisposable
{
    private readonly ILogger<Consumer> _logger;
    private readonly SemaphoreSlim _conLock = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _executionLock = new SemaphoreSlim(1, 1);
    private IChannelHost _chanHost;
    private bool _disposedValue;
    private Channel<ReceivedMessage> _consumerChannel;

    public string ConsumerTag { get; private set; }
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

        _logger = LogHelpers.GetLogger<Consumer>();
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
                _consumerChannel = Channel.CreateBounded<ReceivedMessage>(
                    new BoundedChannelOptions(ConsumerOptions.BatchSize!.Value)
                    {
                        FullMode = ConsumerOptions.BehaviorWhenFull!.Value
                    });

                await Task.Yield();
                var success = false;
                while (!success)
                {
                    _logger.LogTrace(Consumers.StartingConsumerLoop, ConsumerOptions.ConsumerName);
                    success = await StartConsumingAsync().ConfigureAwait(false);
                }

                _logger.LogDebug(Consumers.Started, ConsumerOptions.ConsumerName);

                Started = true;
            }
        }
        finally { _conLock.Release(); }
    }

    public async Task StopConsumerAsync(bool immediate = false)
    {
        await _conLock.WaitAsync();

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
                else
                {
                    await _consumerChannel
                        .Reader
                        .Completion
                        .ConfigureAwait(false);
                }

                _cts.Cancel();
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

    private CancellationTokenSource _cts;

    private async Task<bool> StartConsumingAsync()
    {
        if (_shutdown) return false;

        _logger.LogInformation(
            Consumers.StartingConsumer,
            ConsumerOptions.ConsumerName);

        var healthy = await _chanHost.ChannelHealthyAsync();
        if (!healthy)
        {
            try
            {
                await _chanHost.WaitUntilChannelIsReadyAsync(
                    Options.PoolOptions.SleepOnErrorInterval,
                    _cts.Token);
            }
            catch (OperationCanceledException)
            { return false; }
        }

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
                ConsumerTag = _chanHost.StartConsuming(_asyncConsumer, ConsumerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Consumers.StartingConsumerError);
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
                ConsumerTag = _chanHost.StartConsuming(_consumer, ConsumerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Consumers.StartingConsumerError);
                return false;
            }
        }

        _cts = new CancellationTokenSource();
        _logger.LogInformation(
            Consumers.StartedConsumer,
            ConsumerOptions.ConsumerName);

        return true;
    }

    protected virtual async Task SetChannelHostAsync()
    {
        var autoAck = ConsumerOptions.AutoAck ?? false;
        _logger.LogTrace(Consumers.GettingTransientChannelHost, ConsumerOptions.ConsumerName);

        _chanHost = await ChannelPool
            .GetTransientChannelAsync(!autoAck)
            .ConfigureAwait(false);

        _logger.LogDebug(
            Consumers.ChannelEstablished,
            ConsumerOptions.ConsumerName,
            _chanHost?.ChannelId.ToString() ?? "ChannelHost: null");
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
            Consumers.ConsumerMessageReceived,
            ConsumerOptions.ConsumerName,
            bdea.DeliveryTag);

        await HandleMessageAsync(bdea).ConfigureAwait(false);
    }

    private async void ConsumerShutdown(object sender, ShutdownEventArgs e)
    {
        if (await _conLock.WaitAsync(0))
        {
            try
            {
                if (!_shutdown)
                {
                    await HandleRecoverableShutdownAsync(e)
                        .ConfigureAwait(false);
                }
                else
                { await _chanHost.StopConsumingAsync(); }
            }
            finally
            { _conLock.Release(); }
        }
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
            Consumers.ConsumerAsyncMessageReceived,
            ConsumerOptions.ConsumerName,
            bdea.DeliveryTag);

        await HandleMessageAsync(bdea).ConfigureAwait(false);
    }

    protected virtual async ValueTask<bool> HandleMessageAsync(BasicDeliverEventArgs bdea)
    {
        if (!await _consumerChannel.Writer.WaitToWriteAsync().ConfigureAwait(false)) return false;

        try
        {
            await _consumerChannel
                .Writer
                .WriteAsync(new ReceivedMessage(_chanHost.GetChannel(), bdea, !(ConsumerOptions.AutoAck ?? false)))
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

    protected async Task ConsumerShutdownAsync(object sender, ShutdownEventArgs e)
    {
        if (await _conLock.WaitAsync(0))
        {
            try
            {
                if (!_shutdown)
                {
                    _logger.LogInformation(
                        Consumers.ConsumerShutdownEvent,
                        ConsumerOptions.ConsumerName,
                        _chanHost.ChannelId,
                        e.ReplyText);

                    await HandleRecoverableShutdownAsync(e)
                        .ConfigureAwait(false);
                }
                else
                { await _chanHost.StopConsumingAsync(); }
            }
            finally
            { _conLock.Release(); }
        }
    }

    private static readonly string _consumerShutdownExceptionMessage = "Consumer's ChannelHost {0} had an unhandled exception during recovery.";

    /// <summary>
    /// This method used to rebuild channels/connections for Consumers. Due to recent
    /// changes in RabbitMQ.Client, it is now possible for the consumer to be in a state
    /// of self-recovery. Unfortunately, there are still some edge cases where the channel
    /// has exception and is closed server side and this library needs to be able to recover
    /// from those events.
    /// </summary>
    /// <para>Docs: https://www.rabbitmq.com/client-libraries/dotnet-api-guide#recovery</para>
    /// <param name="e"></param>
    /// <returns></returns>
    protected virtual async Task HandleRecoverableShutdownAsync(ShutdownEventArgs e)
    {
        try
        {
            await _chanHost
                .WaitUntilChannelIsReadyAsync(Options.PoolOptions.SleepOnErrorInterval, _cts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        { return; }
        catch (Exception ex)
        {
            _logger.LogError(ex, _consumerShutdownExceptionMessage);
        }

        _logger.LogInformation(
            Consumers.ConsumerShutdownEventFinished,
            ConsumerOptions.ConsumerName,
            _chanHost.ChannelId);
    }

    public ChannelReader<ReceivedMessage> GetConsumerBuffer() => _consumerChannel.Reader;

    public async ValueTask<ReceivedMessage> ReadAsync()
    {
        if (!await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false)) throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);

        return await _consumerChannel
            .Reader
            .ReadAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<ReceivedMessage>> ReadUntilEmptyAsync()
    {
        if (!await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false)) throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);

        var list = new List<ReceivedMessage>();
        await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false);
        while (_consumerChannel.Reader.TryRead(out var message))
        {
            if (message == null) { break; }
            list.Add(message);
        }

        return list;
    }

    public async IAsyncEnumerable<ReceivedMessage> StreamUntilQueueEmptyAsync()
    {
        if (!await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false)) throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);

        await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false);
        while (_consumerChannel.Reader.TryRead(out var message))
        {
            if (message == null) { break; }
            yield return message;
        }
    }

    public async IAsyncEnumerable<ReceivedMessage> StreamUntilConsumerStopAsync()
    {
        if (!await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false)) throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);

        await foreach (var receivedData in _consumerChannel.Reader.ReadAllAsync())
        {
            yield return receivedData;
        }
    }

    public async Task DataflowExecutionEngineAsync(
        Func<ReceivedMessage, Task<bool>> workBodyAsync,
        int maxDoP = 4,
        bool ensureOrdered = true,
        int boundedCapacity = 1000,
        TaskScheduler taskScheduler = null,
        CancellationToken token = default)
    {
        var dataflowEngine = new DataflowEngine<ReceivedMessage, bool>(workBodyAsync, maxDoP, ensureOrdered, null, null, boundedCapacity, taskScheduler);

        await TransferDataToDataflowEngine(dataflowEngine, token);
    }

    public async Task DataflowExecutionEngineAsync(
        Func<ReceivedMessage, Task<bool>> workBodyAsync,
        int maxDoP = 4,
        bool ensureOrdered = true,
        Func<ReceivedMessage, Task<ReceivedMessage>> preWorkBodyAsync = null,
        Func<bool, Task> postWorkBodyAsync = null,
        int boundedCapacity = 1000,
        TaskScheduler taskScheduler = null,
        CancellationToken token = default)
    {
        var dataflowEngine = new DataflowEngine<ReceivedMessage, bool>(
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
        DataflowEngine<ReceivedMessage, bool> dataflowEngine,
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
        Func<ReceivedMessage, Task<bool>> workBodyAsync,
        int maxDoP = 4,
        bool ensureOrdered = true,
        Func<bool, Task> postWorkBodyAsync = null,
        int boundedCapacity = 1000,
        TaskScheduler taskScheduler = null,
        CancellationToken token = default)
    {
        var channelBlockEngine = new ChannelBlockEngine<ReceivedMessage, bool>(
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
        Func<ReceivedMessage, Task<bool>> workBodyAsync,
        int maxDoP = 4,
        bool ensureOrdered = true,
        TaskScheduler taskScheduler = null,
        CancellationToken token = default)
    {
        _ = new ChannelBlockEngine<ReceivedMessage, bool>(
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
        ChannelBlockEngine<ReceivedMessage, bool> channelBlockEngine,
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
