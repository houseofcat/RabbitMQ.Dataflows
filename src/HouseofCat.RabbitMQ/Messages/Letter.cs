using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HouseofCat.RabbitMQ;

public interface IMessage
{
    string MessageId { get; set; }
    Envelope Envelope { get; set; }
    ReadOnlyMemory<byte> Body { get; set; }
    ActivityContext? ActivityContext { get; set; }

    IMetadata GetMetadata();

    IMetadata CreateMetadataIfMissing();

    T GetHeader<T>(string key);
    bool RemoveHeader(string key);
    IDictionary<string, object> GetHeadersOutOfMetadata();

    ReadOnlyMemory<byte> GetBodyToPublish(ISerializationProvider serializationProvider);

    IPublishReceipt GetPublishReceipt(bool error);

    IBasicProperties BuildProperties(IChannelHost channelHost, bool withOptionalHeaders);
}

public class Letter : IMessage
{
    public Envelope Envelope { get; set; }
    public string MessageId { get; set; }
    public ActivityContext? ActivityContext { get; set; }

    public LetterMetadata LetterMetadata { get; set; }
    public ReadOnlyMemory<byte> Body { get; set; }


    public IBasicProperties BuildProperties(IChannelHost channelHost, bool withOptionalHeaders)
    {
        MessageId ??= Guid.NewGuid().ToString();

        var props = this.CreateBasicProperties(channelHost, withOptionalHeaders, LetterMetadata);
        props.MessageId = MessageId;

        // Non-optional Header.
        props.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForMessage;

        return props;
    }

    public Letter() { }

    public Letter(string exchange, string routingKey, ReadOnlyMemory<byte> data, LetterMetadata metadata = null, RoutingOptions routingOptions = null)
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

    public Letter(string exchange, string routingKey, ReadOnlyMemory<byte> data, string id, RoutingOptions routingOptions = null)
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

    public IMetadata GetMetadata() => LetterMetadata;

    public IMetadata CreateMetadataIfMissing()
    {
        LetterMetadata ??= new LetterMetadata();
        return LetterMetadata;
    }

    public T GetHeader<T>(string key) => LetterMetadata.GetHeader<T>(key);
    public bool RemoveHeader(string key) => LetterMetadata.RemoveHeader(key);
    public IDictionary<string, object> GetHeadersOutOfMetadata() => LetterMetadata.GetHeadersOutOfMetadata();

    public ReadOnlyMemory<byte> GetBodyToPublish(ISerializationProvider serializationProvider) =>
        serializationProvider.Serialize(this).ToArray();

    public IPublishReceipt GetPublishReceipt(bool error) =>
        new PublishReceipt { MessageId = MessageId, IsError = error, OriginalLetter = error ? this : null };
}
