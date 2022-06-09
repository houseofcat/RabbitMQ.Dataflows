using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Logger;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using HouseofCat.Utilities.Time;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ
{
    public interface IPublisher
    {
        RabbitOptions Options { get; }
        bool AutoPublisherStarted { get; }

        void StartAutoPublish(
            Func<IPublishReceipt, ValueTask> processReceiptAsync = null);
        Task StartAutoPublishAsync(
            Func<IPublishReceipt, ValueTask> processReceiptAsync = null);
        Task StopAutoPublishAsync(bool immediate = false);
        ValueTask QueueMessageAsync(
            IMessage message,
            CancellationToken cancellationToken = default);

        ChannelReader<IPublishReceipt> GetReceiptBufferReader();
        Task PublishAsync(
            IMessage message,
            bool createReceipt,
            bool withHeaders = true,
            CancellationToken cancellationToken = default);
        Task PublishWithConfirmationAsync(
            IMessage message,
            bool createReceipt,
            bool withOptionalHeaders = true,
            CancellationToken cancellationToken = default);
        Task<bool> PublishAsync(
            string exchangeName,
            string routingKey,
            ReadOnlyMemory<byte> payload,
            bool mandatory = false,
            IBasicProperties messageProperties = null,
            string messageId = null,
            CancellationToken cancellationToken = default);
        Task<bool> PublishAsync(
            string exchangeName,
            string routingKey,
            ReadOnlyMemory<byte> payload,
            IDictionary<string, object> headers = null,
            string messageId = null,
            byte? priority = 0,
            bool mandatory = false,
            CancellationToken cancellationToken = default);
        Task<bool> PublishBatchAsync(
            string exchangeName,
            string routingKey,
            IList<ReadOnlyMemory<byte>> payloads,
            bool mandatory = false,
            IBasicProperties messageProperties = null,
            CancellationToken cancellationToken = default);
        Task<bool> PublishBatchAsync(
            string exchangeName,
            string routingKey,
            IList<ReadOnlyMemory<byte>> payloads,
            IDictionary<string, object> headers = null,
            byte? priority = 0,
            bool mandatory = false,
            CancellationToken cancellationToken = default);
        Task PublishManyAsBatchAsync(
            IList<IMessage> messages,
            bool createReceipt,
            bool withHeaders = true,
            CancellationToken cancellationToken = default);
        Task PublishManyAsync(
            IList<IMessage> messages,
            bool createReceipt,
            bool withHeaders = true,
            CancellationToken cancellationToken = default);
    }

    public class Publisher : IPublisher, IDisposable
    {
        public RabbitOptions Options { get; }
        public bool AutoPublisherStarted { get; private set; }

        private readonly ILogger<Publisher> _logger;
        private readonly IConnectionPool _connectionPool;
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
        private Task _receiptTask;
        private bool _disposedValue;

        public Publisher(
            RabbitOptions options,
            ISerializationProvider serializationProvider,
            IEncryptionProvider encryptionProvider = null,
            ICompressionProvider compressionProvider = null)
            : this(
                  new ConnectionPool(options),
                  serializationProvider,
                  encryptionProvider,
                  compressionProvider)
        { }

        public Publisher(
            IConnectionPool connectionPool,
            ISerializationProvider serializationProvider,
            IEncryptionProvider encryptionProvider = null,
            ICompressionProvider compressionProvider = null)
        {
            Guard.AgainstNull(connectionPool, nameof(connectionPool));
            Guard.AgainstNull(serializationProvider, nameof(serializationProvider));

            Options = connectionPool.Options;
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

            _connectionPool = connectionPool;
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

        #region AutoPublisher
        public void StartAutoPublish(
            Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
        {
            _pubLock.Wait();

            try
            {
                if (AutoPublisherStarted) return;
                SetupPublisher(processReceiptAsync);
            }
            finally { _pubLock.Release(); }
        }

        public async Task StartAutoPublishAsync(
            Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
        {
            await _pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (AutoPublisherStarted) return;
                SetupPublisher(processReceiptAsync);
            }
            finally { _pubLock.Release(); }
        }

        private void SetupPublisher(
            Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
        {
            AutoPublisherStarted = true;

            _messageQueue = Channel.CreateBounded<IMessage>(
                new BoundedChannelOptions(Options.PublisherOptions.LetterQueueBufferSize)
                {
                    FullMode = Options.PublisherOptions.BehaviorWhenFull
                });

            _publishingTask = ProcessMessagesAsync(_messageQueue.Reader);

            if (processReceiptAsync == null) processReceiptAsync = ProcessReceiptAsync;

            _receiptTask = ProcessReceiptsAsync(processReceiptAsync);
        }

        private async Task ProcessMessagesAsync(
            ChannelReader<IMessage> channelReader)
        {
            await Task.Yield();
            await foreach (var message in channelReader.ReadAllAsync().ConfigureAwait(false))
            {
                if (message == null)
                { continue; }

                var metadata = message.GetMetadata();

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
                    metadata.CustomFields[Constants.HeaderForEncryptDate] = Time.GetDateTimeNow(Time.Formats.CatRFC3339);
                }

                _logger.LogDebug(LogMessages.AutoPublishers.MessagePublished, message.MessageId, metadata?.Id);

                await PublishAsync(message, _createPublishReceipts, _withHeaders)
                    .ConfigureAwait(false);
            }
        }

        private async Task ProcessReceiptsAsync(
            Func<IPublishReceipt, ValueTask> processReceiptAsync)
        {
            await Task.Yield();
            await foreach (var receipt in _receiptBuffer.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await processReceiptAsync(receipt).ConfigureAwait(false);
            }
        }

        // Super simple version to bake in requeueing of all failed to publish messages.
        private async ValueTask ProcessReceiptAsync(IPublishReceipt receipt)
        {
            var originalMessage = receipt.GetOriginalMessage();
            if (receipt.IsError && originalMessage != null)
            {
                _logger.LogWarning($"Failed publish for message ({originalMessage.MessageId}). Retrying with AutoPublishing...");

                try
                { await QueueMessageAsync(receipt.GetOriginalMessage()); }
                catch (Exception ex) /* No-op */
                { _logger.LogDebug("Error ({0}) occurred on retry, most likely because retry during shutdown.", ex.Message); }
            }
        }

        public async Task StopAutoPublishAsync(bool immediate = false)
        {
            await _pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!AutoPublisherStarted) return;

                AutoPublisherStarted = false;

                _messageQueue.Writer.TryComplete();

                if (immediate) return;

                // FIXME This creates a race codition with the local variables if we try to start auto publishing immediately
                await Task.WhenAll(_messageQueue.Reader.Completion, _publishingTask, _receiptTask).ConfigureAwait(false);
            }
            finally
            { _pubLock.Release(); }
        }

        public ChannelReader<IPublishReceipt> GetReceiptBufferReader()
        {
            return _receiptBuffer.Reader;
        }

        public async ValueTask QueueMessageAsync(
            IMessage message,
            CancellationToken cancellationToken = default)
        {
            if (!AutoPublisherStarted) throw new InvalidOperationException(ExceptionMessages.AutoPublisherNotStartedError);
            Guard.AgainstNull(message, nameof(message));

            try
            {
                var metadata = message.GetMetadata();

                await _messageQueue
                    .Writer
                    .WriteAsync(message)
                    .ConfigureAwait(false);

                _logger.LogDebug(LogMessages.AutoPublishers.MessageQueued, message.MessageId, metadata?.Id);
            }
            catch
            {
                throw new InvalidOperationException(ExceptionMessages.QueueChannelError);
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
            string messageId = null,
            CancellationToken cancellationToken = default)
        {
            Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));

            var error = false;
            var (channel, returnFunc) = await _connectionPool.GetChannelAsync(false, cancellationToken).ConfigureAwait(false);
            if (messageProperties == null)
            {
                messageProperties = channel.CreateBasicProperties();
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
                channel.BasicPublish(
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
                await returnFunc.Invoke(channel, error).ConfigureAwait(false);
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
            bool mandatory = false,
            CancellationToken cancellationToken = default)
        {
            Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));

            var error = false;
            var (channel, returnFunc) = await _connectionPool.GetChannelAsync(false, cancellationToken).ConfigureAwait(false);

            try
            {
                channel.BasicPublish(
                    exchange: exchangeName ?? string.Empty,
                    routingKey: routingKey,
                    mandatory: mandatory,
                    basicProperties: BuildProperties(headers, channel, messageId, priority),
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
                await returnFunc.Invoke(channel, error).ConfigureAwait(false);
            }

            return error;
        }

        // A basic implementation of publishing batches but using the ChannelPool. If message properties is null, one is created and all messages are set to persistent.
        public async Task<bool> PublishBatchAsync(
            string exchangeName,
            string routingKey,
            IList<ReadOnlyMemory<byte>> payloads,
            bool mandatory = false,
            IBasicProperties messageProperties = null,
            CancellationToken cancellationToken = default)
        {
            Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));
            Guard.AgainstNullOrEmpty(payloads, nameof(payloads));

            var error = false;
            var (channel, returnFunc) = await _connectionPool.GetChannelAsync(false, cancellationToken).ConfigureAwait(false);
            if (messageProperties == null)
            {
                messageProperties = channel.CreateBasicProperties();
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
                var batch = channel.CreateBasicPublishBatch();

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
                await returnFunc.Invoke(channel, error).ConfigureAwait(false);
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
            bool mandatory = false,
            CancellationToken cancellationToken = default)
        {
            Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));
            Guard.AgainstNullOrEmpty(payloads, nameof(payloads));

            var error = false;
            var (channel, returnFunc) = await _connectionPool.GetChannelAsync(false, cancellationToken).ConfigureAwait(false);

            try
            {
                var batch = channel.CreateBasicPublishBatch();

                for (int i = 0; i < payloads.Count; i++)
                {
                    batch.Add(exchangeName, routingKey, mandatory, BuildProperties(headers, channel, null, priority), payloads[i]);
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
                await returnFunc.Invoke(channel, error).ConfigureAwait(false);
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
        public async Task PublishAsync(
            IMessage message,
            bool createReceipt,
            bool withOptionalHeaders = true,
            CancellationToken cancellationToken = default)
        {
            var error = false;
            var (channel, returnFunc) = await _connectionPool.GetChannelAsync(false, cancellationToken).ConfigureAwait(false);

            try
            {
                channel.BasicPublish(
                    message.Envelope.Exchange,
                    message.Envelope.RoutingKey,
                    message.Envelope.RoutingOptions?.Mandatory ?? false,
                    message.BuildProperties(channel, withOptionalHeaders),
                    message.GetBodyToPublish(_serializationProvider));
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
            
            if (createReceipt)
            {
                try
                {
                    await CreateReceiptAsync(message, error).ConfigureAwait(false);
                }
                catch
                {
                    error = true;
                }
            }
                

            await returnFunc.Invoke(channel, error).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires an ackable channel from the channel pool, then publishes message based on the letter/envelope parameters and waits for confirmation.
        /// <para>Only throws exception when failing to acquire channel or when creating a receipt after the ReceiptBuffer is closed.</para>
        /// <para>Not fully ready for production yet.</para>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="createReceipt"></param>
        /// <param name="withOptionalHeaders"></param>
        public async Task PublishWithConfirmationAsync(
            IMessage message,
            bool createReceipt,
            bool withOptionalHeaders = true,
            CancellationToken cancellationToken = default)
        {
            var error = false;
            var (channel, returnFunc) = await _connectionPool.GetChannelAsync(true, cancellationToken).ConfigureAwait(false);

            try
            {
                channel.WaitForConfirmsOrDie(_waitForConfirmation);

                channel.BasicPublish(
                    message.Envelope.Exchange,
                    message.Envelope.RoutingKey,
                    message.Envelope.RoutingOptions?.Mandatory ?? false,
                    message.BuildProperties(channel, withOptionalHeaders),
                    message.GetBodyToPublish(_serializationProvider));

                channel.WaitForConfirmsOrDie(_waitForConfirmation);
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

            if (createReceipt)
            {
                try
                {
                    await CreateReceiptAsync(message, error).ConfigureAwait(false);
                }
                catch
                {
                    error = true;
                }
            }

            await returnFunc.Invoke(channel, error).ConfigureAwait(false);
        }

        /// <summary>
        /// Use this method to sequentially publish all messages in a list in the order received.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="createReceipt"></param>
        /// <param name="withOptionalHeaders"></param>
        public async Task PublishManyAsync(
            IList<IMessage> messages,
            bool createReceipt,
            bool withOptionalHeaders = true,
            CancellationToken cancellationToken = default)
        {
            var error = false;
            var (channel, returnFunc) = await _connectionPool.GetChannelAsync(false, cancellationToken).ConfigureAwait(false);

            for (int i = 0; i < messages.Count; i++)
            {
                try
                {
                    channel.BasicPublish(
                        messages[i].Envelope.Exchange,
                        messages[i].Envelope.RoutingKey,
                        messages[i].Envelope.RoutingOptions.Mandatory,
                        messages[i].BuildProperties(channel, withOptionalHeaders),
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
                {
                    try
                    {
                        await CreateReceiptAsync(messages[i], error).ConfigureAwait(false);
                    }
                    catch
                    {
                        error = true;
                    }
                }

                if (error) { break; }
            }

            await returnFunc.Invoke(channel, error).ConfigureAwait(false);
        }

        /// <summary>
        /// Use this method when a group of letters who have the same properties (deliverymode, messagetype, priority).
        /// <para>Receipt with no error indicates that we successfully handed off to internal library, not necessarily published.</para>
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="createReceipt"></param>
        /// <param name="withOptionalHeaders"></param>
        public async Task PublishManyAsBatchAsync(
            IList<IMessage> messages,
            bool createReceipt,
            bool withOptionalHeaders = true,
            CancellationToken cancellationToken = default)
        {
            var error = false;
            var (channel, returnFunc) = await _connectionPool.GetChannelAsync(false, cancellationToken).ConfigureAwait(false);

            try
            {
                if (messages.Count > 0)
                {
                    var publishBatch = channel.CreateBasicPublishBatch();
                    for (int i = 0; i < messages.Count; i++)
                    {
                        publishBatch.Add(
                            messages[i].Envelope.Exchange,
                            messages[i].Envelope.RoutingKey,
                            messages[i].Envelope.RoutingOptions.Mandatory,
                            messages[i].BuildProperties(channel, withOptionalHeaders),
                            messages[i].GetBodyToPublish(_serializationProvider).AsMemory());

                        if (createReceipt)
                        {
                            try
                            {
                                await CreateReceiptAsync(messages[i], error).ConfigureAwait(false);
                            }
                            catch
                            {
                                error = true;
                            }
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
            {
                await returnFunc.Invoke(channel, error).ConfigureAwait(false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask CreateReceiptAsync(IMessage message, bool error)
        {
            try
            {
                await _receiptBuffer
                    .Writer
                    .WriteAsync(message.GetPublishReceipt(error))
                    .ConfigureAwait(false);
            }
            catch
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }
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
            IModel channel,
            string messageId = null,
            byte? priority = 0,
            byte? deliveryMode = 2)
        {
            var props = channel.CreateBasicProperties();
            props.DeliveryMode = deliveryMode ?? 2; // Default Persisted
            props.Priority = priority ?? 0; // Default Priority
            props.MessageId = messageId ?? Guid.NewGuid().ToString();

            if (!props.IsHeadersPresent())
            {
                props.Headers = new Dictionary<string, object>();
            }

            if (headers?.Count > 0)
            {
                foreach (var kvp in headers)
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

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
