using System;
using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public interface IMetadata
{
    string PayloadId { get; set; }

    Dictionary<string, object> Fields { get; set; }
}

public sealed class Metadata : IMetadata
{
    /// <summary>
    /// PayloadId is a user supplied identifier that allows identifying the Body without needing to deserialize.
    /// </summary>
    public string PayloadId { get; set; }

    public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();

    public bool Encrypted()
    {
        if (Fields == null) return false;

        if (Fields.TryGetValue(Constants.HeaderForEncrypted, out var value))
        {
            return (bool)value;
        }

        return false;
    }

    public string EncryptionType()
    {
        if (Fields == null) return null;

        if (Fields.TryGetValue(Constants.HeaderForEncryption, out var value))
        {
            return (string)value;
        }

        return null;
    }

    public string EncryptedDate()
    {
        if (Fields == null) return null;

        if (Fields.TryGetValue(Constants.HeaderForEncryptDate, out var value))
        {
            return (string)value;
        }

        return null;
    }

    public DateTime? EncryptedDateTime()
    {
        if (Fields == null) return null;

        if (Fields.TryGetValue(Constants.HeaderForEncryptDate, out var value)
            && DateTime.TryParse((string)value, out var dateTime))
        {
            return dateTime;
        }

        return null;
    }

    public bool Compressed()
    {
        if (Fields == null) return false;

        if (Fields.TryGetValue(Constants.HeaderForCompressed, out var value))
        {
            return (bool)value;
        }

        return false;
    }

    public string CompressionType()
    {
        if (Fields == null) return null;

        if (Fields.TryGetValue(Constants.HeaderForCompression, out var value))
        {
            return (string)value;
        }

        return null;
    }
}
