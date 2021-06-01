using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using RabbitMQ.Client;

namespace HouseofCat.RabbitMQ
{
    public interface IMessage
    {
        Envelope Envelope { get; set; }
        ulong MessageId { get; }

        IMetadata Metadata { get; set; }
        byte[] Body { get; set; }
        
        byte[] GetBodyToPublish(ISerializationProvider serializationProvider);

        IBasicProperties BuildProperties(IChannelHost channelHost, bool withHeaders);
    }
}