using HouseofCat.RabbitMQ.Pools;
using RabbitMQ.Client;

namespace HouseofCat.RabbitMQ
{
    public interface IMessage
    {
        Envelope Envelope { get; set; }
        ulong MessageId { get; }

        IMetadata Metadata { get; set; }
        byte[] Body { get; set; }
        
        IBasicProperties BuildProperties(IChannelHost channelHost, bool withHeaders);
    }
}