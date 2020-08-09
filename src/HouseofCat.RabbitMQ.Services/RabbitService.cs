using HouseofCat.Compression.Builtin;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Logger;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.RabbitMQ.Pipelines;
using HouseofCat.Utilities.File;
using HouseofCat.Utilities.Time;
using HouseofCat.Workflows.Pipelines;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HouseofCat.Compression;

namespace HouseofCat.RabbitMQ.Services
{
    public interface IRabbitService
    {
        IPublisher Publisher { get; }
        IChannelPool ChannelPool { get; }
        Options Options { get; }
        ConcurrentDictionary<string, IConsumer<ReceivedData>> Consumers { get; }
        ITopologer Topologer { get; }

        Task ComcryptAsync(Letter letter);
        Task<bool> CompressAsync(Letter letter);
        IConsumerPipeline<TOut> CreateConsumerPipeline<TOut>(string consumerName, int batchSize, bool? ensureOrdered, Func<int, bool?, IPipeline<ReceivedData, TOut>> pipelineBuilder) where TOut : IWorkState;
        IConsumerPipeline<TOut> CreateConsumerPipeline<TOut>(string consumerName, IPipeline<ReceivedData, TOut> pipeline) where TOut : IWorkState;

        /// <summary>
        /// Use this CreateConsumerPipeline function when using the new ConsumerOption config that includes ConsumerPipeline settings.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="consumerName"></param>
        /// <param name="pipelineBuilder"></param>
        /// <returns></returns>
        IConsumerPipeline<TOut> CreateConsumerPipeline<TOut>(string consumerName, Func<int, bool?, IPipeline<ReceivedData, TOut>> pipelineBuilder) where TOut : IWorkState;

        Task DecomcryptAsync(Letter letter);
        Task<bool> DecompressAsync(Letter letter);
        bool Decrypt(Letter letter);
        bool Encrypt(Letter letter);
        Task<ReadOnlyMemory<byte>?> GetAsync(string queueName);
        Task<T> GetAsync<T>(string queueName);
        IConsumer<ReceivedData> GetConsumer(string consumerName);
        IConsumer<ReceivedData> GetConsumerByPipelineName(string consumerPipelineName);

        ValueTask ShutdownAsync(bool immediately);
    }

    public class RabbitService : IRabbitService, IDisposable
    {
        private readonly SemaphoreSlim _serviceLock = new SemaphoreSlim(1, 1);
        private bool _disposedValue;

        public Options Options { get; }
        public IChannelPool ChannelPool { get; }
        public IPublisher Publisher { get; }
        public ITopologer Topologer { get; }
        public IEncryptionProvider EncryptionProvider { get; }
        public ICompressionProvider CompressionProvider { get; }

        public ConcurrentDictionary<string, IConsumer<ReceivedData>> Consumers { get; private set; } = new ConcurrentDictionary<string, IConsumer<ReceivedData>>();
        private ConcurrentDictionary<string, ConsumerOptions> ConsumerPipelineNameToConsumerOptions { get; set; } = new ConcurrentDictionary<string, ConsumerOptions>();

        /// <summary>
        /// Reads config from a provided file name path. Builds out a RabbitService with instantiated dependencies based on config settings.
        /// </summary>
        /// <param name="fileNamePath"></param>
        /// <param name="passphrase"></param>
        /// <param name="salt"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="processReceiptAsync"></param>
        public RabbitService(string fileNamePath, IEncryptionProvider encryptionProvider, ICompressionProvider compressionProvider, ILoggerFactory loggerFactory = null, Func<PublishReceipt, ValueTask> processReceiptAsync = null)
            : this(
                  JsonFileReader
                    .ReadFileAsync<Options>(fileNamePath)
                    .GetAwaiter()
                    .GetResult(),
                  encryptionProvider,
                  compressionProvider,
                  loggerFactory,
                  processReceiptAsync)
        { }

        /// <summary>
        /// Builds out a RabbitService with instantiated dependencies based on config settings.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="passphrase"></param>
        /// <param name="salt"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="processReceiptAsync"></param>
        public RabbitService(Options options, IEncryptionProvider encryptionProvider, ICompressionProvider compressionProvider, ILoggerFactory loggerFactory = null, Func<PublishReceipt, ValueTask> processReceiptAsync = null)
        {
            LogHelper.LoggerFactory = loggerFactory;

            Options = options;
            ChannelPool = new ChannelPool(Options);

            EncryptionProvider = EncryptionProvider;
            CompressionProvider = CompressionProvider;

            Publisher = new Publisher(ChannelPool, EncryptionProvider, CompressionProvider);
            Topologer = new Topologer(ChannelPool);

            Options.ApplyGlobalConsumerOptions();
            BuildConsumers();

            Publisher
                .StartAutoPublishAsync(processReceiptAsync)
                .GetAwaiter()
                .GetResult();

            BuildConsumerTopology()
                .GetAwaiter()
                .GetResult();
        }

        public async ValueTask ShutdownAsync(bool immediately)
        {
            await _serviceLock.WaitAsync().ConfigureAwait(false);

            try
            {
                await Publisher
                    .StopAutoPublishAsync(immediately)
                    .ConfigureAwait(false);

                await StopAllConsumers(immediately)
                    .ConfigureAwait(false);

                await ChannelPool
                    .ShutdownAsync()
                    .ConfigureAwait(false);
            }
            finally
            { _serviceLock.Release(); }
        }

        private async ValueTask StopAllConsumers(bool immediately)
        {
            foreach (var kvp in Consumers)
            {
                await kvp
                    .Value
                    .StopConsumerAsync(immediately)
                    .ConfigureAwait(false);
            }
        }

        private void BuildConsumers()
        {
            foreach (var consumerSetting in Options.ConsumerOptions)
            {
                if (!string.IsNullOrEmpty(consumerSetting.Value.ConsumerPipelineOptions.ConsumerPipelineName))
                {
                    ConsumerPipelineNameToConsumerOptions.TryAdd(consumerSetting.Value.ConsumerPipelineOptions.ConsumerPipelineName, consumerSetting.Value);
                }
                Consumers.TryAdd(consumerSetting.Value.ConsumerName, new Consumer(ChannelPool, consumerSetting.Value));
            }
        }

        private async Task BuildConsumerTopology()
        {
            foreach (var consumer in Consumers)
            {
                if (!string.IsNullOrWhiteSpace(consumer.Value.ConsumerOptions.QueueName))
                {
                    await Topologer.CreateQueueAsync(consumer.Value.ConsumerOptions.QueueName).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(consumer.Value.ConsumerOptions.ErrorQueueName))
                {
                    await Topologer.CreateQueueAsync(consumer.Value.ConsumerOptions.ErrorQueueName).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(consumer.Value.ConsumerOptions.TargetQueueName))
                {
                    await Topologer.CreateQueueAsync(consumer.Value.ConsumerOptions.TargetQueueName).ConfigureAwait(false);
                }
            }
        }

        public IConsumerPipeline<TOut> CreateConsumerPipeline<TOut>(
            string consumerName,
            int batchSize,
            bool? ensureOrdered,
            Func<int, bool?, IPipeline<ReceivedData, TOut>> pipelineBuilder)
            where TOut : IWorkState
        {
            var consumer = GetConsumer(consumerName);
            var pipeline = pipelineBuilder.Invoke(batchSize, ensureOrdered);

            return new ConsumerPipeline<TOut>(consumer, pipeline);
        }

        /// <summary>
        /// Use this CreateConsumerPipeline function when using the new ConsumerOption config that includes ConsumerPipeline settings.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="consumerName"></param>
        /// <param name="pipelineBuilder"></param>
        /// <returns></returns>
        public IConsumerPipeline<TOut> CreateConsumerPipeline<TOut>(
            string consumerName,
            Func<int, bool?, IPipeline<ReceivedData, TOut>> pipelineBuilder)
            where TOut : IWorkState
        {
            var consumer = GetConsumer(consumerName);
            var pipeline = pipelineBuilder.Invoke(
                consumer.ConsumerOptions.ConsumerPipelineOptions.MaxDegreesOfParallelism ?? 1,
                consumer.ConsumerOptions.ConsumerPipelineOptions.EnsureOrdered ?? true);

            return new ConsumerPipeline<TOut>(consumer, pipeline);
        }

        public IConsumerPipeline<TOut> CreateConsumerPipeline<TOut>(
            string consumerName,
            IPipeline<ReceivedData, TOut> pipeline)
            where TOut : IWorkState
        {
            var consumer = GetConsumer(consumerName);

            return new ConsumerPipeline<TOut>(consumer, pipeline);
        }

        public IConsumer<ReceivedData> GetConsumer(string consumerName)
        {
            if (!Consumers.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.NoConsumerOptionsMessage, consumerName));
            return Consumers[consumerName];
        }

        public IConsumer<ReceivedData> GetConsumerByPipelineName(string consumerPipelineName)
        {
            if (!ConsumerPipelineNameToConsumerOptions.ContainsKey(consumerPipelineName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.NoConsumerPipelineOptionsMessage, consumerPipelineName));
            if (!Consumers.ContainsKey(ConsumerPipelineNameToConsumerOptions[consumerPipelineName].ConsumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.NoConsumerOptionsMessage, ConsumerPipelineNameToConsumerOptions[consumerPipelineName].ConsumerName));
            return Consumers[ConsumerPipelineNameToConsumerOptions[consumerPipelineName].ConsumerName];
        }

        public async Task DecomcryptAsync(Letter letter)
        {
            var decrypted = Decrypt(letter);

            if (decrypted)
            {
                await DecompressAsync(letter).ConfigureAwait(false);
            }
        }

        public async Task ComcryptAsync(Letter letter)
        {
            await CompressAsync(letter).ConfigureAwait(false);

            Encrypt(letter);
        }

        // Returns Success
        public bool Encrypt(Letter letter)
        {
            if (!letter.LetterMetadata.Encrypted)
            {
                letter.Body = EncryptionProvider.Encrypt(letter.Body);
                letter.LetterMetadata.Encrypted = true;
                return true;
            }

            return false;
        }

        // Returns Success
        public bool Decrypt(Letter letter)
        {
            if (letter.LetterMetadata.Encrypted)
            {
                letter.Body = EncryptionProvider.Decrypt(letter.Body);
                letter.LetterMetadata.Encrypted = false;
                return true;
            }

            return false;
        }

        // Returns Success
        public async Task<bool> CompressAsync(Letter letter)
        {
            if (letter.LetterMetadata.Encrypted)
            { return false; } // Don't compress after encryption.

            if (!letter.LetterMetadata.Compressed)
            {
                letter.Body = await CompressionProvider.CompressAsync(letter.Body);
                letter.LetterMetadata.Compressed = true;
                letter.LetterMetadata.CustomFields[Constants.HeaderForCompressed] = true;
                if (letter.LetterMetadata.CustomFields.ContainsKey(Constants.HeaderForCompressed))
                {
                    letter.LetterMetadata.CustomFields.Remove(Constants.HeaderForCompressed);
                }

                if (letter.LetterMetadata.CustomFields.ContainsKey(Constants.HeaderForCompression))
                {
                    letter.LetterMetadata.CustomFields.Remove(Constants.HeaderForCompression);
                }

                return true;
            }

            return true;
        }

        // Returns Success
        public async Task<bool> DecompressAsync(Letter letter)
        {
            if (letter.LetterMetadata.Encrypted)
            { return false; } // Don't decompress before decryption.

            if (letter.LetterMetadata.Compressed)
            {
                try
                {
                    letter.Body = await CompressionProvider.DecompressAsync(letter.Body);
                    letter.LetterMetadata.Compressed = false;

                    if (letter.LetterMetadata.CustomFields.ContainsKey(Constants.HeaderForCompressed))
                    {
                        letter.LetterMetadata.CustomFields.Remove(Constants.HeaderForCompressed);
                    }

                    if (letter.LetterMetadata.CustomFields.ContainsKey(Constants.HeaderForCompression))
                    {
                        letter.LetterMetadata.CustomFields.Remove(Constants.HeaderForCompression);
                    }
                }
                catch { return false; }
            }

            return true;
        }

        /// <summary>
        /// Simple retrieve message (byte[]) from queue. Null if nothing was available or on error.
        /// <para>AutoAcks message.</para>
        /// </summary>
        /// <param name="queueName"></param>
        public async Task<ReadOnlyMemory<byte>?> GetAsync(string queueName)
        {
            IChannelHost chanHost;

            try
            {
                chanHost = await ChannelPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);
            }
            catch { return default; }

            BasicGetResult result = null;
            var error = false;
            try
            {
                result = chanHost
                    .GetChannel()
                    .BasicGet(queueName, true);
            }
            catch { error = true; }
            finally
            {
                await ChannelPool
                    .ReturnChannelAsync(chanHost, error)
                    .ConfigureAwait(false);
            }

            return result?.Body;
        }

        /// <summary>
        /// Simple retrieve message (byte[]) from queue and convert to type T. Default (assumed null) if nothing was available (or on transmission error).
        /// <para>AutoAcks message.</para>
        /// </summary>
        /// <param name="queueName"></param>
        public async Task<T> GetAsync<T>(string queueName)
        {
            IChannelHost chanHost;

            try
            {
                chanHost = await ChannelPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);
            }
            catch { return default; }

            BasicGetResult result = null;
            var error = false;
            try
            {
                result = chanHost
                    .GetChannel()
                    .BasicGet(queueName, true);
            }
            catch { error = true; }
            finally
            {
                await ChannelPool
                    .ReturnChannelAsync(chanHost, error)
                    .ConfigureAwait(false);
            }

            return result != null ? JsonSerializer.Deserialize<T>(result.Body.Span) : default;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _serviceLock.Dispose();
                }

                Consumers = null;
                ConsumerPipelineNameToConsumerOptions = null;
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
