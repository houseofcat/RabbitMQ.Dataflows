using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities.Random;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public static class MessageExtensions
{
    private static readonly XorShift XorShift = new XorShift(true);

    public static TMessage Clone<TMessage>(this IMessage message)
        where TMessage : IMessage, new()
    {
        return new TMessage
        {
            Exchange = new string(message.Exchange),
            RoutingKey = new string(message.RoutingKey),
            DeliveryMode = message.DeliveryMode,
            Mandatory = message.Mandatory,
            PriorityLevel = message.PriorityLevel
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

        props.DeliveryMode = message.DeliveryMode;
        props.ContentType = new string(message.ContentType);
        props.Priority = message.PriorityLevel;
        props.MessageId = message.MessageId == null
            ? new string(message.MessageId)
            : Guid.NewGuid().ToString();

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

    public static IMessage CreateSimpleRandomLetter(string queueName, int bodySize = 1000)
    {
        var payload = new byte[bodySize];
        XorShift.FillBuffer(payload, 0, bodySize);

        return new Message
        {
            MessageId = Guid.NewGuid().ToString(),
            Metadata = new Metadata(),
            Exchange = string.Empty,
            RoutingKey = new string(queueName),
            DeliveryMode = 1,
            PriorityLevel = 0,
            Body = payload
        };
    }

    public static IList<IMessage> CreateManySimpleRandomLetters(List<string> queueNames, int letterCount, int bodySize = 1000)
    {
        var random = new Random();
        var letters = new List<IMessage>();

        var queueCount = queueNames.Count;
        for (var i = 0; i < letterCount; i++)
        {
            letters.Add(CreateSimpleRandomLetter(queueNames[random.Next(0, queueCount)], bodySize));
        }

        return letters;
    }

    public static IList<IMessage> CreateManySimpleRandomLetters(string queueName, int letterCount, int bodySize = 1000)
    {
        var letters = new List<IMessage>();

        for (var i = 0; i < letterCount; i++)
        {
            letters.Add(CreateSimpleRandomLetter(queueName, bodySize));
        }

        return letters;
    }
}
