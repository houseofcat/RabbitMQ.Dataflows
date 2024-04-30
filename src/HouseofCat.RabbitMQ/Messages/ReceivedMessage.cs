using HouseofCat.Utilities.Helpers;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace HouseofCat.RabbitMQ;

public interface IReceivedMessage
{
    IMessage Message { get; set; }
    ReadOnlyMemory<byte> Body { get; set; }
    IBasicProperties Properties { get; }

    IModel Channel { get; }

    bool Ackable { get; }

    string ObjectType { get; }

    bool Encrypted { get; }
    string EncryptionType { get; }
    DateTime EncryptedDateTime { get; }

    bool Compressed { get; }
    string CompressionType { get; }

    public string TraceParentHeader { get; }
    public SpanContext? ParentSpanContext { get; set; }

    string ConsumerTag { get; }
    ulong DeliveryTag { get; }

    bool FailedToDeserialize { get; }

    bool AckMessage();

    bool NackMessage(bool requeue);
    bool RejectMessage(bool requeue);
}

public class ReceivedMessage : IReceivedMessage, IDisposable
{
    public IMessage Message { get; set; }
    public ReadOnlyMemory<byte> Body { get; set; }
    public IBasicProperties Properties { get; }

    public IModel Channel { get; private set; }

    public bool Ackable { get; }

    public string ObjectType { get; private set; }

    public bool Encrypted { get; private set; }
    public string EncryptionType { get; private set; }
    public DateTime EncryptedDateTime { get; private set; }

    public bool Compressed { get; private set; }
    public string CompressionType { get; private set; }

    public string TraceParentHeader { get; private set; }
    public SpanContext? ParentSpanContext { get; set; }

    public string ConsumerTag { get; }
    public ulong DeliveryTag { get; }

    public bool FailedToDeserialize { get; set; }

    public ReceivedMessage(
        IModel channel,
        BasicGetResult result,
        bool ackable)
    {
        Ackable = ackable;
        Channel = channel;
        DeliveryTag = result.DeliveryTag;
        Properties = result.BasicProperties;
        Body = result.Body;

        ReadHeaders();
    }

    public ReceivedMessage(
        IModel channel,
        BasicDeliverEventArgs args,
        bool ackable)
    {
        Ackable = ackable;
        Channel = channel;
        ConsumerTag = args.ConsumerTag;
        DeliveryTag = args.DeliveryTag;
        Properties = args.BasicProperties;
        Body = args.Body;

        ReadHeaders();
    }

    private void ReadHeaders()
    {
        if (Properties?.Headers is null) return;

        if (Properties.Headers.TryGetValue(Constants.HeaderForObjectType, out object objectType))
        {
            ObjectType = Encoding.UTF8.GetString((byte[])objectType);

            if (Properties.Headers.TryGetValue(Constants.HeaderForEncrypted, out object encryptedValue))
            { Encrypted = (bool)encryptedValue; }

            if (Properties.Headers.TryGetValue(Constants.HeaderForEncryption, out object encryptedType))
            { EncryptionType = Encoding.UTF8.GetString((byte[])encryptedType); }

            if (Properties.Headers.TryGetValue(Constants.HeaderForEncryptDate, out object encryptedDate))
            { EncryptedDateTime = DateTime.Parse(Encoding.UTF8.GetString((byte[])encryptedDate)); }

            if (Properties.Headers.TryGetValue(Constants.HeaderForCompressed, out object compressedValue))
            { Compressed = (bool)compressedValue; }

            if (Properties.Headers.TryGetValue(Constants.HeaderForCompression, out object compressedType))
            { CompressionType = Encoding.UTF8.GetString((byte[])compressedType); }
        }
        else
        {
            ObjectType = Constants.HeaderValueForUnknownObjectType;
        }

        if (Properties.Headers.TryGetValue(Constants.HeaderForTraceParent, out object traceParentHeader))
        {
            TraceParentHeader = Encoding.UTF8.GetString((byte[])traceParentHeader);
            if (!string.IsNullOrEmpty(TraceParentHeader))
            {
                ParentSpanContext = OpenTelemetryHelpers.ExtractSpanContextFromTraceHeader(TraceParentHeader);
            }
        }
    }

    /// <summary>
    /// Acknowledges the message server side.
    /// </summary>
    public bool AckMessage()
    {
        var success = true;

        if (Ackable)
        {
            try
            {
                Channel?.BasicAck(DeliveryTag, false);
                Channel = null;
            }
            catch { success = false; }
        }

        return success;
    }

    /// <summary>
    /// Negative Acknowledges the message server side with option to requeue.
    /// </summary>
    public bool NackMessage(bool requeue)
    {
        var success = true;

        if (Ackable)
        {
            try
            {
                Channel?.BasicNack(DeliveryTag, false, requeue);
                Channel = null;
            }
            catch { success = false; }
        }

        return success;
    }

    /// <summary>
    /// Reject Message server side with option to requeue.
    /// </summary>
    public bool RejectMessage(bool requeue)
    {
        var success = true;

        if (Ackable)
        {
            try
            {
                Channel?.BasicReject(DeliveryTag, requeue);
                Channel = null;
            }
            catch { success = false; }
        }

        return success;
    }

    private bool _disposedValue;

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (Channel is not null) { Channel = null; }
                if (Message is not null) { Message = null; }
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
