using System.Collections.Generic;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using RabbitMQ.Client;

namespace HouseofCat.RabbitMQ
{
    public interface IMessage
    {
        Envelope Envelope { get; set; }

        byte[] Body { get; set; }
        
        IMetadata CreateMetadataIfMissing();
        
        ulong GetMessageId();
        IMetadata GetMetadata();
        
        IDictionary<string, object> GetHeadersOutOfMetadata();
        
        byte[] GetBodyToPublish(ISerializationProvider serializationProvider);

        IBasicProperties BuildProperties(IChannelHost channelHost, bool withHeaders);
    }
}