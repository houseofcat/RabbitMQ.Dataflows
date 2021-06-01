using System.Collections.Generic;
using HouseofCat.RabbitMQ.Pools;
using RabbitMQ.Client;

namespace HouseofCat.RabbitMQ
{
    public class Letter : IMessage
    {
        public Envelope Envelope { get; set; }
        public ulong MessageId { get; set; }

        public IMetadata Metadata { get; set; }
        public byte[] Body { get; set; }
        
        public IBasicProperties BuildProperties(IChannelHost channelHost, bool withHeaders)
        {
            var props = this.CreateBasicProperties(channelHost, withHeaders);
            
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
            Metadata = metadata ?? new LetterMetadata();
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
            { Metadata = new LetterMetadata { Id = id }; }
            else
            { Metadata = new LetterMetadata(); }
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
            { Metadata = new LetterMetadata { Id = id }; }
            else
            { Metadata = new LetterMetadata(); }
        }

        public Letter Clone() => this.Clone<Letter>();

        public void UpsertHeader(string key, object value) => this.UpsertHeader<LetterMetadata>(key, value);

        public void WriteHeadersToMetadata(IDictionary<string, object> headers) =>
            this.WriteHeadersToMetadata<LetterMetadata>(headers);
    }
}
