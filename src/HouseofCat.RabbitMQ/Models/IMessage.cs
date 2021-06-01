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
        
        ulong GetMessageId();
        IMetadata GetMetadata();
        
        IMetadata CreateMetadataIfMissing();
        
        T GetHeader<T>(string key);
        bool RemoveHeader(string key);
        IDictionary<string, object> GetHeadersOutOfMetadata();
        
        byte[] GetBodyToPublish(ISerializationProvider serializationProvider);

        IBasicProperties BuildProperties(IChannelHost channelHost, bool withHeaders);
    }
}