using System.Collections.Generic;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using RabbitMQ.Client;

namespace HouseofCat.RabbitMQ
{
    public class Letter : IMessage
    {
        public Envelope Envelope { get; set; }
        public ulong LetterId { get; set; }

        public IMetadata LetterMetadata { get; set; }
        public byte[] Body { get; set; }
        
        public IBasicProperties BuildProperties(IChannelHost channelHost, bool withHeaders)
        {
            var props = this.CreateBasicProperties(channelHost, withHeaders, LetterMetadata);
            
            // Non-optional Header.
            props.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForLetter;

            return props;
        }

        public Letter() { }

        public Letter(string exchange, string routingKey, byte[] data, IMetadata metadata = null, RoutingOptions routingOptions = null)
        {
            Envelope = new Envelope
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                RoutingOptions = routingOptions ?? RoutingOptions.CreateDefaultRoutingOptions()
            };
            Body = data;
            LetterMetadata = metadata ?? new LetterMetadata();
        }

        public Letter(string exchange, string routingKey, byte[] data, string id, RoutingOptions routingOptions = null)
        {
            Envelope = new Envelope
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                RoutingOptions = routingOptions ?? RoutingOptions.CreateDefaultRoutingOptions()
            };
            Body = data;
            if (!string.IsNullOrWhiteSpace(id))
            { LetterMetadata = new LetterMetadata { Id = id }; }
            else
            { LetterMetadata = new LetterMetadata(); }
        }

        public Letter(string exchange, string routingKey, byte[] data, string id, byte priority)
        {
            Envelope = new Envelope
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                RoutingOptions = RoutingOptions.CreateDefaultRoutingOptions(priority)
            };
            Body = data;
            if (!string.IsNullOrWhiteSpace(id))
            { LetterMetadata = new LetterMetadata { Id = id }; }
            else
            { LetterMetadata = new LetterMetadata(); }
        }

        public Letter Clone()
        {
            var clone = this.Clone<Letter>();
            clone.LetterMetadata = LetterMetadata.Clone<LetterMetadata>();
            return clone;
        }

        public ulong GetMessageId() => LetterId;
        public IMetadata GetMetadata() => LetterMetadata;

        public IMetadata CreateMetadataIfMissing()
        {
            LetterMetadata ??= new LetterMetadata();
            return LetterMetadata;
        }
        
        public T GetHeader<T>(string key) => LetterMetadata.GetHeader<T>(key);
        public bool RemoveHeader(string key) => LetterMetadata.RemoveHeader(key);
        public IDictionary<string, object> GetHeadersOutOfMetadata() => LetterMetadata.GetHeadersOutOfMetadata();

        public byte[] GetBodyToPublish(ISerializationProvider serializationProvider) => 
            serializationProvider.Serialize(this);
        
        public IPublishReceipt GetPublishReceipt(bool error) => 
            new PublishReceipt { LetterId = GetMessageId(), IsError = error, OriginalLetter = error ? this : null };
    }
}
