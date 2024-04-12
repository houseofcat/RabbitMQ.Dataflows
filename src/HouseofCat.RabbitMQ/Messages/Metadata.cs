using System;
using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public interface IMetadata
{
    string PayloadId { get; }

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

        return Fields.TryGetValue(Constants.HeaderForEncrypted, out var value) && (bool)value;
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

        return Fields.TryGetValue(Constants.HeaderForCompressed, out var value) && (bool)value;
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
