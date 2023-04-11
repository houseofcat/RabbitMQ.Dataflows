using System;
using System.Threading.Tasks;
using HouseofCat.Compression;
using HouseofCat.Encryption.Providers;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using HouseofCat.Utilities.File;
using Microsoft.Extensions.Logging;
using ChannelPool = HouseofCat.RabbitMQ.Recoverable.Pools.ChannelPool;

namespace HouseofCat.RabbitMQ.Recoverable.Service;

public class RabbitService : RabbitService<Consumer>
{
    public RabbitService(
        string fileNamePath,
        ISerializationProvider serializationProvider,
        IEncryptionProvider encryptionProvider = null,
        ICompressionProvider compressionProvider = null,
        ILoggerFactory loggerFactory = null, Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
        : this(
            Utf8JsonFileReader.ReadFileAsync<RabbitOptions>(fileNamePath).GetAwaiter().GetResult(),
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
        Func<IPublishReceipt, ValueTask> processReceiptAsync = null) : base(
            chanPool,
            serializationProvider,
            encryptionProvider,
            compressionProvider,
            loggerFactory,
            processReceiptAsync)
    { }

    protected override Consumer CreateConsumer(ConsumerOptions consumerOptions) => new(ChannelPool, consumerOptions);
}