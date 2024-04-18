using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities.Random;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public static class MessageExtensions
{
    private static readonly XorShift XorShift = new XorShift(true);

    public static Message Clone(this IMessage message)
    {
        return message.ShallowClone<Message>();
    }

    public static TMessage ShallowClone<TMessage>(this IMessage message)
        where TMessage : IMessage, new()
    {
        var clonedMessage = new TMessage
        {
            Exchange = new string(message.Exchange),
            RoutingKey = new string(message.RoutingKey),
            DeliveryMode = message.DeliveryMode,
            Mandatory = message.Mandatory,
            PriorityLevel = message.PriorityLevel,

        };

        if (message.Metadata is not null)
        {
            clonedMessage.Metadata = new Metadata
            {
                PayloadId = new string(message.Metadata.PayloadId)
            };

            if (message.Metadata.Fields is not null)
            {
                clonedMessage.Metadata.Fields = new Dictionary<string, object>(message.Metadata.Fields);
            }
        }

        return clonedMessage;
    }

    public static void UpsertHeader(this IMessage message, string key, object value)
    {
        message.Metadata ??= new Metadata();
        message.Metadata.UpsertHeader(key, value);
    }

    public static void WriteHeadersToMetadata(this IMessage message, IDictionary<string, object> headers)
    {
        message.Metadata ??= new Metadata();
        foreach (var kvp in headers)
        {
            message.Metadata.UpsertHeader(kvp.Key, kvp.Value);
        }
    }

    public static IBasicProperties CreateBasicProperties(
        this IMessage message,
        IChannelHost channelHost,
        bool withOptionalHeaders,
        IMetadata metadata)
    {
        var basicProperties = channelHost.Channel.CreateBasicProperties();

        basicProperties.DeliveryMode = message.DeliveryMode;
        basicProperties.ContentType = new string(message.ContentType);
        basicProperties.Priority = message.PriorityLevel;

        basicProperties.MessageId = string.IsNullOrEmpty(message.MessageId)
            ? new string(message.MessageId)
            : Guid.NewGuid().ToString();

        if (!basicProperties.IsHeadersPresent())
        {
            basicProperties.Headers = new Dictionary<string, object>();
        }

        if (withOptionalHeaders && metadata != null)
        {
            foreach (var kvp in metadata?.Fields)
            {
                if (kvp.Key.StartsWith(Constants.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    basicProperties.Headers[kvp.Key] = kvp.Value;
                }
            }
        }

        return basicProperties;
    }

    public static IMessage CreateSimpleRandomMessage(string queueName, int bodySize = 1000)
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

    public static IList<IMessage> CreateManySimpleRandomMessages(List<string> queueNames, int messageCount, int bodySize = 1000)
    {
        var random = new Random();
        var messages = new List<IMessage>();

        var queueCount = queueNames.Count;
        for (var i = 0; i < messageCount; i++)
        {
            messages.Add(CreateSimpleRandomMessage(queueNames[random.Next(0, queueCount)], bodySize));
        }

        return messages;
    }

    public static IList<IMessage> CreateManySimpleRandomMessages(string queueName, int messageCount, int bodySize = 1000)
    {
        var messages = new List<IMessage>();

        for (var i = 0; i < messageCount; i++)
        {
            messages.Add(CreateSimpleRandomMessage(queueName, bodySize));
        }

        return messages;
    }

    public static void EnrichSpanWithTags(this IMessage message, TelemetrySpan span)
    {
        if (message == null || span == null || !span.IsRecording) return;

        span.SetAttribute(Constants.MessagingSystemKey, Constants.MessagingSystemValue);

        if (!string.IsNullOrEmpty(message.Exchange))
        {
            span.SetAttribute(Constants.MessagingDestinationNameKey, message.Exchange);
        }
        if (!string.IsNullOrEmpty(message.MessageId))
        {
            span.SetAttribute(Constants.MessagingMessageMessageIdKey, message.MessageId);
        }
        if (!string.IsNullOrEmpty(message.RoutingKey))
        {
            span.SetAttribute(Constants.MessagingMessageRoutingKeyKey, message.RoutingKey);
        }
        if (!string.IsNullOrEmpty(message.ContentType))
        {
            span.SetAttribute(Constants.MessagingMessageContentTypeKey, message.ContentType);
        }

        span.SetAttribute(Constants.MessagingMessageBodySizeKey, message.Body.Length);

        if (!string.IsNullOrEmpty(message.Metadata?.PayloadId))
        {
            span.SetAttribute(Constants.MessagingMessagePayloadIdKey, message.Metadata?.PayloadId);
        }

        var encrypted = message.Metadata?.Encrypted();
        if (encrypted.HasValue && encrypted.Value)
        {
            span.SetAttribute(Constants.MessagingMessageEncryptedKey, "true");
            span.SetAttribute(Constants.MessagingMessageEncryptedDateKey, message.Metadata?.EncryptedDate());
            span.SetAttribute(Constants.MessagingMessageEncryptionKey, message.Metadata?.EncryptionType());
        }
        var compressed = message.Metadata?.Compressed();
        if (compressed.HasValue && compressed.Value)
        {
            span.SetAttribute(Constants.MessagingMessageCompressedKey, "true");
            span.SetAttribute(Constants.MessagingMessageCompressionKey, message.Metadata?.CompressionType());
        }
    }
}
