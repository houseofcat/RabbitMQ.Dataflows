using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using HouseofCat.Utilities.Helpers;
using HouseofCat.Utilities.Json;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ;

public interface IConsumer<TFromQueue>
{
    IChannelPool ChannelPool { get; }
    RabbitOptions Options { get; }
    ConsumerOptions ConsumerOptions { get; }
    bool Started { get; }

    ChannelReader<TFromQueue> GetConsumerBuffer();
    ValueTask<TFromQueue> ReadAsync();
    Task StartConsumerAsync();
    Task StopConsumerAsync(bool immediate = false);

    Task<IEnumerable<TFromQueue>> ReadUntilEmptyAsync(CancellationToken token = default);
    ValueTask<IAsyncEnumerable<IReceivedMessage>> ReadUntilStopAsync(CancellationToken token = default);
}

public class Consumer : IConsumer<IReceivedMessage>, IDisposable
{
    private readonly ILogger<Consumer> _logger;
    protected readonly SemaphoreSlim _conLock = new SemaphoreSlim(1, 1);
    protected IChannelHost _chanHost;
    protected bool _disposedValue;
    protected Channel<IReceivedMessage> _consumerChannel;

    public string ConsumerTag { get; private set; }
    protected bool _shutdown;

    public RabbitOptions Options { get; }
    public ConsumerOptions ConsumerOptions { get; }

    public IChannelPool ChannelPool { get; }
    public bool Started { get; private set; }

    public Consumer(
        RabbitOptions options,
        string consumerName,
        JsonSerializerOptions jsonOptions = null)
        : this(new ChannelPool(options), consumerName, jsonOptions)
    { }

    public Consumer(
        IChannelPool channelPool,
        string consumerName,
        JsonSerializerOptions jsonOptions = null)
        : this(channelPool, channelPool.Options.GetConsumerOptions(consumerName), jsonOptions)
    {
        Guard.AgainstNull(channelPool, nameof(channelPool));
        Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));
    }

    public Consumer(
        IChannelPool channelPool,
        ConsumerOptions consumerOptions,
        JsonSerializerOptions jsonOptions = null)
    {
        Guard.AgainstNull(channelPool, nameof(channelPool));
        Guard.AgainstNull(consumerOptions, nameof(consumerOptions));

        _logger = LogHelpers.GetLogger<Consumer>();
        Options = channelPool.Options;
        ChannelPool = channelPool;
        ConsumerOptions = consumerOptions;

        _defaultOptions = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        _defaultOptions.Converters.Add(new FlexibleObjectJsonConverter());
    }

    private static readonly string _startingConsumerLoop = "Consumer [{0}] startup loop executing...";

    public async Task StartConsumerAsync()
    {
        if (!ConsumerOptions.Enabled) return;
        if (!await _conLock.WaitAsync(0).ConfigureAwait(false)) return;

        try
        {
            if (Started) return;

            await SetChannelHostAsync().ConfigureAwait(false);
            _shutdown = false;
            _consumerChannel = Channel.CreateBounded<IReceivedMessage>(
                new BoundedChannelOptions(ConsumerOptions.BatchSize)
                {
                    FullMode = ConsumerOptions.BehaviorWhenFull.Value
                });

            var success = false;
            while (!success)
            {
                _logger.LogTrace(_startingConsumerLoop, ConsumerOptions.ConsumerName);
                success = await StartConsumingAsync().ConfigureAwait(false);
                if (!success)
                { await Task.Delay(Options.PoolOptions.SleepOnErrorInterval); }
            }

            Started = true;
        }
        finally { _conLock.Release(); }
    }

    private static readonly string _stopConsumer = "Consumer [{0}] stop consuming called...";
    private static readonly string _stoppedConsumer = "Consumer [{0}] stopped consuming.";

    public async Task StopConsumerAsync(bool immediate = false)
    {
        if (!await _conLock.WaitAsync(0).ConfigureAwait(false)) return;

        try
        {
            if (!Started) return;

            _shutdown = true;
            _logger.LogInformation(_stopConsumer, ConsumerOptions.ConsumerName);

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
            _logger.LogInformation(
                _stoppedConsumer,
                ConsumerOptions.ConsumerName);
        }
        finally { _conLock.Release(); }
    }

    protected AsyncEventingBasicConsumer _asyncConsumer;
    protected EventingBasicConsumer _consumer;
    protected CancellationTokenSource _cts;

    private static readonly string _startingConsumer = "Consumer [{0}] starting...";
    private static readonly string _startingConsumerError = "Exception creating internal RabbitMQ consumer. Retrying...";
    private static readonly string _startedConsumer = "Consumer [{0}] started.";

    protected async Task<bool> StartConsumingAsync()
    {
        if (_shutdown) return false;

        _logger.LogInformation(
            _startingConsumer,
            ConsumerOptions.ConsumerName);

        if (!_chanHost.ChannelHealthy())
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

        if (Options.PoolOptions.EnableDispatchConsumersAsync)
        {
            if (_asyncConsumer is not null) // Cleanup operation, this prevents an EventHandler leak.
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
                _logger.LogError(ex, _startingConsumerError);
                return false;
            }
        }
        else
        {
            if (_consumer is not null) // Cleanup operation, this prevents an EventHandler leak.
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
                _logger.LogError(ex, _startingConsumerError);
                return false;
            }
        }

        _cts = new CancellationTokenSource();
        _logger.LogInformation(
            _startedConsumer,
            ConsumerOptions.ConsumerName);

        return true;
    }

    private static readonly string _gettingTransientChannelHost = "Consumer [{0}] getting a transient channel.";
    private static readonly string _channelEstablished = "Consumer [{0}] channel host [{1}] assigned.";


    protected virtual async Task SetChannelHostAsync()
    {
        _logger.LogTrace(_gettingTransientChannelHost, ConsumerOptions.ConsumerName);

        _chanHost = await ChannelPool
            .GetTransientChannelAsync(!ConsumerOptions.AutoAck)
            .ConfigureAwait(false);

        _logger.LogDebug(
            _channelEstablished,
            ConsumerOptions.ConsumerName,
            _chanHost?.ChannelId.ToString() ?? "ChannelHost: null");
    }

    private EventingBasicConsumer CreateConsumer()
    {
        EventingBasicConsumer consumer = null;

        _chanHost.Channel.BasicQos(0, ConsumerOptions.BatchSize, false);
        consumer = new EventingBasicConsumer(_chanHost.Channel);

        consumer.Received += ReceiveHandler;
        consumer.Shutdown += ConsumerShutdown;

        return consumer;
    }

    private static readonly string _consumerMessageReceived = "Consumer [{0}] message received (DT:{1}]. Adding to buffer...";

    protected virtual async void ReceiveHandler(object _, BasicDeliverEventArgs bdea)
    {
        _logger.LogDebug(
            _consumerMessageReceived,
            ConsumerOptions.ConsumerName,
            bdea.DeliveryTag);

        await HandleMessageAsync(bdea).ConfigureAwait(false);
    }

    protected async void ConsumerShutdown(object sender, ShutdownEventArgs e)
    {
        if (!await _conLock.WaitAsync(0).ConfigureAwait(false)) return;

        try
        {
            if (!_shutdown)
            {
                await HandleRecoverableShutdownAsync(e)
                    .ConfigureAwait(false);
            }
            else
            { _chanHost.StopConsuming(); }
        }
        finally
        { _conLock.Release(); }
    }

    protected AsyncEventingBasicConsumer CreateAsyncConsumer()
    {
        AsyncEventingBasicConsumer consumer = null;

        _chanHost.Channel.BasicQos(0, ConsumerOptions.BatchSize, false);
        consumer = new AsyncEventingBasicConsumer(_chanHost.Channel);

        consumer.Received += ReceiveHandlerAsync;
        consumer.Shutdown += ConsumerShutdownAsync;

        return consumer;
    }

    protected virtual async Task ReceiveHandlerAsync(object _, BasicDeliverEventArgs bdea)
    {
        _logger.LogDebug(
            _consumerMessageReceived,
            ConsumerOptions.ConsumerName,
            bdea.DeliveryTag);

        await HandleMessageAsync(bdea).ConfigureAwait(false);
    }

    private static readonly string _consumerMessageWriteToBufferError = "Consumer [{0}] was unable to write to channel buffer. Error: {1}";

    private static readonly string _consumerSpanNameFormat = "messaging.rabbitmq.consumer receive";

    protected virtual async ValueTask<bool> HandleMessageAsync(BasicDeliverEventArgs bdea)
    {
        if (!await _consumerChannel.Writer.WaitToWriteAsync().ConfigureAwait(false)) return false;

        try
        {
            var receivedMessage = new ReceivedMessage(_chanHost.Channel, bdea, !ConsumerOptions.AutoAck);
            using var span = OpenTelemetryHelpers.StartActiveSpan(
                _consumerSpanNameFormat,
                SpanKind.Consumer,
                receivedMessage.ParentSpanContext ?? default);

            EnrichSpanWithTags(span, receivedMessage);

            receivedMessage.ParentSpanContext = span?.Context;

            AutoDeserialize(receivedMessage);

            await _consumerChannel
                .Writer
                .WriteAsync(receivedMessage)
                .ConfigureAwait(false);

            span.End();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                _consumerMessageWriteToBufferError,
                ConsumerOptions.ConsumerName,
                ex.Message);
            return false;
        }
    }

    protected void EnrichSpanWithTags(TelemetrySpan span, IReceivedMessage receivedMessage)
    {
        if (span is null || !span.IsRecording) return;

        span.SetAttribute(Constants.MessagingSystemKey, Constants.MessagingSystemValue);

        if (!string.IsNullOrEmpty(receivedMessage?.Message?.MessageId))
        {
            span.SetAttribute(Constants.MessagingMessageMessageIdKey, receivedMessage?.Message.MessageId);
        }
        if (!string.IsNullOrEmpty(ConsumerOptions.ConsumerName))
        {
            span.SetAttribute(Constants.MessagingConsumerNameKey, ConsumerOptions.ConsumerName);
        }
        if (!string.IsNullOrEmpty(ConsumerOptions.QueueName))
        {
            span.SetAttribute(Constants.MessagingMessageRoutingKeyKey, ConsumerOptions.QueueName);
        }

        if (!string.IsNullOrEmpty(receivedMessage?.Message?.Metadata?.PayloadId))
        {
            span.SetAttribute(Constants.MessagingMessagePayloadIdKey, receivedMessage?.Message?.Metadata?.PayloadId);
        }

        var encrypted = receivedMessage?.Message?.Metadata?.Encrypted();
        if (encrypted.HasValue && encrypted.Value)
        {
            span.SetAttribute(Constants.MessagingMessageEncryptedKey, "true");
            span.SetAttribute(Constants.MessagingMessageEncryptedDateKey, receivedMessage?.Message?.Metadata?.EncryptedDate());
            span.SetAttribute(Constants.MessagingMessageEncryptionKey, receivedMessage?.Message?.Metadata?.EncryptionType());
        }
        var compressed = receivedMessage?.Message?.Metadata?.Compressed();
        if (compressed.HasValue && compressed.Value)
        {
            span.SetAttribute(Constants.MessagingMessageCompressedKey, "true");
            span.SetAttribute(Constants.MessagingMessageCompressionKey, receivedMessage?.Message?.Metadata?.CompressionType());
        }
    }

    protected JsonSerializerOptions _defaultOptions;

    protected virtual void AutoDeserialize(ReceivedMessage receivedMessage)
    {
        if (receivedMessage.ObjectType == Constants.HeaderValueForMessageObjectType
            && receivedMessage.Body.Length > 0)
        {
            switch (receivedMessage.Properties.ContentType)
            {
                case Constants.HeaderValueForContentTypeJson:
                    try
                    {
                        receivedMessage.Message = JsonSerializer.Deserialize<Message>(receivedMessage.Body.Span, _defaultOptions);
                    }
                    catch
                    { receivedMessage.FailedToDeserialize = true; }
                    break;
                case Constants.HeaderValueForContentTypeMessagePack:
                    try
                    {
                        receivedMessage.Message = MessagePackProvider.GlobalDeserialize<Message>(receivedMessage.Body);
                    }
                    catch
                    { receivedMessage.FailedToDeserialize = true; }
                    break;
                case Constants.HeaderValueForContentTypeBinary:
                case Constants.HeaderValueForContentTypePlainText:
                default:
                    break;
            }
        }
    }

    private static readonly string _consumerShutdownEvent = "Consumer [{0}] recoverable shutdown event has occurred on Channel [Id: {1}]. Reason: {2}. Attempting to restart consuming...";

    protected async Task ConsumerShutdownAsync(object sender, ShutdownEventArgs e)
    {
        if (!await _conLock.WaitAsync(0).ConfigureAwait(false)) return;

        try
        {
            if (!_shutdown)
            {
                _logger.LogInformation(
                    _consumerShutdownEvent,
                    ConsumerOptions.ConsumerName,
                    _chanHost.ChannelId,
                    e.ReplyText);

                await HandleRecoverableShutdownAsync(e)
                    .ConfigureAwait(false);
            }
            else
            { _chanHost.StopConsuming(); }
        }
        finally
        { _conLock.Release(); }
    }

    private static readonly string _consumerShutdownExceptionMessage = "Consumer's ChannelHost {0} had an unhandled exception during recovery.";
    private static readonly string _consumerShutdownEventFinished = "Consumer [{0}] shutdown event has finished on Channel [Id: {1}].";

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
            _consumerShutdownEventFinished,
            ConsumerOptions.ConsumerName,
            _chanHost.ChannelId);
    }

    public ChannelReader<IReceivedMessage> GetConsumerBuffer() => _consumerChannel.Reader;

    public async ValueTask<IReceivedMessage> ReadAsync()
    {
        if (!await _consumerChannel.Reader.WaitToReadAsync().ConfigureAwait(false)) throw new InvalidOperationException();

        return await _consumerChannel
            .Reader
            .ReadAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<IReceivedMessage>> ReadUntilEmptyAsync(CancellationToken token = default)
    {
        if (!await _consumerChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false)) throw new InvalidOperationException();

        var list = new List<IReceivedMessage>();
        await _consumerChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false);
        while (_consumerChannel.Reader.TryRead(out var message))
        {
            if (message is null) { break; }
            list.Add(message);
        }

        return list;
    }

    public async ValueTask<IAsyncEnumerable<IReceivedMessage>> ReadUntilStopAsync(CancellationToken token = default)
    {
        if (!await _consumerChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false)) throw new InvalidOperationException();

        return _consumerChannel.Reader.ReadAllAsync(token);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue) return;

        if (disposing)
        {
            _conLock.Dispose();
        }

        _consumerChannel = null;
        _chanHost = null;
        _disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
