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
        bool AutoPublisherStarted { get; }
        Options Options { get; }

        ChannelReader<PublishReceipt> GetReceiptBufferReader();
        Task PublishAsync(Letter letter, bool createReceipt, bool withHeaders = true);
        Task PublishWithConfirmationAsync(Letter letter, bool createReceipt, bool withHeaders = true);
        Task<bool> PublishAsync(string exchangeName, string routingKey, ReadOnlyMemory<byte> payload, bool mandatory = false, IBasicProperties messageProperties = null);
        Task<bool> PublishAsync(string exchangeName, string routingKey, ReadOnlyMemory<byte> payload, IDictionary<string, object> headers = null, byte? priority = 0, bool mandatory = false);
        Task<bool> PublishBatchAsync(string exchangeName, string routingKey, IList<ReadOnlyMemory<byte>> payloads, bool mandatory = false, IBasicProperties messageProperties = null);
        Task<bool> PublishBatchAsync(string exchangeName, string routingKey, IList<ReadOnlyMemory<byte>> payloads, IDictionary<string, object> headers = null, byte? priority = 0, bool mandatory = false);
        Task PublishManyAsBatchAsync(IList<Letter> letters, bool createReceipt, bool withHeaders = true);
        Task PublishManyAsync(IList<Letter> letters, bool createReceipt, bool withHeaders = true);
        ValueTask QueueLetterAsync(Letter letter);
        Task StartAutoPublishAsync(Func<PublishReceipt, ValueTask> processReceiptAsync = null);
        Task StopAutoPublishAsync(bool immediately = false);
    }

    public class Publisher : IPublisher, IDisposable
    {
        public Options Options { get; }

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

        private Channel<Letter> _letterQueue;
        private Channel<PublishReceipt> _receiptBuffer;

        private Task _publishingTask;
        private Task _processReceiptsAsync;
        private bool _disposedValue;

        public Publisher(
            Options options,
            ISerializationProvider serializationProvider,
            IEncryptionProvider encryptionProvider,
            ICompressionProvider compressionProvider)
            : this(
                  new ChannelPool(options),
                  serializationProvider,
                  encryptionProvider,
                  compressionProvider)
        { }

        public Publisher(
            IChannelPool channelPool,
            ISerializationProvider serializationProvider,
            IEncryptionProvider encryptionProvider,
            ICompressionProvider compressionProvider)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNull(serializationProvider, nameof(serializationProvider));

            Options = channelPool.Options;
            _logger = LogHelper.GetLogger<Publisher>();
            _serializationProvider = serializationProvider;

            if (Options.PublisherOptions.Encrypt && encryptionProvider != null)
            {
                _encrypt = false;
                _logger.LogWarning("Encryption disabled, encryptionProvider provided was null.");
            }
            else if (Options.PublisherOptions.Encrypt)
            {
                _encrypt = true;
                _encryptionProvider = encryptionProvider;
            }

            if (Options.PublisherOptions.Compress && compressionProvider != null)
            {
                _compress = false;
                _logger.LogWarning("Compression disabled, compressionProvider provided was null.");
            }
            else if (Options.PublisherOptions.Encrypt)
            {
                _compress = true;
                _compressionProvider = compressionProvider;
            }

            _channelPool = channelPool;
            _receiptBuffer = Channel.CreateBounded<PublishReceipt>(
                new BoundedChannelOptions(1024)
                {
                    SingleWriter = false,
                    SingleReader = true,
                });

            _withHeaders = Options.PublisherOptions.WithHeaders;
            _createPublishReceipts = Options.PublisherOptions.CreatePublishReceipts;
            _waitForConfirmation = TimeSpan.FromMilliseconds(Options.PublisherOptions.WaitForConfirmationTimeoutInMilliseconds);

        }

        public async Task StartAutoPublishAsync(Func<PublishReceipt, ValueTask> processReceiptAsync = null)
        {
            await _pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                _letterQueue = Channel.CreateBounded<Letter>(
                    new BoundedChannelOptions(Options.PublisherOptions.LetterQueueBufferSize)
                    {
                        FullMode = Options.PublisherOptions.BehaviorWhenFull
                    });

                _publishingTask = ProcessLettersAsync(_letterQueue.Reader);

                if (processReceiptAsync == null)
                { processReceiptAsync = ProcessReceiptAsync; }

                if (_processReceiptsAsync == null)
                {
                    _processReceiptsAsync = ProcessReceiptsAsync(processReceiptAsync);
                }

                AutoPublisherStarted = true;
            }
            finally { _pubLock.Release(); }
        }

        public async Task StopAutoPublishAsync(bool immediately = false)
        {
            await _pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (AutoPublisherStarted)
                {
                    _letterQueue.Writer.Complete();

                    if (!immediately)
                    {
                        await _letterQueue
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

        public ChannelReader<PublishReceipt> GetReceiptBufferReader()
        {
            return _receiptBuffer.Reader;
        }

        #region AutoPublisher

        public async ValueTask QueueLetterAsync(Letter letter)
        {
            if (!AutoPublisherStarted) throw new InvalidOperationException(ExceptionMessages.AutoPublisherNotStartedError);
            Guard.AgainstNull(letter, nameof(letter));

            if (!await _letterQueue
                 .Writer
                 .WaitToWriteAsync()
                 .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.QueueChannelError);
            }

            _logger.LogDebug(LogMessages.AutoPublishers.LetterQueued, letter.LetterId, letter.LetterMetadata?.Id);

            await _letterQueue
                .Writer
                .WriteAsync(letter)
                .ConfigureAwait(false);
        }

        private async Task ProcessLettersAsync(ChannelReader<Letter> channelReader)
        {
            await Task.Yield();
            while (await channelReader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (channelReader.TryRead(out var letter))
                {
                    if (letter == null)
                    { continue; }

                    if (_compress)
                    {
                        letter.Body = await _compressionProvider.CompressAsync(letter.Body).ConfigureAwait(false);
                        letter.LetterMetadata.Compressed = _compress;
                        letter.LetterMetadata.CustomFields[Constants.HeaderForCompressed] = _compress;
                        letter.LetterMetadata.CustomFields[Constants.HeaderForCompression] = Constants.HeaderValueForGzipCompress;
                    }

                    if (_encrypt)
                    {
                        letter.Body = _encryptionProvider.Encrypt(letter.Body);
                        letter.LetterMetadata.Encrypted = _encrypt;
                        letter.LetterMetadata.CustomFields[Constants.HeaderForEncrypted] = _encrypt;
                        letter.LetterMetadata.CustomFields[Constants.HeaderForEncryption] = Constants.HeaderValueForArgonAesEncrypt;
                        letter.LetterMetadata.CustomFields[Constants.HeaderForEncryptDate] = Time.GetDateTimeNow(Time.Formats.CatRFC3339);
                    }

                    _logger.LogDebug(LogMessages.AutoPublishers.LetterPublished, letter.LetterId, letter.LetterMetadata?.Id);

                    await PublishAsync(letter, _createPublishReceipts, _withHeaders)
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task ProcessReceiptsAsync(Func<PublishReceipt, ValueTask> processReceiptAsync)
        {
            await Task.Yield();
            await foreach (var receipt in _receiptBuffer.Reader.ReadAllAsync())
            {
                await processReceiptAsync(receipt).ConfigureAwait(false);
            }
        }

        // Super simple version to bake in requeueing of all failed to publish messages.
        private async ValueTask ProcessReceiptAsync(PublishReceipt receipt)
        {
            if (receipt.IsError && receipt.OriginalLetter != null && AutoPublisherStarted)
            {
                _logger.LogWarning($"Failed publish for letter ({receipt.OriginalLetter.LetterId}). Retrying with AutoPublishing...");

                try
                { await QueueLetterAsync(receipt.OriginalLetter); }
                catch (Exception ex) /* No-op */
                { _logger.LogDebug("Error ({0}) occurred on retry, most likely because retry during shutdown.", ex.Message); }
            }
            else if (receipt.IsError)
            {
                _logger.LogError($"Failed publish for letter ({receipt.OriginalLetter.LetterId}). Unable to retry as the original letter was not received.");
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
            IBasicProperties messageProperties = null)
        {
            Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));

            var error = false;
            var channelHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);
            if (messageProperties == null)
            {
                messageProperties = channelHost.GetChannel().CreateBasicProperties();
                messageProperties.DeliveryMode = 2;

                if (!messageProperties.IsHeadersPresent())
                {
                    messageProperties.Headers = new Dictionary<string, object>();
                }
            }

            // Non-optional Header.
            messageProperties.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForMessage;

            try
            {
                channelHost.GetChannel().BasicPublish(
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
            byte? priority = 0,
            bool mandatory = false)
        {
            Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));

            var error = false;
            var channelHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

            try
            {
                channelHost.GetChannel().BasicPublish(
                    exchange: exchangeName ?? string.Empty,
                    routingKey: routingKey,
                    mandatory: mandatory,
                    basicProperties: BuildProperties(headers, channelHost, priority),
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

            var error = false;
            var channelHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);
            if (messageProperties == null)
            {
                messageProperties = channelHost.GetChannel().CreateBasicProperties();
                messageProperties.DeliveryMode = 2;

                if (!messageProperties.IsHeadersPresent())
                {
                    messageProperties.Headers = new Dictionary<string, object>();
                }
            }

            // Non-optional Header.
            messageProperties.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForMessage;

            try
            {
                var batch = channelHost.GetChannel().CreateBasicPublishBatch();

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

            var error = false;
            var channelHost = await _channelPool.GetChannelAsync().ConfigureAwait(false);

            try
            {
                var batch = channelHost.GetChannel().CreateBasicPublishBatch();

                for (int i = 0; i < payloads.Count; i++)
                {
                    batch.Add(exchangeName, routingKey, mandatory, BuildProperties(headers, channelHost, priority), payloads[i]);
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
        /// <param name="letter"></param>
        /// <param name="createReceipt"></param>
        /// <param name="withHeaders"></param>
        public async Task PublishAsync(Letter letter, bool createReceipt, bool withHeaders = true)
        {
            var error = false;
            var chanHost = await _channelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            try
            {
                chanHost.GetChannel().BasicPublish(
                    letter.Envelope.Exchange,
                    letter.Envelope.RoutingKey,
                    letter.Envelope.RoutingOptions?.Mandatory ?? false,
                    BuildProperties(letter, chanHost, withHeaders),
                    _serializationProvider.Serialize(letter));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    LogMessages.Publishers.PublishLetterFailed,
                    $"{letter.Envelope.Exchange}->{letter.Envelope.RoutingKey}",
                    letter.LetterId,
                    ex.Message);

                error = true;
            }
            finally
            {
                if (createReceipt)
                {
                    await CreateReceiptAsync(letter, error)
                        .ConfigureAwait(false);
                }

                await _channelPool
                    .ReturnChannelAsync(chanHost, error);
            }
        }

        /// <summary>
        /// Acquires an ackable channel from the channel pool, then publishes message based on the letter/envelope parameters and waits for confirmation.
        /// <para>Only throws exception when failing to acquire channel or when creating a receipt after the ReceiptBuffer is closed.</para>
        /// <para>Not fully ready for production yet.</para>
        /// </summary>
        /// <param name="letter"></param>
        /// <param name="createReceipt"></param>
        /// <param name="withHeaders"></param>
        public async Task PublishWithConfirmationAsync(Letter letter, bool createReceipt, bool withHeaders = true)
        {
            var error = false;
            var chanHost = await _channelPool
                .GetAckChannelAsync()
                .ConfigureAwait(false);

            try
            {
                chanHost.GetChannel().WaitForConfirmsOrDie(_waitForConfirmation);

                chanHost.GetChannel().BasicPublish(
                    letter.Envelope.Exchange,
                    letter.Envelope.RoutingKey,
                    letter.Envelope.RoutingOptions?.Mandatory ?? false,
                    BuildProperties(letter, chanHost, withHeaders),
                    _serializationProvider.Serialize(letter));

                chanHost.GetChannel().WaitForConfirmsOrDie(_waitForConfirmation);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    LogMessages.Publishers.PublishLetterFailed,
                    $"{letter.Envelope.Exchange}->{letter.Envelope.RoutingKey}",
                    letter.LetterId,
                    ex.Message);

                error = true;
            }
            finally
            {
                if (createReceipt)
                {
                    await CreateReceiptAsync(letter, error)
                        .ConfigureAwait(false);
                }

                await _channelPool
                    .ReturnChannelAsync(chanHost, error);
            }
        }

        /// <summary>
        /// Use this method to sequentially publish all messages in a list in the order received.
        /// </summary>
        /// <param name="letters"></param>
        /// <param name="createReceipt"></param>
        /// <param name="withHeaders"></param>
        public async Task PublishManyAsync(IList<Letter> letters, bool createReceipt, bool withHeaders = true)
        {
            var error = false;
            var chanHost = await _channelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            for (int i = 0; i < letters.Count; i++)
            {
                try
                {
                    chanHost.GetChannel().BasicPublish(
                        letters[i].Envelope.Exchange,
                        letters[i].Envelope.RoutingKey,
                        letters[i].Envelope.RoutingOptions.Mandatory,
                        BuildProperties(letters[i], chanHost, withHeaders),
                        _serializationProvider.Serialize(letters[i]));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(
                        LogMessages.Publishers.PublishLetterFailed,
                        $"{letters[i].Envelope.Exchange}->{letters[i].Envelope.RoutingKey}",
                        letters[i].LetterId,
                        ex.Message);

                    error = true;
                }

                if (createReceipt)
                { await CreateReceiptAsync(letters[i], error).ConfigureAwait(false); }

                if (error) { break; }
            }

            await _channelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false);
        }

        /// <summary>
        /// Use this method when a group of letters who have the same properties (deliverymode, messagetype, priority).
        /// <para>Receipt with no error indicates that we successfully handed off to internal library, not necessarily published.</para>
        /// </summary>
        /// <param name="letters"></param>
        /// <param name="createReceipt"></param>
        /// <param name="withHeaders"></param>
        public async Task PublishManyAsBatchAsync(IList<Letter> letters, bool createReceipt, bool withHeaders = true)
        {
            var error = false;
            var chanHost = await _channelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            try
            {
                if (letters.Count > 0)
                {
                    var publishBatch = chanHost.GetChannel().CreateBasicPublishBatch();
                    for (int i = 0; i < letters.Count; i++)
                    {
                        publishBatch.Add(
                            letters[i].Envelope.Exchange,
                            letters[i].Envelope.RoutingKey,
                            letters[i].Envelope.RoutingOptions.Mandatory,
                            BuildProperties(letters[i], chanHost, withHeaders),
                            _serializationProvider.Serialize(letters[i]).AsMemory());

                        if (createReceipt)
                        {
                            await CreateReceiptAsync(letters[i], error).ConfigureAwait(false);
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
        private async ValueTask CreateReceiptAsync(Letter letter, bool error)
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
                .WriteAsync(new PublishReceipt { LetterId = letter.LetterId, IsError = error, OriginalLetter = error ? letter : null })
                .ConfigureAwait(false);
        }

        private static IBasicProperties BuildProperties(Letter letter, IChannelHost channelHost, bool withHeaders)
        {
            var props = channelHost.GetChannel().CreateBasicProperties();

            props.DeliveryMode = letter.Envelope.RoutingOptions.DeliveryMode;
            props.ContentType = letter.Envelope.RoutingOptions.MessageType;
            props.Priority = letter.Envelope.RoutingOptions.PriorityLevel;

            if (!props.IsHeadersPresent())
            {
                props.Headers = new Dictionary<string, object>();
            }

            if (withHeaders && letter.LetterMetadata != null)
            {
                foreach (var kvp in letter.LetterMetadata?.CustomFields)
                {
                    if (kvp.Key.StartsWith(Constants.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        props.Headers[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Non-optional Header.
            props.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForLetter;

            return props;
        }

        private static IBasicProperties BuildProperties(IDictionary<string, object> headers, IChannelHost channelHost, byte? priority = 0, byte? deliveryMode = 2)
        {
            var props = channelHost.GetChannel().CreateBasicProperties();
            props.DeliveryMode = deliveryMode ?? 2; // Default Persisted
            props.Priority = priority ?? 0; // Default Priority

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
                _letterQueue = null;
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
