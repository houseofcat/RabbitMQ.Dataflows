using HouseofCat.Compression;
using HouseofCat.Dataflows.Pipelines;
using HouseofCat.Encryption;
using HouseofCat.RabbitMQ.Dataflows;
using HouseofCat.RabbitMQ.Pipelines;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using HouseofCat.Utilities;
using HouseofCat.Utilities.Errors;
using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Services;

public interface IRabbitService
{
    IChannelPool ChannelPool { get; }
    ITopologer Topologer { get; }
    RabbitOptions Options { get; }

    string TimeFormat { get; set; }

    ISerializationProvider SerializationProvider { get; }
    IEncryptionProvider EncryptionProvider { get; }
    ICompressionProvider CompressionProvider { get; }

    IPublisher Publisher { get; }

    ConcurrentDictionary<string, IConsumer<IReceivedMessage>> Consumers { get; }
    ConcurrentDictionary<string, ConsumerOptions> ConsumerOptions { get; }

    Task ComcryptAsync(IMessage message);
    Task<bool> CompressAsync(IMessage message);

    Task DecomcryptAsync(IMessage message);
    Task<bool> DecompressAsync(IMessage message);

    bool Decrypt(IMessage message);
    bool Encrypt(IMessage message);

    Task<ReadOnlyMemory<byte>> GetAsync(string queueName);
    Task<T> GetAsync<T>(string queueName);
    IConsumer<IReceivedMessage> GetConsumer(string consumerName);

    ValueTask ShutdownAsync(bool immediately);
}

public class RabbitService : IRabbitService, IDisposable
{
    private readonly SemaphoreSlim _serviceLock = new SemaphoreSlim(1, 1);
    private bool _disposedValue;

    public RabbitOptions Options { get; }
    public IChannelPool ChannelPool { get; }
    public IPublisher Publisher { get; }
    public ITopologer Topologer { get; }

    public ISerializationProvider SerializationProvider { get; }
    public IEncryptionProvider EncryptionProvider { get; }
    public ICompressionProvider CompressionProvider { get; }

    public ConcurrentDictionary<string, IConsumer<IReceivedMessage>> Consumers { get; private set; } = new ConcurrentDictionary<string, IConsumer<IReceivedMessage>>();
    public ConcurrentDictionary<string, ConsumerOptions> ConsumerOptions { get; private set; } = new ConcurrentDictionary<string, ConsumerOptions>();

    public string TimeFormat { get; set; } = TimeHelpers.Formats.CatsAltFormat;

    public RabbitService(
        string fileNamePath,
        ISerializationProvider serializationProvider,
        IEncryptionProvider encryptionProvider = null,
        ICompressionProvider compressionProvider = null,
        ILoggerFactory loggerFactory = null, Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
        : this(
              JsonFileReader
                .ReadFileAsync<RabbitOptions>(fileNamePath)
                .GetAwaiter()
                .GetResult(),
              serializationProvider,
              encryptionProvider,
              compressionProvider,
              loggerFactory,
              processReceiptAsync)
    { }

    public RabbitService(
        RabbitOptions options,
        ISerializationProvider serializationProvider,
        IEncryptionProvider encryptionProvider = null,
        ICompressionProvider compressionProvider = null,
        ILoggerFactory loggerFactory = null,
        Func<IPublishReceipt, ValueTask> processReceiptAsync = null) : this(
            new ChannelPool(options),
            serializationProvider,
            encryptionProvider,
            compressionProvider,
            loggerFactory,
            processReceiptAsync)
    { }

    public RabbitService(
        IChannelPool chanPool,
        ISerializationProvider serializationProvider,
        IEncryptionProvider encryptionProvider = null,
        ICompressionProvider compressionProvider = null,
        ILoggerFactory loggerFactory = null,
        Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
    {
        Guard.AgainstNull(chanPool, nameof(chanPool));
        Guard.AgainstNull(serializationProvider, nameof(serializationProvider));
        LogHelpers.LoggerFactory = loggerFactory;

        Options = chanPool.Options;
        ChannelPool = chanPool;

        SerializationProvider = serializationProvider;
        EncryptionProvider = encryptionProvider;
        CompressionProvider = compressionProvider;

        Publisher = new Publisher(ChannelPool, SerializationProvider, EncryptionProvider, CompressionProvider)
        {
            TimeFormat = TimeFormat
        };

        Topologer = new Topologer(ChannelPool);

        BuildConsumers();

        Publisher.StartAutoPublish(processReceiptAsync);

        BuildConsumerTopologyAsync()
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
        foreach (var options in Options.ConsumerOptions)
        {
            ConsumerOptions.TryAdd(options.Value.ConsumerName, options.Value);
            Consumers.TryAdd(options.Value.ConsumerName, new Consumer(ChannelPool, options.Value));
        }
    }

    private async Task BuildConsumerTopologyAsync()
    {
        foreach (var consumer in Consumers)
        {
            if (!string.IsNullOrWhiteSpace(consumer.Value.ConsumerOptions.QueueName))
            {
                if (consumer.Value.ConsumerOptions.QueueArgs == null
                    || consumer.Value.ConsumerOptions.QueueArgs.Count == 0)
                {
                    await Topologer
                        .CreateQueueAsync(consumer.Value.ConsumerOptions.QueueName)
                        .ConfigureAwait(false);
                }
                else
                {
                    await Topologer
                        .CreateQueueAsync(
                            consumer.Value.ConsumerOptions.QueueName,
                            true,
                            false,
                            false,
                            consumer.Value.ConsumerOptions.QueueArgs)
                        .ConfigureAwait(false);
                }
            }

            if (!string.IsNullOrWhiteSpace(consumer.Value.ConsumerOptions.TargetQueueName))
            {
                if (consumer.Value.ConsumerOptions.TargetQueueArgs == null
                    || consumer.Value.ConsumerOptions.TargetQueueArgs.Count == 0)
                {
                    await Topologer
                        .CreateQueueAsync(consumer.Value.ConsumerOptions.TargetQueueName)
                        .ConfigureAwait(false);
                }
                else
                {
                    await Topologer
                        .CreateQueueAsync(
                            consumer.Value.ConsumerOptions.TargetQueueName,
                            true,
                            false,
                            false,
                            consumer.Value.ConsumerOptions.TargetQueueArgs)
                        .ConfigureAwait(false);
                }
            }

            if (!string.IsNullOrWhiteSpace(consumer.Value.ConsumerOptions.ErrorSuffix)
                && !string.IsNullOrWhiteSpace(consumer.Value.ConsumerOptions.ErrorQueueName))
            {
                if (consumer.Value.ConsumerOptions.ErrorQueueArgs == null
                    || consumer.Value.ConsumerOptions.ErrorQueueArgs.Count == 0)
                {
                    await Topologer
                        .CreateQueueAsync(consumer.Value.ConsumerOptions.ErrorQueueName)
                        .ConfigureAwait(false);
                }
                else
                {
                    await Topologer
                        .CreateQueueAsync(
                            consumer.Value.ConsumerOptions.ErrorQueueName,
                            true,
                            false,
                            false,
                            consumer.Value.ConsumerOptions.ErrorQueueArgs)
                        .ConfigureAwait(false);
                }
            }
        }
    }

    public IConsumer<IReceivedMessage> GetConsumer(string consumerName)
    {
        if (!Consumers.TryGetValue(consumerName, out IConsumer<IReceivedMessage> value))
        { throw new ArgumentException(string.Format(ExceptionMessages.NoConsumerOptionsMessage, consumerName)); }
        return value;
    }

    public async Task DecomcryptAsync(IMessage message)
    {
        if (message is null) return;

        var decrypted = Decrypt(message);

        if (decrypted)
        {
            await DecompressAsync(message).ConfigureAwait(false);
        }
    }

    public async Task ComcryptAsync(IMessage message)
    {
        if (message is null) return;

        await CompressAsync(message).ConfigureAwait(false);

        Encrypt(message);
    }

    public bool Encrypt(IMessage message)
    {
        if (!message.Metadata.Encrypted)
        {
            message.Body = EncryptionProvider.Encrypt(message.Body);
            message.Metadata.Encrypted = true;
            message.Metadata.Fields[Constants.HeaderForEncrypted] = true;
            message.Metadata.Fields[Constants.HeaderForEncryption] = EncryptionProvider.Type;
            message.Metadata.Fields[Constants.HeaderForEncryptDate] = TimeHelpers.GetDateTimeNow(TimeFormat);

            return true;
        }

        return false;
    }

    public bool Decrypt(IMessage message)
    {
        if (message.Metadata.Encrypted)
        {
            message.Body = EncryptionProvider.Decrypt(message.Body);
            message.Metadata.Encrypted = false;
            message.Metadata.Fields[Constants.HeaderForEncrypted] = false;

            message.Metadata.Fields.Remove(Constants.HeaderForEncryption);
            message.Metadata.Fields.Remove(Constants.HeaderForEncryptDate);

            return true;
        }

        return false;
    }

    public async Task<bool> CompressAsync(IMessage message)
    {
        if (message.Metadata.Encrypted)
        { return false; } // Don't compress after encryption.

        if (!message.Metadata.Compressed)
        {
            message.Body = (await CompressionProvider.CompressAsync(message.Body).ConfigureAwait(false)).ToArray();
            message.Metadata.Compressed = true;
            message.Metadata.Fields[Constants.HeaderForCompressed] = true;
            message.Metadata.Fields[Constants.HeaderForCompression] = CompressionProvider.Type;

            return true;
        }

        return true;
    }

    public async Task<bool> DecompressAsync(IMessage message)
    {
        if (message.Metadata.Encrypted)
        { return false; } // Don't decompress before decryption.

        if (message.Metadata.Compressed)
        {
            try
            {
                message.Body = (await CompressionProvider.DecompressAsync(message.Body).ConfigureAwait(false)).ToArray();
                message.Metadata.Compressed = false;
                message.Metadata.Fields[Constants.HeaderForCompressed] = false;

                message.Metadata.Fields.Remove(Constants.HeaderForCompression);
            }
            catch { return false; }
        }

        return true;
    }

    public async Task<ReadOnlyMemory<byte>> GetAsync(string queueName)
    {
        IChannelHost chanHost;

        try
        {
            chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);
        }
        catch { return default; }

        var error = false;
        try
        {
            var result = chanHost
                .Channel
                .BasicGet(queueName, true);

            return result.Body;
        }
        catch { error = true; }
        finally
        {
            await ChannelPool
                .ReturnChannelAsync(chanHost, error)
                .ConfigureAwait(false);
        }

        return default;
    }

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
                .Channel
                .BasicGet(queueName, true);
        }
        catch { error = true; }
        finally
        {
            await ChannelPool
                .ReturnChannelAsync(chanHost, error)
                .ConfigureAwait(false);
        }

        return result != null
            ? SerializationProvider.Deserialize<T>(result.Body)
            : default;
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
            ConsumerOptions = null;
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
