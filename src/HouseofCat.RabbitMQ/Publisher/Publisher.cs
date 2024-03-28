using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using HouseofCat.Utilities;
using HouseofCat.Utilities.Errors;
using HouseofCat.Utilities.Time;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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

    ChannelReader<IPublishReceipt> GetReceiptBufferReader();
    Task PublishAsync(IMessage message, bool createReceipt, bool withHeaders = true);
    Task PublishWithConfirmationAsync(IMessage message, bool createReceipt, bool withOptionalHeaders = true);

    Task<bool> PublishAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> payload,
        bool mandatory = false,
        IBasicProperties messageProperties = null,
        string messageId = null);

    Task<bool> PublishAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> payload,
        IDictionary<string, object> headers = null,
        string messageId = null,
        byte? priority = 0,
        bool mandatory = false);

    Task<bool> PublishBatchAsync(
        string exchangeName,
        string routingKey,
        IList<ReadOnlyMemory<byte>> payloads,
        bool mandatory = false,
        IBasicProperties messageProperties = null);

    Task<bool> PublishBatchAsync(
        string exchangeName,
        string routingKey,
        IList<ReadOnlyMemory<byte>> payloads,
        IDictionary<string, object> headers = null,
        byte? priority = 0,
        bool mandatory = false);

    Task PublishManyAsBatchAsync(IList<IMessage> messages, bool createReceipt, bool withHeaders = true);
    Task PublishManyAsync(IList<IMessage> messages, bool createReceipt, bool withHeaders = true);

    void QueueMessage(IMessage message);
    ValueTask QueueMessageAsync(IMessage message);
    void StartAutoPublish(Func<IPublishReceipt, ValueTask> processReceiptAsync = null);
    Task StartAutoPublishAsync(Func<IPublishReceipt, ValueTask> processReceiptAsync = null);
    Task StopAutoPublishAsync(bool immediately = false);
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
    private readonly bool _withHeaders;
    private readonly bool _compress;
    private readonly bool _encrypt;
    private readonly bool _createPublishReceipts;
    private readonly TimeSpan _waitForConfirmation;

    private Channel<IMessage> _messageQueue;
    private Channel<IPublishReceipt> _receiptBuffer;

    private Task _publishingTask;
    private Task _processReceiptsAsync;
    private bool _disposedValue;

    public string TimeFormat { get; set; } = Time.Formats.CatsAltFormat;

    private static readonly ActivitySource _activitySource =
        new ActivitySource(Assembly.GetEntryAssembly().GetName().Name ?? "HouseofCat.RabbitMQ");

    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

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
        _logger = LogHelper.GetLogger<Publisher>();
        _serializationProvider = serializationProvider;

        if (Options.PublisherOptions.Encrypt && encryptionProvider == null)
        {
            _encrypt = false;
            _logger.LogWarning("Encryption disabled, encryptionProvider provided was null.");
        }
        else if (Options.PublisherOptions.Encrypt)
        {
            _encrypt = true;
            _encryptionProvider = encryptionProvider;
        }

        if (Options.PublisherOptions.Compress && compressionProvider == null)
        {
            _compress = false;
            _logger.LogWarning("Compression disabled, compressionProvider provided was null.");
        }
        else if (Options.PublisherOptions.Compress)
        {
            _compress = true;
            _compressionProvider = compressionProvider;
        }

        _channelPool = channelPool;
        _receiptBuffer = Channel.CreateBounded<IPublishReceipt>(
            new BoundedChannelOptions(1024)
            {
                SingleWriter = false,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.DropOldest, // never block
            });

        _withHeaders = Options.PublisherOptions.WithHeaders;
        _createPublishReceipts = Options.PublisherOptions.CreatePublishReceipts;
        _waitForConfirmation = TimeSpan.FromMilliseconds(Options.PublisherOptions.WaitForConfirmationTimeoutInMilliseconds);
    }

    public void StartAutoPublish(Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
    {
        _pubLock.Wait();

        try { SetupPublisher(processReceiptAsync); }
        finally { _ = _pubLock.Release(); }
    }

    public async Task StartAutoPublishAsync(Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
    {
        await _pubLock.WaitAsync().ConfigureAwait(false);

        try { SetupPublisher(processReceiptAsync); }
        finally { _ = _pubLock.Release(); }
    }

    private void SetupPublisher(Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
    {
        _messageQueue = Channel.CreateBounded<IMessage>(
        new BoundedChannelOptions(Options.PublisherOptions.LetterQueueBufferSize)
        {
            FullMode = Options.PublisherOptions.BehaviorWhenFull
        });

        _publishingTask = ProcessMessagesAsync(_messageQueue.Reader);

        processReceiptAsync ??= ProcessReceiptAsync;

        _processReceiptsAsync ??= ProcessReceiptsAsync(processReceiptAsync);

        AutoPublisherStarted = true;
    }

    public async Task StopAutoPublishAsync(bool immediately = false)
    {
        await _pubLock.WaitAsync().ConfigureAwait(false);

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
        { _ = _pubLock.Release(); }
    }

    public ChannelReader<IPublishReceipt> GetReceiptBufferReader()
    {
        return _receiptBuffer.Reader;
    }

    #region AutoPublisher

    public void QueueMessage(IMessage message)
    {
        if (!AutoPublisherStarted)
        {
            throw new InvalidOperationException(ExceptionMessages.AutoPublisherNotStartedError);
        }

        Guard.AgainstNull(message, nameof(message));

        IMetadata metadata = message.GetMetadata();
        _logger.LogDebug(LogMessages.AutoPublishers.MessageQueued, message.MessageId, metadata?.Id);

        _ = _messageQueue.Writer.TryWrite(message);
    }

    public async ValueTask QueueMessageAsync(IMessage message)
    {
        if (!AutoPublisherStarted)
        {
            throw new InvalidOperationException(ExceptionMessages.AutoPublisherNotStartedError);
        }

        Guard.AgainstNull(message, nameof(message));

        if (!await _messageQueue
             .Writer
             .WaitToWriteAsync()
             .ConfigureAwait(false))
        {
            throw new InvalidOperationException(ExceptionMessages.QueueChannelError);
        }

        IMetadata metadata = message.GetMetadata();
        _logger.LogDebug(LogMessages.AutoPublishers.MessageQueued, message.MessageId, metadata?.Id);

        await _messageQueue
          .Writer
          .WriteAsync(message)
          .ConfigureAwait(false);
    }

    private async Task ProcessMessagesAsync(ChannelReader<IMessage> channelReader)
    {
        await Task.Yield();
        while (await channelReader.WaitToReadAsync().ConfigureAwait(false))
        {
            while (channelReader.TryRead(out IMessage message))
            {
                if (message == null)
                { continue; }

                IMetadata metadata = message.GetMetadata();

                if (_compress)
                {
                    message.Body = _compressionProvider.Compress(message.Body).ToArray();
                    metadata.Compressed = _compress;
                    metadata.CustomFields[Constants.HeaderForCompressed] = _compress;
                    metadata.CustomFields[Constants.HeaderForCompression] = _compressionProvider.Type;
                }

                if (_encrypt)
                {
                    message.Body = _encryptionProvider.Encrypt(message.Body).ToArray();
                    metadata.Encrypted = _encrypt;
                    metadata.CustomFields[Constants.HeaderForEncrypted] = _encrypt;
                    metadata.CustomFields[Constants.HeaderForEncryption] = _encryptionProvider.Type;
                    metadata.CustomFields[Constants.HeaderForEncryptDate] = Time.GetDateTimeNow(Time.Formats.RFC3339Long);
                }

                _logger.LogDebug(LogMessages.AutoPublishers.MessagePublished, message.MessageId, metadata?.Id);

                await PublishAsync(message, _createPublishReceipts, _withHeaders)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessReceiptsAsync(Func<IPublishReceipt, ValueTask> processReceiptAsync)
    {
        await Task.Yield();
        await foreach (IPublishReceipt receipt in _receiptBuffer.Reader.ReadAllAsync())
        {
            await processReceiptAsync(receipt).ConfigureAwait(false);
        }
    }

    // Super simple version to bake in requeueing of all failed to publish messages.
    private async ValueTask ProcessReceiptAsync(IPublishReceipt receipt)
    {
        IMessage originalMessage = receipt.GetOriginalMessage();
        if (receipt.IsError && originalMessage != null)
        {
            if (AutoPublisherStarted)
            {
                _logger.LogWarning($"Failed publish for message ({originalMessage.MessageId}). Retrying with AutoPublishing...");

                try
                { await QueueMessageAsync(receipt.GetOriginalMessage()); }
                catch (Exception ex) /* No-op */
                { _logger.LogDebug("Error ({0}) occurred on retry, most likely because retry during shutdown.", ex.Message); }
            }
            else
            {
                _logger.LogError($"Failed publish for message ({originalMessage.MessageId}). Unable to retry as the original message was not received.");
            }
        }
    }

    #endregion

    #region Publishing

    // A basic implementation of publish but using the ChannelPool. If message properties is null, one is created and all messages are set to persistent.
    public async Task<bool> PublishAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> payload,
        bool mandatory = false,
        IBasicProperties messageProperties = null,
        string messageId = null)
    {
        Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));

        bool error = false;
        IChannelHost channelHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);
        if (messageProperties == null)
        {
            messageProperties = channelHost.GetChannel().CreateBasicProperties();
            messageProperties.DeliveryMode = 2;
            messageProperties.MessageId = messageId ?? Guid.NewGuid().ToString();

            if (!messageProperties.IsHeadersPresent())
            {
                messageProperties.Headers = new Dictionary<string, object>();
            }
        }

        // Non-optional Header.
        messageProperties.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForMessage;

        try
        {
            channelHost
                .GetChannel()
                .BasicPublish(
                    exchange: exchangeName ?? string.Empty,
                    routingKey: routingKey,
                    mandatory: mandatory,
                    basicProperties: messageProperties,
                    body: payload);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(LogMessages.Publishers.PublishFailed, $"{exchangeName}->{routingKey}", ex.Message);
            error = true;
        }
        finally
        {
            await _channelPool
                .ReturnChannelAsync(channelHost, error);
        }

        return error;
    }

    // A basic implementation of publish but using the ChannelPool. If headers are provided and start with "x-", they get included in the message properties.
    public async Task<bool> PublishAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> payload,
        IDictionary<string, object> headers = null,
        string messageId = null,
        byte? priority = 0,
        bool mandatory = false)
    {
        Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));

        bool error = false;
        IChannelHost channelHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

        try
        {
            channelHost
                .GetChannel()
                .BasicPublish(
                    exchange: exchangeName ?? string.Empty,
                    routingKey: routingKey,
                    mandatory: mandatory,
                    basicProperties: BuildProperties(headers, channelHost, messageId, priority),
                    body: payload);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                LogMessages.Publishers.PublishFailed,
                $"{exchangeName}->{routingKey}",
                ex.Message);

            error = true;
        }
        finally
        {
            await _channelPool
                .ReturnChannelAsync(channelHost, error);
        }

        return error;
    }

    // A basic implementation of publishing batches but using the ChannelPool. If message properties is null, one is created and all messages are set to persistent.
    public async Task<bool> PublishBatchAsync(
        string exchangeName,
        string routingKey,
        IList<ReadOnlyMemory<byte>> payloads,
        bool mandatory = false,
        IBasicProperties messageProperties = null)
    {
        Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));
        Guard.AgainstNullOrEmpty(payloads, nameof(payloads));

        bool error = false;
        IChannelHost channelHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);
        if (messageProperties == null)
        {
            messageProperties = channelHost.GetChannel().CreateBasicProperties();
            messageProperties.DeliveryMode = 2;
            messageProperties.MessageId = Guid.NewGuid().ToString();

            if (!messageProperties.IsHeadersPresent())
            {
                messageProperties.Headers = new Dictionary<string, object>();
            }
        }

        // Non-optional Header.
        messageProperties.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForMessage;

        try
        {
            IBasicPublishBatch batch = channelHost.GetChannel().CreateBasicPublishBatch();

            for (int i = 0; i < payloads.Count; i++)
            {
                batch.Add(exchangeName, routingKey, mandatory, messageProperties, payloads[i]);
            }

            batch.Publish();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                LogMessages.Publishers.PublishFailed,
                $"{exchangeName}->{routingKey}",
                ex.Message);

            error = true;
        }
        finally
        {
            await _channelPool
                .ReturnChannelAsync(channelHost, error);
        }

        return error;
    }

    // A basic implementation of publishing batches but using the ChannelPool. If message properties is null, one is created and all messages are set to persistent.
    public async Task<bool> PublishBatchAsync(
        string exchangeName,
        string routingKey,
        IList<ReadOnlyMemory<byte>> payloads,
        IDictionary<string, object> headers = null,
        byte? priority = 0,
        bool mandatory = false)
    {
        Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));
        Guard.AgainstNullOrEmpty(payloads, nameof(payloads));

        bool error = false;
        IChannelHost channelHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

        try
        {
            IBasicPublishBatch batch = channelHost.GetChannel().CreateBasicPublishBatch();

            for (int i = 0; i < payloads.Count; i++)
            {
                batch.Add(exchangeName, routingKey, mandatory, BuildProperties(headers, channelHost, null, priority), payloads[i]);
            }

            batch.Publish();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                LogMessages.Publishers.PublishFailed,
                $"{exchangeName}->{routingKey}",
                ex.Message);

            error = true;
        }
        finally
        {
            await _channelPool
                .ReturnChannelAsync(channelHost, error);
        }

        return error;
    }

    /// <summary>
    /// Acquires a channel from the channel pool, then publishes message based on the letter/envelope parameters.
    /// <para>Only throws exception when failing to acquire channel or when creating a receipt after the ReceiptBuffer is closed.</para>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="createReceipt"></param>
    /// <param name="withOptionalHeaders"></param>
    public async Task PublishAsync(IMessage message, bool createReceipt, bool withOptionalHeaders = true)
    {
        bool error = false;
        IChannelHost channelHost = await _channelPool
            .GetChannelAsync()
            .ConfigureAwait(false);

        string activityName = $"{message.Envelope.RoutingKey} send";
        // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name

        using Activity activity = Activity.Current ??
            _activitySource.StartActivity(activityName, ActivityKind.Producer, parentContext: message.ActivityContext.HasValue ? message.ActivityContext.Value : default);

        try
        {
            ReadOnlyMemory<byte> body = message.GetBodyToPublish(_serializationProvider);

            ActivityContext contextToInject = default;
            if (activity != null)
            {
                contextToInject = activity.Context;
            }

            IBasicProperties basicProperties = message.BuildProperties(channelHost, withOptionalHeaders);
            AddMessagingTags(activity, message);
            // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
            Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), basicProperties, InjectTraceContextIntoBasicProperties);


            channelHost
                .GetChannel()
                .BasicPublish(
                    message.Envelope.Exchange,
                    message.Envelope.RoutingKey,
                    message.Envelope.RoutingOptions?.Mandatory ?? false,
                    basicProperties,
                    body);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                LogMessages.Publishers.PublishMessageFailed,
                $"{message.Envelope.Exchange}->{message.Envelope.RoutingKey}",
                message.MessageId,
                ex.Message);

            error = true;
        }
        finally
        {
            if (createReceipt)
            {
                await CreateReceiptAsync(message, error)
                    .ConfigureAwait(false);
            }

            await _channelPool
                .ReturnChannelAsync(channelHost, error);
        }
    }

    /// <summary>
    /// Acquires an ackable channel from the channel pool, then publishes message based on the letter/envelope parameters and waits for confirmation.
    /// <para>Only throws exception when failing to acquire channel or when creating a receipt after the ReceiptBuffer is closed.</para>
    /// <para>Not fully ready for production yet.</para>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="createReceipt"></param>
    /// <param name="withOptionalHeaders"></param>
    public async Task PublishWithConfirmationAsync(IMessage message, bool createReceipt, bool withOptionalHeaders = true)
    {
        bool error = false;
        IChannelHost chanHost = await _channelPool
            .GetAckChannelAsync()
            .ConfigureAwait(false);

        try
        {
            chanHost.GetChannel().WaitForConfirmsOrDie(_waitForConfirmation);

            chanHost
                .GetChannel()
                .BasicPublish(
                    message.Envelope.Exchange,
                    message.Envelope.RoutingKey,
                    message.Envelope.RoutingOptions?.Mandatory ?? false,
                    message.BuildProperties(chanHost, withOptionalHeaders),
                    message.GetBodyToPublish(_serializationProvider));

            chanHost.GetChannel().WaitForConfirmsOrDie(_waitForConfirmation);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                LogMessages.Publishers.PublishMessageFailed,
                $"{message.Envelope.Exchange}->{message.Envelope.RoutingKey}",
                message.MessageId,
                ex.Message);

            error = true;
        }
        finally
        {
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
    /// Use this method to sequentially publish all messages in a list in the order received.
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="createReceipt"></param>
    /// <param name="withOptionalHeaders"></param>
    public async Task PublishManyAsync(IList<IMessage> messages, bool createReceipt, bool withOptionalHeaders = true)
    {
        bool error = false;
        IChannelHost chanHost = await _channelPool
            .GetChannelAsync()
            .ConfigureAwait(false);

        for (int i = 0; i < messages.Count; i++)
        {
            try
            {
                chanHost.GetChannel().BasicPublish(
                    messages[i].Envelope.Exchange,
                    messages[i].Envelope.RoutingKey,
                    messages[i].Envelope.RoutingOptions.Mandatory,
                    messages[i].BuildProperties(chanHost, withOptionalHeaders),
                    messages[i].GetBodyToPublish(_serializationProvider));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    LogMessages.Publishers.PublishMessageFailed,
                    $"{messages[i].Envelope.Exchange}->{messages[i].Envelope.RoutingKey}",
                    messages[i].MessageId,
                    ex.Message);

                error = true;
            }

            if (createReceipt)
            { await CreateReceiptAsync(messages[i], error).ConfigureAwait(false); }

            if (error) { break; }
        }

        await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false);
    }

    /// <summary>
    /// Use this method when a group of letters who have the same properties (deliverymode, messagetype, priority).
    /// <para>Receipt with no error indicates that we successfully handed off to internal library, not necessarily published.</para>
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="createReceipt"></param>
    /// <param name="withOptionalHeaders"></param>
    public async Task PublishManyAsBatchAsync(IList<IMessage> messages, bool createReceipt, bool withOptionalHeaders = true)
    {
        bool error = false;
        IChannelHost chanHost = await _channelPool
            .GetChannelAsync()
            .ConfigureAwait(false);

        try
        {
            if (messages.Count > 0)
            {
                IBasicPublishBatch publishBatch = chanHost.GetChannel().CreateBasicPublishBatch();
                for (int i = 0; i < messages.Count; i++)
                {
                    publishBatch.Add(
                        messages[i].Envelope.Exchange,
                        messages[i].Envelope.RoutingKey,
                        messages[i].Envelope.RoutingOptions.Mandatory,
                        messages[i].BuildProperties(chanHost, withOptionalHeaders),
                        messages[i].GetBodyToPublish(_serializationProvider));

                    if (createReceipt)
                    {
                        await CreateReceiptAsync(messages[i], error).ConfigureAwait(false);
                    }
                }

                publishBatch.Publish();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                LogMessages.Publishers.PublishBatchFailed,
                ex.Message);

            error = true;
        }
        finally
        { await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask CreateReceiptAsync(IMessage message, bool error)
    {
        if (!await _receiptBuffer
            .Writer
            .WaitToWriteAsync()
            .ConfigureAwait(false))
        {
            throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
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
        byte? deliveryMode = 2)
    {
        IBasicProperties props = channelHost.GetChannel().CreateBasicProperties();
        props.DeliveryMode = deliveryMode ?? 2; // Default Persisted
        props.Priority = priority ?? 0; // Default Priority
        props.MessageId = messageId ?? Guid.NewGuid().ToString();

        if (!props.IsHeadersPresent())
        {
            props.Headers = new Dictionary<string, object>();
        }

        if (headers?.Count > 0)
        {
            foreach (KeyValuePair<string, object> kvp in headers)
            {
                if (kvp.Key.StartsWith(Constants.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    props.Headers[kvp.Key] = kvp.Value;
                }
            }
        }

        // Non-optional Header.
        props.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForMessage;

        return props;
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

    private void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
    {
        try
        {
            props.Headers ??= new Dictionary<string, object>();

            props.Headers[key] = value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inject trace context.");
        }
    }
    public static void AddMessagingTags(Activity activity, IMessage message)
    {
        // See:
        //   * https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#messaging-attributes
        //   * https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/rabbitmq.md
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination_kind", "queue");
        activity?.SetTag("messaging.destination", message.Envelope.Exchange);
        activity?.SetTag("messaging.rabbitmq.routing_key", message.Envelope.RoutingKey);
        activity?.SetTag("messaging.message.id", message.MessageId);
        activity?.SetTag("messaging.operation", "publish");
    }
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
