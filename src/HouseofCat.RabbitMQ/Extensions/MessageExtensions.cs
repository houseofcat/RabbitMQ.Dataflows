using System;
using System.Collections.Generic;
using HouseofCat.RabbitMQ.Pools;
using RabbitMQ.Client;

namespace HouseofCat.RabbitMQ
{
    public static class MessageExtensions
    {
        public static TMessage Clone<TMessage>(this IMessage message) 
            where TMessage : IMessage, new()
        {
            return new TMessage
            {
                Envelope = new Envelope
                {
                    Exchange = message.Envelope.Exchange,
                    RoutingKey = message.Envelope.RoutingKey,
                    RoutingOptions = new RoutingOptions
                    {
                        DeliveryMode = message.Envelope.RoutingOptions?.DeliveryMode ?? 2,
                        Mandatory = message.Envelope.RoutingOptions?.Mandatory ?? false,
                        PriorityLevel = message.Envelope.RoutingOptions?.PriorityLevel ?? 0,
                    }
                }
            };
        }

        public static void UpsertHeader(this IMessage message, string key, object value)
        {
            var metadata = message.CreateMetadataIfMissing();
            metadata.UpsertHeader(key, value);
        }
        
        public static void WriteHeadersToMetadata(this IMessage message, IDictionary<string, object> headers)
        {
            var metadata = message.CreateMetadataIfMissing();
            metadata.WriteHeadersToMetadata(headers);
        }
        
        public static IBasicProperties CreateBasicProperties(
            this IMessage message,
            IChannelHost channelHost,
            bool withOptionalHeaders,
            IMetadata metadata)
        {
            var props = channelHost.GetChannel().CreateBasicProperties();

            props.DeliveryMode = message.Envelope.RoutingOptions.DeliveryMode;
            props.ContentType = message.Envelope.RoutingOptions.MessageType;
            props.Priority = message.Envelope.RoutingOptions.PriorityLevel;
            props.MessageId = message.MessageId ?? Guid.NewGuid().ToString();

            if (!props.IsHeadersPresent())
            {
                props.Headers = new Dictionary<string, object>();
            }

            if (withOptionalHeaders && metadata != null)
            {
                foreach (var kvp in metadata?.CustomFields)
                {
                    if (kvp.Key.StartsWith(Constants.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        props.Headers[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            return props;
        }
    }
}
