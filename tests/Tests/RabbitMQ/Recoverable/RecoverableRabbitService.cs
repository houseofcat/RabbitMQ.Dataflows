using System;
using System.Threading.Tasks;
using HouseofCat.Compression;
using HouseofCat.Encryption.Providers;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;

namespace IntegrationTests.RabbitMQ.Recoverable;

public class RecoverableRabbitService : RabbitService
{
    public RecoverableRabbitService(
        IChannelPool chanPool, 
        ISerializationProvider serializationProvider, 
        IEncryptionProvider encryptionProvider = null, 
        ICompressionProvider compressionProvider = null, 
        ILoggerFactory loggerFactory = null, 
        Func<IPublishReceipt, ValueTask> processReceiptAsync = null)
        : base(chanPool, serializationProvider, encryptionProvider, compressionProvider, loggerFactory, processReceiptAsync)
    {
    }

    protected override IConsumer<ReceivedData> CreateConsumer(ConsumerOptions consumerOptions) => 
        new RecoverableConsumer(ChannelPool, consumerOptions);
}