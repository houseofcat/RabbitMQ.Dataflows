using System;
using System.Collections.Generic;
using HouseofCat.RabbitMQ.Pools;
using RabbitMQ.Client;

namespace HouseofCat.RabbitMQ
{
    public static class MessageExtensions
    {
        public static TMessage Clone<TMessage, TMetadata>(this IMessage message) 
            where TMessage : IMessage, new()
            where TMetadata : IMetadata, new()
        {
            var metadata = message.Metadata.Clone<TMetadata>();

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
                },
                Metadata = metadata
            };
        }

        public static T GetHeader<T>(this IMessage message, string key) => message.Metadata.GetHeader<T>(key);
        
        public static bool RemoveHeader(this IMessage message, string key) => message.Metadata.RemoveHeader(key);
        
        public static void UpsertHeader<TMetadata>(this IMessage message, string key, object value)
            where TMetadata: IMetadata, new()
        {
            if (message.Metadata == null)
            { message.Metadata = new TMetadata(); }
            
            message.Metadata.UpsertHeader(key, value);
        }

        public static IDictionary<string, object> GetHeadersOutOfMetadata(this IMessage message) => 
            message.Metadata.GetHeadersOutOfMetadata();
        
        public static void WriteHeadersToMetadata<TMetadata>(this IMessage message, IDictionary<string, object> headers)
            where TMetadata: IMetadata, new()
        {
            if (message.Metadata == null)
            { message.Metadata = new TMetadata(); }
            
            message.Metadata.WriteHeadersToMetadata(headers);
        }
        
        public static IBasicProperties CreateBasicProperties(this IMessage message, IChannelHost channelHost, bool withHeaders)
        {
            var props = channelHost.GetChannel().CreateBasicProperties();

            props.DeliveryMode = message.Envelope.RoutingOptions.DeliveryMode;
            props.ContentType = message.Envelope.RoutingOptions.MessageType;
            props.Priority = message.Envelope.RoutingOptions.PriorityLevel;

            if (!props.IsHeadersPresent())
            {
                props.Headers = new Dictionary<string, object>();
            }

            if (withHeaders && message.Metadata != null)
            {
                foreach (var kvp in message.Metadata?.CustomFields)
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