using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Helpers;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
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

    IMetadata GetMetadata();

    IMetadata CreateMetadataIfMissing();

    T GetHeader<T>(string key);
    bool RemoveHeader(string key);
    IDictionary<string, object> GetHeadersOutOfMetadata();

    ReadOnlyMemory<byte> GetBodyToPublish(ISerializationProvider serializationProvider);

    IPublishReceipt GetPublishReceipt(bool error);

    IBasicProperties BuildProperties(IChannelHost channelHost, bool withOptionalHeaders);
}

public class Message : IMessage
{
    public string MessageId { get; set; }

    public string Exchange { get; set; }
    public string RoutingKey { get; set; }

    [Range(1, 2, ErrorMessage = Constants.RangeErrorMessage)]
    public byte DeliveryMode { get; set; } = 2;

    public bool Mandatory { get; set; }

    // Max Priority letter level is 255, however, the max-queue priority though is 10, so > 10 is treated as 10.
    [Range(0, 10, ErrorMessage = Constants.RangeErrorMessage)]
    public byte PriorityLevel { get; set; }

    public string ContentType { get; set; } = Constants.HeaderValueForContentTypeApplicationJson;

    public IMetadata Metadata { get; set; }
    public ReadOnlyMemory<byte> Body { get; set; }

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

    public Message() { }

    public Message(
        string exchange,
        string routingKey,
        ReadOnlyMemory<byte> data,
        Metadata metadata = null)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
        Body = data;
        Metadata = metadata ?? new Metadata();
    }

    public Message(string exchange, string routingKey, ReadOnlyMemory<byte> data, string id)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
        Body = data;

        if (!string.IsNullOrWhiteSpace(id))
        { Metadata = new Metadata { Id = id }; }
        else
        { Metadata = new Metadata(); }
    }

    public Message(string exchange, string routingKey, byte[] data, string id, byte priority)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
        Body = data;
        if (!string.IsNullOrWhiteSpace(id))
        { Metadata = new Metadata { Id = id }; }
        else
        { Metadata = new Metadata(); }
    }

    public Message Clone()
    {
        var clone = this.Clone<Message>();
        clone.Metadata = Metadata.Clone<Metadata>();
        return clone;
    }

    public IMetadata GetMetadata() => Metadata;

    public IMetadata CreateMetadataIfMissing()
    {
        Metadata ??= new Metadata();
        return Metadata;
    }

    public T GetHeader<T>(string key) => Metadata.GetHeader<T>(key);
    public bool RemoveHeader(string key) => Metadata.RemoveHeader(key);
    public IDictionary<string, object> GetHeadersOutOfMetadata() => Metadata.GetHeadersOutOfMetadata();

    public ReadOnlyMemory<byte> GetBodyToPublish(ISerializationProvider serializationProvider) =>
        serializationProvider.Serialize(this).ToArray();

    public IPublishReceipt GetPublishReceipt(bool error) =>
        new PublishReceipt { MessageId = MessageId, IsError = error, OriginalLetter = error ? this : null };
}
