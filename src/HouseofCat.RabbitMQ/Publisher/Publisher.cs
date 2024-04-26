using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ;

public interface IPublisher
{
    bool AutoPublisherStarted { get; }
    RabbitOptions Options { get; }

    string TimeFormat { get; set; }

    void StartAutoPublish(Func<IPublishReceipt, ValueTask> processReceiptAsync = null);
    Task StartAutoPublishAsync(Func<IPublishReceipt, ValueTask> processReceiptAsync = null);
    Task StopAutoPublishAsync(bool immediately = false);

    void QueueMessage(IMessage message);
    ValueTask QueueMessageAsync(IMessage message);

    ChannelReader<IPublishReceipt> GetReceiptBufferReader();
    Task PublishAsync(IMessage message, bool createReceipt, bool withOptionalHeaders = true);
    Task PublishWithConfirmationAsync(IMessage message, bool createReceipt, bool withOptionalHeaders = true);

    Task PublishManyAsBatchAsync(IList<IMessage> messages, bool createReceipt, bool withOptionalHeaders = true);
    Task PublishManyAsync(IList<IMessage> messages, bool createReceipt, bool withOptionalHeaders = true);

    Task<bool> PublishAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> body,
        bool mandatory = false,
        IBasicProperties basicProperties = null,
        string messageId = null,
        string contentType = null);

    Task<bool> PublishAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> body,
        IDictionary<string, object> headers = null,
        string messageId = null,
        byte? priority = 0,
        byte? deliveryMode = 2,
        bool mandatory = false,
        string contentType = null);

    Task<bool> PublishBatchAsync(
        string exchangeName,
        string routingKey,
        IList<ReadOnlyMemory<byte>> bodies,
        bool mandatory = false,
        IBasicProperties basicProperties = null,
        string contentType = null);

    Task<bool> PublishBatchAsync(
        string exchangeName,
        string routingKey,
        IList<ReadOnlyMemory<byte>> bodies,
        IDictionary<string, object> headers = null,
        byte? priority = 0,
        byte? deliveryMode = 2,
        bool mandatory = false,
        string contentType = null);
}

public class Publisher : IPublisher, IDisposable
{
    public RabbitOptions Options { get; }
    public bool AutoPublisherStarted { get; private set; }

    private readonly ILogger<Publisher> _logger;
    private readonly IChannelPool _channelPool;
    private readonly SemaphoreSlim _pubLock = new SemaphoreSlim(1, 1);

    private readonly ISerializationProvider _serializationProvider;
    private readonly IEncryptionProvider _encryptionProvider;
    private readonly ICompressionProvider _compressionProvider;

    private readonly TimeSpan _waitForConfirmation;

    private Channel<IMessage> _messageQueue;
    private Channel<IPublishReceipt> _receiptBuffer;

    private Task _publishingTask;
    private Task _processReceiptsAsync;
    private bool _disposedValue;

    public string TimeFormat { get; set; } = TimeHelpers.Formats.RFC3339Long;

    public Publisher(
        RabbitOptions options,
        ISerializationProvider serializationProvider,
        IEncryptionProvider encryptionProvider = null,
        ICompressionProvider compressionProvider = null)
        : this(
              new ChannelPool(options),
              serializationProvider,
              encryptionProvider,
              compressionProvider)
    { }

    public Publisher(
        IChannelPool channelPool,
        ISerializationProvider serializationProvider,
        IEncryptionProvider encryptionProvider = null,
        ICompressionProvider compressionProvider = null)
    {
        Guard.AgainstNull(channelPool, nameof(channelPool));
        Guard.AgainstNull(serializationProvider, nameof(serializationProvider));

        Options = channelPool.Options;
        _logger = LogHelpers.GetLogger<Publisher>();
        _serializationProvider = serializationProvider;

        if (Options.PublisherOptions.Encrypt && encryptionProvider == null)
        {
            Options.PublisherOptions.Encrypt = false;
            _logger.LogWarning("Encryption disabled, encryptionProvider provided was null.");
        }
        else if (Options.PublisherOptions.Encrypt)
        {
            _encryptionProvider = encryptionProvider;
        }

        if (Options.PublisherOptions.Compress && compressionProvider == null)
        {
            Options.PublisherOptions.Compress = false;
            _logger.LogWarning("Compression disabled, compressionProvider provided was null.");
        }
        else if (Options.PublisherOptions.Compress)
        {
            _compressionProvider = compressionProvider;
        }

        _channelPool = channelPool;
        _receiptBuffer = Channel.CreateBounded<IPublishReceipt>(
            new BoundedChannelOptions(100)
            {
                SingleWriter = false,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.DropOldest, // never block
            });

        _waitForConfirmation = TimeSpan.FromMilliseconds(Options.PublisherOptions.WaitForConfirmationTimeoutInMilliseconds);
    }

    public ChannelReader<IPublishReceipt> GetReceiptBufferReader()
    {
        return _receiptBuffer.Reader;
    }

    #region AutoPublisher

    public void StartAutoPublish(Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
    {
        if (!_pubLock.Wait(0)) return;

        try { SetupAutoPublisher(processReceiptAsync); }
        finally { _pubLock.Release(); }
    }

    public async Task StartAutoPublishAsync(Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
    {
        if (!await _pubLock.WaitAsync(0).ConfigureAwait(false)) return;

        try { SetupAutoPublisher(processReceiptAsync); }
        finally { _pubLock.Release(); }
    }

    private void SetupAutoPublisher(Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
    {
        if (AutoPublisherStarted) return;

        _messageQueue = Channel.CreateBounded<IMessage>(
            new BoundedChannelOptions(Options.PublisherOptions.MessageQueueBufferSize)
            {
                FullMode = Options.PublisherOptions.BehaviorWhenFull
            });

        _publishingTask = ProcessMessagesAsync(_messageQueue.Reader);

        if (Options.PublisherOptions.CreatePublishReceipts)
        {
            processReceiptAsync ??= ProcessReceiptAsync;
            _processReceiptsAsync ??= ProcessReceiptsAsync(processReceiptAsync);
        }

        AutoPublisherStarted = true;
    }

    public async Task StopAutoPublishAsync(bool immediately = false)
    {
        if (!await _pubLock.WaitAsync(0).ConfigureAwait(false)) return;

        try
        {
            if (AutoPublisherStarted)
            {
                _messageQueue.Writer.Complete();

                if (!immediately)
                {
                    await _messageQueue
                        .Reader
                        .Completion
                        .ConfigureAwait(false);

                    // Wait for Publishing To Finish.
                    while (!_publishingTask.IsCompleted)
                    {
                        await Task.Delay(10).ConfigureAwait(false);
                    }
                }

                AutoPublisherStarted = false;
            }
        }
        finally
        { _pubLock.Release(); }
    }

    private static readonly string _autoPublisherNotStartedError = "AutoPublisher has not been started.";
    private static readonly string _messageQueued = "AutoPublisher queued message [MessageId:{0} InternalId:{1}].";

    public void QueueMessage(IMessage message)
    {
        if (!AutoPublisherStarted) throw new InvalidOperationException(_autoPublisherNotStartedError);
        Guard.AgainstNull(message, nameof(message));

        _logger.LogDebug(_messageQueued, message.MessageId, message.Metadata?.PayloadId);

        _messageQueue.Writer.TryWrite(message);
    }

    private static readonly string _queueChannelError = "Can't queue a message to a closed Threading.Channel.";

    public async ValueTask QueueMessageAsync(IMessage message)
    {
        if (!AutoPublisherStarted) throw new InvalidOperationException(_autoPublisherNotStartedError);
        Guard.AgainstNull(message, nameof(message));

        if (!await _messageQueue
             .Writer
             .WaitToWriteAsync()
             .ConfigureAwait(false))
        {
            throw new InvalidOperationException(_queueChannelError);
        }

        _logger.LogDebug(_messageQueued, message.MessageId, message.Metadata?.PayloadId);

        await _messageQueue
            .Writer
            .WriteAsync(message)
            .ConfigureAwait(false);
    }

    private static readonly string _defaultAutoPublisherSpanName = "messaging.rabbitmq.autopublisher process";
    private static readonly string _compressEventName = "compressed";
    private static readonly string _encryptEventName = "encrypted";
    private static readonly string _messagePublished = "AutoPublisher published message [MessageId:{0} InternalId:{1}]. Listen for receipt to indicate success...";

    private async Task ProcessMessagesAsync(ChannelReader<IMessage> channelReader)
    {
        await Task.Yield();
        while (await channelReader.WaitToReadAsync().ConfigureAwait(false))
        {
            while (channelReader.TryRead(out var message))
            {
                if (message == null)
                { continue; }

                using var span = OpenTelemetryHelpers.StartActiveSpan(
                    _defaultAutoPublisherSpanName,
                    SpanKind.Internal,
                    message.ParentSpanContext ?? default);

                message.Metadata ??= new Metadata();

                // If parent span context is not set, set it to the current span.
                if (message.ParentSpanContext == default)
                {
                    message.ParentSpanContext = span.Context;
                }

                if (Options.PublisherOptions.Compress)
                {
                    message.Body = _compressionProvider.Compress(message.Body).ToArray();
                    message.Metadata.Fields[Constants.HeaderForCompressed] = true;
                    message.Metadata.Fields[Constants.HeaderForCompression] = _compressionProvider.Type;
                    span?.AddEvent(_compressEventName);
                }

                if (Options.PublisherOptions.Encrypt)
                {
                    message.Body = _encryptionProvider.Encrypt(message.Body).ToArray();
                    message.Metadata.Fields[Constants.HeaderForEncrypted] = true;
                    message.Metadata.Fields[Constants.HeaderForEncryption] = _encryptionProvider.Type;
                    message.Metadata.Fields[Constants.HeaderForEncryptDate] = TimeHelpers.GetDateTimeNow(TimeHelpers.Formats.RFC3339Long);
                    span?.AddEvent(_encryptEventName);
                }

                _logger.LogDebug(_messagePublished, message.MessageId, message.Metadata?.PayloadId);

                await PublishAsync(message, Options.PublisherOptions.CreatePublishReceipts, Options.PublisherOptions.WithHeaders)
                    .ConfigureAwait(false);

                span.End();
            }
        }
    }

    private async Task ProcessReceiptsAsync(Func<IPublishReceipt, ValueTask> processReceiptAsync)
    {
        await Task.Yield();
        await foreach (var receipt in _receiptBuffer.Reader.ReadAllAsync())
        {
            await processReceiptAsync(receipt).ConfigureAwait(false);
        }
    }

    private async ValueTask ProcessReceiptAsync(IPublishReceipt receipt)
    {
        if (AutoPublisherStarted
            && receipt.IsError
            && receipt.OriginalMessage != null)
        {
            _logger.LogWarning($"Failed publish for message ({receipt.OriginalMessage.MessageId}). Retrying with AutoPublishing...");

            try
            { await QueueMessageAsync(receipt.OriginalMessage); }
            catch (Exception ex) /* No-op */
            { _logger.LogDebug("Error ({0}) occurred on retry, most likely because retry during shutdown.", ex.Message); }
        }
        else if (receipt.IsError)
        {
            _logger.LogError($"Failed publish for message ({receipt.OriginalMessage.MessageId}). Unable to retry as the original message was not received.");
        }
    }

    #endregion

    #region Publishing

    private static readonly string _publishFailed = "Publish to route [{0}] failed, flagging channel host. Error: {1}";

    public async Task<bool> PublishAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> body,
        bool mandatory = false,
        IBasicProperties basicProperties = null,
        string messageId = null,
        string contentType = null)
    {
        Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));

        using var span = OpenTelemetryHelpers.StartActiveSpan(nameof(PublishAsync), SpanKind.Producer);
        EnrichSpanWithTags(span, exchangeName, routingKey, messageId);

        var error = false;
        var channelHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);
        if (basicProperties == null)
        {
            basicProperties = channelHost.Channel.CreateBasicProperties();
            basicProperties.DeliveryMode = 2;
            basicProperties.MessageId = messageId ?? Guid.NewGuid().ToString();

            if (!basicProperties.IsHeadersPresent())
            {
                basicProperties.Headers = new Dictionary<string, object>();
            }
        }

        SetMandatoryHeaders(basicProperties, contentType);

        try
        {
            channelHost
                .Channel
                .BasicPublish(
                    exchange: exchangeName ?? string.Empty,
                    routingKey: routingKey,
                    mandatory: mandatory,
                    basicProperties: basicProperties,
                    body: body);
        }
        catch (Exception ex)
        {
            OpenTelemetryHelpers.SetSpanAsError(span, ex);
            _logger.LogDebug(_publishFailed, $"{exchangeName}->{routingKey}", ex.Message);
            error = true;
        }
        finally
        {
            span.End();
            await _channelPool
                .ReturnChannelAsync(channelHost, error);
        }

        return error;
    }

    // A basic implementation of publish but using the ChannelPool. If headers are provided and start with "x-", they get included in the message properties.
    public async Task<bool> PublishAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> body,
        IDictionary<string, object> headers = null,
        string messageId = null,
        byte? priority = 0,
        byte? deliveryMode = 2,
        bool mandatory = false,
        string contentType = null)
    {
        Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));

        using var span = OpenTelemetryHelpers.StartActiveSpan(nameof(PublishAsync), SpanKind.Producer);
        EnrichSpanWithTags(span, exchangeName, routingKey);

        var error = false;
        var channelHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

        try
        {
            channelHost
                .Channel
                .BasicPublish(
                    exchange: exchangeName ?? string.Empty,
                    routingKey: routingKey,
                    mandatory: mandatory,
                    basicProperties: BuildProperties(headers, channelHost, messageId, priority, deliveryMode, contentType),
                    body: body);
        }
        catch (Exception ex)
        {
            OpenTelemetryHelpers.SetSpanAsError(span, ex);
            _logger.LogDebug(
                _publishFailed,
                $"{exchangeName}->{routingKey}",
                ex.Message);

            error = true;
        }
        finally
        {
            span.End();
            await _channelPool
                .ReturnChannelAsync(channelHost, error);
        }

        return error;
    }

    // A basic implementation of publishing batches but using the ChannelPool. If message properties is null, one is created and all messages are set to persistent.
    public async Task<bool> PublishBatchAsync(
        string exchangeName,
        string routingKey,
        IList<ReadOnlyMemory<byte>> bodies,
        bool mandatory = false,
        IBasicProperties basicProperties = null,
        string contentType = null)
    {
        Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));
        Guard.AgainstNullOrEmpty(bodies, nameof(bodies));

        using var span = OpenTelemetryHelpers.StartActiveSpan(nameof(PublishBatchAsync), SpanKind.Producer);

        var error = false;
        var channelHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);
        if (basicProperties == null)
        {
            basicProperties = channelHost.Channel.CreateBasicProperties();
            basicProperties.DeliveryMode = 2;
            basicProperties.MessageId = Guid.NewGuid().ToString();

            if (!basicProperties.IsHeadersPresent())
            {
                basicProperties.Headers = new Dictionary<string, object>();
            }
        }

        // Non-optional Header.
        SetMandatoryHeaders(basicProperties, contentType);

        try
        {
            var batch = channelHost.Channel.CreateBasicPublishBatch();

            for (var i = 0; i < bodies.Count; i++)
            {
                using var innerSpan = OpenTelemetryHelpers.StartActiveSpan("IBasicPublishBatch.Add", SpanKind.Producer);
                EnrichSpanWithTags(span, exchangeName, routingKey);
                batch.Add(exchangeName, routingKey, mandatory, basicProperties, bodies[i]);
            }

            batch.Publish();
        }
        catch (Exception ex)
        {
            OpenTelemetryHelpers.SetSpanAsError(span, ex);
            _logger.LogDebug(
                _publishFailed,
                $"{exchangeName}->{routingKey}",
                ex.Message);

            error = true;
        }
        finally
        {
            span.End();
            await _channelPool
                .ReturnChannelAsync(channelHost, error);
        }

        return error;
    }

    // A basic implementation of publishing batches but using the ChannelPool. If message properties is null, one is created and all messages are set to persistent.
    public async Task<bool> PublishBatchAsync(
        string exchangeName,
        string routingKey,
        IList<ReadOnlyMemory<byte>> bodies,
        IDictionary<string, object> headers = null,
        byte? priority = 0,
        byte? deliveryMode = 2,
        bool mandatory = false,
        string contentType = null)
    {
        Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));
        Guard.AgainstNullOrEmpty(bodies, nameof(bodies));

        using var span = OpenTelemetryHelpers.StartActiveSpan(nameof(PublishBatchAsync), SpanKind.Producer);
        span.SetAttribute(Constants.MessagingBatchProcessValue, bodies.Count);

        var error = false;
        var channelHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

        try
        {
            var batch = channelHost.Channel.CreateBasicPublishBatch();

            for (var i = 0; i < bodies.Count; i++)
            {
                using var innerSpan = OpenTelemetryHelpers.StartActiveSpan("IBasicPublishBatch.Add", SpanKind.Producer);
                EnrichSpanWithTags(span, exchangeName, routingKey);

                var properties = BuildProperties(headers, channelHost, null, priority, deliveryMode, contentType);
                batch.Add(exchangeName, routingKey, mandatory, properties, bodies[i]);
            }

            batch.Publish();
        }
        catch (Exception ex)
        {
            OpenTelemetryHelpers.SetSpanAsError(span, ex);

            _logger.LogDebug(
                _publishFailed,
                $"{exchangeName}->{routingKey}",
                ex.Message);

            error = true;
        }
        finally
        {
            span.End();
            await _channelPool
                .ReturnChannelAsync(channelHost, error);
        }

        return error;
    }

    private static readonly string _defaultPublishSpanName = "messaging.rabbitmq.publisher publish";
    private static readonly string _publishMessageFailed = "Publish to route [{0}] failed [MessageId: {1}] flagging channel host. Error: {2}";

    /// <summary>
    /// Acquires a channel from the channel pool, then publishes message based on the message parameters.
    /// <para>Only throws exception when failing to acquire channel or when creating a receipt after the ReceiptBuffer is closed.</para>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="createReceipt"></param>
    /// <param name="withOptionalHeaders"></param>
    public async Task PublishAsync(IMessage message, bool createReceipt, bool withOptionalHeaders = true)
    {
        using var span = OpenTelemetryHelpers.StartActiveSpan(
            _defaultPublishSpanName,
            SpanKind.Producer,
            message.ParentSpanContext ?? default);

        message.EnrichSpanWithTags(span);

        var error = false;
        var chanHost = await _channelPool
            .GetChannelAsync()
            .ConfigureAwait(false);

        try
        {
            var body = _serializationProvider.Serialize(message);
            span?.SetAttribute(Constants.MessagingMessageEnvelopeSizeKey, body.Length);
            chanHost
                .Channel
                .BasicPublish(
                    message.Exchange,
                    message.RoutingKey,
                    message.Mandatory,
                    message.BuildProperties(chanHost, withOptionalHeaders, _serializationProvider.ContentType),
                    body);
        }
        catch (Exception ex)
        {
            OpenTelemetryHelpers.SetSpanAsError(span, ex);
            _logger.LogDebug(
                _publishMessageFailed,
                $"{message.Exchange}->{message.RoutingKey}",
                message.MessageId,
                ex.Message);

            error = true;
        }
        finally
        {
            span.End();
            if (createReceipt)
            {
                await CreateReceiptAsync(message, error)
                    .ConfigureAwait(false);
            }

            await _channelPool
                .ReturnChannelAsync(chanHost, error);
        }
    }

    /// <summary>
    /// Acquires an ackable channel from the channel pool, then publishes message based on the message parameters and waits for confirmation.
    /// <para>Only throws exception when failing to acquire channel or when creating a receipt after the ReceiptBuffer is closed.</para>
    /// <para>Not fully ready for production yet.</para>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="createReceipt"></param>
    /// <param name="withOptionalHeaders"></param>
    public async Task PublishWithConfirmationAsync(IMessage message, bool createReceipt, bool withOptionalHeaders = true)
    {
        using var span = OpenTelemetryHelpers.StartActiveSpan(
            _defaultPublishSpanName,
            SpanKind.Producer,
            message.ParentSpanContext ?? default);

        message.EnrichSpanWithTags(span);

        var error = false;
        var chanHost = await _channelPool
            .GetAckChannelAsync()
            .ConfigureAwait(false);

        try
        {
            var body = _serializationProvider.Serialize(message);
            span?.SetAttribute(Constants.MessagingMessageEnvelopeSizeKey, body.Length);

            chanHost
                .Channel
                .BasicPublish(
                    message.Exchange,
                    message.RoutingKey,
                    message.Mandatory,
                    message.BuildProperties(chanHost, withOptionalHeaders, _serializationProvider.ContentType),
                    body);

            chanHost.Channel.WaitForConfirmsOrDie(_waitForConfirmation);
        }
        catch (Exception ex)
        {
            OpenTelemetryHelpers.SetSpanAsError(span, ex);
            _logger.LogDebug(
                _publishMessageFailed,
                $"{message.Exchange}->{message.RoutingKey}",
                message.MessageId,
                ex.Message);

            error = true;
        }
        finally
        {
            span.End();
            if (createReceipt)
            {
                await CreateReceiptAsync(message, error)
                    .ConfigureAwait(false);
            }

            await _channelPool
                .ReturnChannelAsync(chanHost, error);
        }
    }

    private static readonly string _defaultBatchPublishSpanName = "messaging.rabbitmq.publisher batch";

    /// <summary>
    /// Use this method to sequentially publish all messages in a list in the order received.
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="createReceipt"></param>
    /// <param name="withOptionalHeaders"></param>
    public async Task PublishManyAsync(IList<IMessage> messages, bool createReceipt, bool withOptionalHeaders = true)
    {
        using var span = OpenTelemetryHelpers.StartActiveSpan(_defaultBatchPublishSpanName, SpanKind.Producer);
        span?.SetAttribute(Constants.MessagingBatchProcessValue, messages.Count);

        var error = false;
        var chanHost = await _channelPool
            .GetChannelAsync()
            .ConfigureAwait(false);

        for (var i = 0; i < messages.Count; i++)
        {
            try
            {
                using var innerSpan = OpenTelemetryHelpers.StartActiveSpan(
                    _defaultPublishSpanName,
                    SpanKind.Producer,
                    span?.Context ?? default);

                messages[i].EnrichSpanWithTags(innerSpan);

                var body = _serializationProvider.Serialize(messages[i].Body);
                innerSpan?.SetAttribute(Constants.MessagingMessageEnvelopeSizeKey, body.Length);

                chanHost.Channel.BasicPublish(
                    messages[i].Exchange,
                    messages[i].RoutingKey,
                    messages[i].Mandatory,
                    messages[i].BuildProperties(chanHost, withOptionalHeaders, _serializationProvider.ContentType),
                    body);
            }
            catch (Exception ex)
            {
                OpenTelemetryHelpers.SetSpanAsError(span, ex);
                _logger.LogDebug(
                    _publishMessageFailed,
                    $"{messages[i].Exchange}->{messages[i].RoutingKey}",
                    messages[i].MessageId,
                    ex.Message);

                error = true;
            }

            if (createReceipt)
            { await CreateReceiptAsync(messages[i], error).ConfigureAwait(false); }

            if (error) { break; }
        }

        span.End();
        await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false);
    }

    private static readonly string _publishBatchFailed = "Batch publish failed, flagging channel host. Error: {0}";

    /// <summary>
    /// Use this method when a group of messages who have the same properties (deliverymode, messagetype, priority).
    /// <para>Receipt with no error indicates that we successfully handed off to internal library, not necessarily published.</para>
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="createReceipt"></param>
    /// <param name="withOptionalHeaders"></param>
    public async Task PublishManyAsBatchAsync(IList<IMessage> messages, bool createReceipt, bool withOptionalHeaders = true)
    {
        if (messages.Count < 1) return;

        using var span = OpenTelemetryHelpers.StartActiveSpan(nameof(PublishManyAsBatchAsync), SpanKind.Producer);
        span?.SetAttribute(Constants.MessagingBatchProcessValue, messages.Count);

        var error = false;
        var chanHost = await _channelPool
            .GetChannelAsync()
            .ConfigureAwait(false);

        try
        {
            var publishBatch = chanHost.Channel.CreateBasicPublishBatch();
            for (var i = 0; i < messages.Count; i++)
            {
                using var innerSpan = OpenTelemetryHelpers.StartActiveSpan(
                    _defaultPublishSpanName,
                    SpanKind.Producer,
                    span?.Context ?? default);

                messages[i].EnrichSpanWithTags(innerSpan);

                var body = _serializationProvider.Serialize(messages[i].Body);
                innerSpan?.SetAttribute(Constants.MessagingMessageEnvelopeSizeKey, body.Length);

                publishBatch.Add(
                    messages[i].Exchange,
                    messages[i].RoutingKey,
                    messages[i].Mandatory,
                    messages[i].BuildProperties(chanHost, withOptionalHeaders, _serializationProvider.ContentType),
                    body);

                if (createReceipt)
                {
                    await CreateReceiptAsync(messages[i], error).ConfigureAwait(false);
                }
            }

            publishBatch.Publish();
        }
        catch (Exception ex)
        {
            OpenTelemetryHelpers.SetSpanAsError(span, ex);
            _logger.LogDebug(
                _publishBatchFailed,
                ex.Message);

            error = true;
        }
        finally
        {
            span.End();
            await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false);
        }
    }

    private static readonly string _channelReadErrorMessage = "Can't use reader on a closed Threading.Channel.";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask CreateReceiptAsync(IMessage message, bool error)
    {
        if (!await _receiptBuffer
            .Writer
            .WaitToWriteAsync()
            .ConfigureAwait(false))
        {
            throw new InvalidOperationException(_channelReadErrorMessage);
        }

        await _receiptBuffer
            .Writer
            .WriteAsync(message.GetPublishReceipt(error))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Intended to bring feature parity and include properties when publishing byte[], which you get for free when publishing with IMessage objects.
    /// </summary>
    /// <param name="headers"></param>
    /// <param name="channelHost"></param>
    /// <param name="messageId"></param>
    /// <param name="priority"></param>
    /// <param name="deliveryMode"></param>
    /// <returns></returns>
    private static IBasicProperties BuildProperties(
        IDictionary<string, object> headers,
        IChannelHost channelHost,
        string messageId = null,
        byte? priority = 0,
        byte? deliveryMode = 2,
        string contentType = null)
    {
        var basicProperties = channelHost.Channel.CreateBasicProperties();
        basicProperties.DeliveryMode = deliveryMode ?? 2; // Default Persisted
        basicProperties.Priority = priority ?? 0; // Default Priority
        basicProperties.MessageId = messageId ?? Guid.NewGuid().ToString();

        if (!basicProperties.IsHeadersPresent())
        {
            basicProperties.Headers = new Dictionary<string, object>();
        }

        if (headers?.Count > 0)
        {
            foreach (var kvp in headers)
            {
                if (kvp.Key.StartsWith(Constants.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    basicProperties.Headers[kvp.Key] = kvp.Value;
                }
            }
        }

        SetMandatoryHeaders(basicProperties, contentType);

        return basicProperties;
    }

    private static void SetMandatoryHeaders(IBasicProperties basicProperties, string contentType)
    {
        basicProperties.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForMessageObjectType;
        if (!string.IsNullOrEmpty(contentType))
        {
            basicProperties.Headers[Constants.HeaderForContentType] = contentType;
        }
        var openTelHeader = OpenTelemetryHelpers.GetOrCreateTraceHeaderFromCurrentActivity();
        basicProperties.Headers[Constants.HeaderForTraceParent] = openTelHeader;
    }

    public static void EnrichSpanWithTags(
        TelemetrySpan span,
        string exchangeName,
        string routingKey,
        string messageId = null)
    {
        if (span == null || !span.IsRecording) return;

        span.SetAttribute(Constants.MessagingSystemKey, Constants.MessagingSystemValue);

        if (!string.IsNullOrEmpty(exchangeName))
        { span.SetAttribute(Constants.MessagingDestinationNameKey, exchangeName); }

        if (!string.IsNullOrEmpty(routingKey))
        { span.SetAttribute(Constants.MessagingMessageRoutingKeyKey, routingKey); }

        if (!string.IsNullOrEmpty(messageId))
        { span.SetAttribute(Constants.MessagingMessageMessageIdKey, messageId); }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _pubLock.Dispose();
            }

            _receiptBuffer = null;
            _messageQueue = null;
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
