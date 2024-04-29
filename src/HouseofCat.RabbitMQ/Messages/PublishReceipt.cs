using System;

namespace HouseofCat.RabbitMQ;

public interface IPublishReceipt
{
    bool IsError { get; set; }
    string MessageId { get; set; }
    IMessage OriginalMessage { get; set; }
}

public struct PublishReceipt : IPublishReceipt, IEquatable<PublishReceipt>
{
    public bool IsError { get; set; }
    public string MessageId { get; set; }
    public IMessage OriginalMessage { get; set; }

    public readonly bool Equals(PublishReceipt other)
    {
        return OriginalMessage.Body.Equals(other.OriginalMessage.Body);
    }

    public override readonly bool Equals(object obj)
    {
        return obj is PublishReceipt receipt && Equals(receipt);
    }

    public static bool operator ==(PublishReceipt left, PublishReceipt right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PublishReceipt left, PublishReceipt right)
    {
        return !left.Equals(right);
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
