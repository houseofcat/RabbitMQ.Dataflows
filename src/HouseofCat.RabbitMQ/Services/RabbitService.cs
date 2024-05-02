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

    Task StartAsync(Func<IPublishReceipt, ValueTask> processReceiptAsync = null);
    ValueTask ShutdownAsync(bool immediately);
}

public class RabbitService : IRabbitService, IDisposable
{
    private readonly SemaphoreSlim _serviceLock = new SemaphoreSlim(1, 1);

    public RabbitOptions Options { get; }
    public IChannelPool ChannelPool { get; private set; }
    public IPublisher Publisher { get; private set; }
    public ITopologer Topologer { get; private set; }

    public ISerializationProvider SerializationProvider { get; }
    public IEncryptionProvider EncryptionProvider { get; }
    public ICompressionProvider CompressionProvider { get; }

    public ConcurrentDictionary<string, IConsumer<IReceivedMessage>> Consumers { get; private set; } = new ConcurrentDictionary<string, IConsumer<IReceivedMessage>>();
    public ConcurrentDictionary<string, ConsumerOptions> ConsumerOptions { get; private set; } = new ConcurrentDictionary<string, ConsumerOptions>();

    public string TimeFormat { get; set; } = TimeHelpers.Formats.RFC3339Long;

    public RabbitService(
        RabbitOptions options,
        ISerializationProvider serializationProvider,
        IEncryptionProvider encryptionProvider = null,
        ICompressionProvider compressionProvider = null,
        ILoggerFactory loggerFactory = null)
    {
        Guard.AgainstNull(options, nameof(options));
        Guard.AgainstNull(serializationProvider, nameof(serializationProvider));
        LogHelpers.LoggerFactory = loggerFactory;

        Options = options;
        SerializationProvider = serializationProvider;
        EncryptionProvider = encryptionProvider;
        CompressionProvider = compressionProvider;
    }

    public RabbitService(
        IChannelPool channelPool,
        ISerializationProvider serializationProvider,
        IEncryptionProvider encryptionProvider = null,
        ICompressionProvider compressionProvider = null,
        ILoggerFactory loggerFactory = null)
    {
        Guard.AgainstNull(channelPool, nameof(channelPool));
        Guard.AgainstNull(serializationProvider, nameof(serializationProvider));
        LogHelpers.LoggerFactory = loggerFactory;

        Options = channelPool.Options;
        ChannelPool = channelPool;

        SerializationProvider = serializationProvider;
        EncryptionProvider = encryptionProvider;
        CompressionProvider = compressionProvider;

        Publisher = new Publisher(ChannelPool, SerializationProvider, EncryptionProvider, CompressionProvider)
        {
            TimeFormat = TimeFormat
        };

        Topologer = new Topologer(ChannelPool);
    }

    private bool _started;

    /// <summary>
    /// Starts the RabbitService connection pool, channel pool, and auto-publishing. It prepares consumers and builds the consumer topology so they
    /// are ready to go.
    /// <para>If you pass in a processReceiptAsync function, it will be used to process receipts from the auto-publishing instead of the default method.</para>
    /// </summary>
    /// <param name="processReceiptAsync"></param>
    /// <returns></returns>
    public async Task StartAsync(Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
    {
        if (!await _serviceLock.WaitAsync(0).ConfigureAwait(false)) return;

        try
        {
            if (_started) return;

            ChannelPool ??= new ChannelPool(Options);

            Publisher ??= new Publisher(ChannelPool, SerializationProvider, EncryptionProvider, CompressionProvider)
            {
                TimeFormat = TimeFormat
            };

            Topologer ??= new Topologer(ChannelPool);

            BuildConsumers();
            await BuildConsumerTopologyAsync();
            await Publisher.StartAutoPublishAsync(processReceiptAsync);

            _started = true;
        }
        finally
        { _serviceLock.Release(); }
    }

    public async ValueTask ShutdownAsync(bool immediately)
    {
        if (!await _serviceLock.WaitAsync(0).ConfigureAwait(false)) return;

        try
        {
            if (!_started) return;

            await Publisher
                .StopAutoPublishAsync(immediately)
                .ConfigureAwait(false);

            await StopAllConsumers(immediately)
                .ConfigureAwait(false);

            await ChannelPool
                .ShutdownAsync()
                .ConfigureAwait(false);

            _started = false;
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
            if (!consumer.Value.ConsumerOptions.BuildQueues)
            { continue; }

            if (!string.IsNullOrWhiteSpace(consumer.Value.ConsumerOptions.QueueName))
            {
                await Topologer
                    .CreateQueueAsync(
                        consumer.Value.ConsumerOptions.QueueName,
                        consumer.Value.ConsumerOptions.BuildQueueDurable,
                        consumer.Value.ConsumerOptions.BuildQueueExclusive,
                        consumer.Value.ConsumerOptions.BuildQueueAutoDelete,
                        consumer.Value.ConsumerOptions.QueueArgs)
                    .ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(consumer.Value.ConsumerOptions.SendQueueName))
            {
                await Topologer
                    .CreateQueueAsync(
                        consumer.Value.ConsumerOptions.SendQueueName,
                        consumer.Value.ConsumerOptions.BuildQueueDurable,
                        consumer.Value.ConsumerOptions.BuildQueueExclusive,
                        consumer.Value.ConsumerOptions.BuildQueueAutoDelete,
                        consumer.Value.ConsumerOptions.SendQueueArgs)
                    .ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(consumer.Value.ConsumerOptions.ErrorQueueName))
            {
                await Topologer
                    .CreateQueueAsync(
                        consumer.Value.ConsumerOptions.ErrorQueueName,
                        consumer.Value.ConsumerOptions.BuildQueueDurable,
                        consumer.Value.ConsumerOptions.BuildQueueExclusive,
                        consumer.Value.ConsumerOptions.BuildQueueAutoDelete,
                        consumer.Value.ConsumerOptions.ErrorQueueArgs)
                    .ConfigureAwait(false);
            }
        }
    }

    private static readonly string _noConsumerOptionsMessage = "Consumer {0} options not found in Options.ConsumerOptions.";

    public IConsumer<IReceivedMessage> GetConsumer(string consumerName)
    {
        if (!Consumers.TryGetValue(consumerName, out IConsumer<IReceivedMessage> value))
        { throw new ArgumentException(string.Format(_noConsumerOptionsMessage, consumerName)); }
        return value;
    }

    public async Task ComcryptAsync(IMessage message)
    {
        if (message is null || message.Body.Length == 0) return;

        await CompressAsync(message).ConfigureAwait(false);
        Encrypt(message);
    }

    public async Task DecomcryptAsync(IMessage message)
    {
        if (message is null || message.Body.Length == 0) return;

        Decrypt(message);
        await DecompressAsync(message).ConfigureAwait(false);
    }

    public bool Encrypt(IMessage message)
    {
        if (EncryptionProvider is null || message.Metadata.Encrypted())
        {
            return false;
        }

        message.Body = EncryptionProvider.Encrypt(message.Body);
        message.Metadata.Fields[Constants.HeaderForEncrypted] = true;
        message.Metadata.Fields[Constants.HeaderForEncryption] = EncryptionProvider.Type;
        message.Metadata.Fields[Constants.HeaderForEncryptDate] = TimeHelpers.GetDateTimeNow(TimeFormat);
        return true;
    }

    public bool Decrypt(IMessage message)
    {
        if (EncryptionProvider is null || !message.Metadata.Encrypted())
        {
            return false;
        }

        message.Body = EncryptionProvider.Decrypt(message.Body);
        message.Metadata.Fields[Constants.HeaderForEncrypted] = false;
        message.Metadata.Fields.Remove(Constants.HeaderForEncryption);
        message.Metadata.Fields.Remove(Constants.HeaderForEncryptDate);
        return true;
    }

    public async Task<bool> CompressAsync(IMessage message)
    {
        if (CompressionProvider is null
            || message.Metadata.Encrypted()
            || message.Metadata.Compressed())
        {
            return false;
        }

        try
        {
            message.Body = await CompressionProvider.CompressAsync(message.Body).ConfigureAwait(false);
            message.Metadata.Fields[Constants.HeaderForCompressed] = true;
            message.Metadata.Fields[Constants.HeaderForCompression] = CompressionProvider.Type;
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> DecompressAsync(IMessage message)
    {
        if (CompressionProvider is null
            || message.Metadata.Encrypted()
            || !message.Metadata.Compressed())
        {
            return false;
        }

        try
        {
            message.Body = await CompressionProvider.DecompressAsync(message.Body).ConfigureAwait(false);
            message.Metadata.Fields[Constants.HeaderForCompressed] = false;
            message.Metadata.Fields.Remove(Constants.HeaderForCompression);
            return true;
        }
        catch { return false; }
    }

    public async Task<ReadOnlyMemory<byte>> GetAsync(string queueName)
    {
        using var span = OpenTelemetryHelpers.StartActiveSpan(
            nameof(GetAsync),
            SpanKind.Consumer);

        var chanHost = await ChannelPool
            .GetChannelAsync()
            .ConfigureAwait(false);

        var error = false;
        try
        {
            var result = chanHost
                .Channel
                .BasicGet(queueName, true);

            return result.Body;
        }
        catch (Exception ex)
        {
            OpenTelemetryHelpers.SetSpanAsError(span, ex);
            error = true;
        }
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
        using var span = OpenTelemetryHelpers.StartActiveSpan(
            nameof(GetAsync),
            SpanKind.Consumer);

        var chanHost = await ChannelPool
            .GetChannelAsync()
            .ConfigureAwait(false);

        BasicGetResult result = null;
        var error = false;
        try
        {
            result = chanHost
                .Channel
                .BasicGet(queueName, true);
        }
        catch (Exception ex)
        {
            OpenTelemetryHelpers.SetSpanAsError(span, ex);
            error = true;
        }
        finally
        {
            await ChannelPool
                .ReturnChannelAsync(chanHost, error)
                .ConfigureAwait(false);
        }

        return result is not null
            ? SerializationProvider.Deserialize<T>(result.Body)
            : default;
    }

    private bool _disposedValue;

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
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
