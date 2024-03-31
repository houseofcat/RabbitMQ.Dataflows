using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ;

public interface IReceivedMessage
{
    bool Ackable { get; }
    IModel Channel { get; set; }

    string ContentType { get; }
    string ObjectType { get; }
    bool Encrypted { get; }
    string EncryptionType { get; }
    DateTime EncryptedDateTime { get; }
    bool Compressed { get; }
    string CompressionType { get; }

    ReadOnlyMemory<byte> Body { get; set; }
    string ConsumerTag { get; }
    ulong DeliveryTag { get; }
    Message Message { get; set; }

    IBasicProperties Properties { get; }

    bool AckMessage();
    void Complete();
    Task<bool> Completion { get; }

    bool NackMessage(bool requeue);
    bool RejectMessage(bool requeue);
}

public sealed class ReceivedMessage : IReceivedMessage, IDisposable
{
    /// <summary>
    /// Indicates that the content was not deserializeable based on the provided headers.
    /// </summary>
    public bool FailedToDeserialize { get; private set; }
    public IBasicProperties Properties { get; }
    public bool Ackable { get; }
    public IModel Channel { get; set; }
    public string ConsumerTag { get; }
    public ulong DeliveryTag { get; }
    public ReadOnlyMemory<byte> Body { get; set; }
    public Message Message { get; set; }

    // Retrieved From Headers
    public string ContentType { get; private set; }
    public string ObjectType { get; private set; }
    public bool Encrypted { get; private set; }
    public string EncryptionType { get; private set; }
    public DateTime EncryptedDateTime { get; private set; }
    public bool Compressed { get; private set; }
    public string CompressionType { get; private set; }
    public string TraceParentHeader { get; private set; }

    private readonly TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();
    public Task<bool> Completion => _completionSource.Task;

    private bool _disposedValue;

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

            if (Properties.Headers.TryGetValue(Constants.HeaderForObjectType, out object contentType))
            {
                ContentType = Encoding.UTF8.GetString((byte[])contentType);
            }

            if (ObjectType == Constants.HeaderValueForMessageObjectType
                && Body.Length > 0)
            {
                try
                { Message = JsonSerializer.Deserialize<Message>(Body.Span); }
                catch
                { FailedToDeserialize = true; }
            }

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
            if (Properties.Headers.TryGetValue(Constants.HeaderForContentType, out object contentType))
            {
                ContentType = Encoding.UTF8.GetString((byte[])contentType);
            }
        }

        if (Properties.Headers.TryGetValue(Constants.HeaderForTraceParent, out object traceParentHeader))
        {
            TraceParentHeader = Encoding.UTF8.GetString((byte[])traceParentHeader);
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

    /// <summary>
    /// A way to indicate this message is fully finished with.
    /// </summary>
    public void Complete() => _completionSource.SetResult(true);

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _completionSource.Task.Dispose();
            }

            if (Channel != null) { Channel = null; }
            if (Message != null) { Message = null; }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
