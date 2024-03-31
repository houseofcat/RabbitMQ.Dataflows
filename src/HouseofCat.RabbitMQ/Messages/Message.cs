using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities.Helpers;
using RabbitMQ.Client;
using System;
using System.ComponentModel.DataAnnotations;

namespace HouseofCat.RabbitMQ;

public interface IMessage
{
    string MessageId { get; set; }
    string Exchange { get; set; }
    string RoutingKey { get; set; }

    byte DeliveryMode { get; set; }

    bool Mandatory { get; set; }

    byte PriorityLevel { get; set; }

    string ContentType { get; set; }

    ReadOnlyMemory<byte> Body { get; set; }

    IMetadata Metadata { get; set; }

    IPublishReceipt GetPublishReceipt(bool error);

    IBasicProperties BuildProperties(IChannelHost channelHost, bool withOptionalHeaders);
}

public sealed class Message : IMessage
{
    public string MessageId { get; set; }

    public string Exchange { get; set; }
    public string RoutingKey { get; set; }

    [Range(1, 2, ErrorMessage = Constants.RangeErrorMessage)]
    public byte DeliveryMode { get; set; } = 2;

    public bool Mandatory { get; set; }

    // The max-queue priority though is 10, so > 10 is treated as 10.
    [Range(0, 10, ErrorMessage = Constants.RangeErrorMessage)]
    public byte PriorityLevel { get; set; }

    public string ContentType { get; set; } = Constants.HeaderValueForContentTypeJson;

    public IMetadata Metadata { get; set; }
    public ReadOnlyMemory<byte> Body { get; set; }

    public Message() { }

    public Message(
        string exchange,
        string routingKey,
        ReadOnlyMemory<byte> body,
        Metadata metadata = null)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
        Body = body;
        Metadata = metadata ?? new Metadata();
    }

    public Message(string exchange, string routingKey, ReadOnlyMemory<byte> body, string payloadId)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
        Body = body;

        if (!string.IsNullOrWhiteSpace(payloadId))
        { Metadata = new Metadata { PayloadId = payloadId }; }
        else
        { Metadata = new Metadata(); }
    }

    public IBasicProperties BuildProperties(IChannelHost channelHost, bool withOptionalHeaders)
    {
        MessageId ??= Guid.NewGuid().ToString();

        var basicProperties = this.CreateBasicProperties(channelHost, withOptionalHeaders, Metadata);
        basicProperties.MessageId = MessageId;

        // Non-optional Header.
        basicProperties.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForMessageObjectType;
        var openTelHeader = OpenTelemetryHelpers.CreateOpenTelemetryHeaderFromCurrentActivityOrDefault();
        basicProperties.Headers[Constants.HeaderForTraceParent] = openTelHeader;

        return basicProperties;
    }

    public IMetadata CreateMetadataIfMissing()
    {
        Metadata ??= new Metadata();
        return Metadata;
    }

    public IPublishReceipt GetPublishReceipt(bool error) =>
        new PublishReceipt { MessageId = MessageId, IsError = error, OriginalMessage = error ? this : null };
}
