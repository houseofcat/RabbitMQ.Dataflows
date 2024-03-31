using HouseofCat.Utilities.Errors;
using System;
using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public static class MetadataExtensions
{
    public static T Clone<T>(this IMetadata metadata)
        where T : IMetadata, new()
    {
        var clonedMetadata = new T
        {
            Compressed = metadata.Compressed,
            Encrypted = metadata.Encrypted
        };

        foreach (var kvp in metadata.CustomFields)
        {
            clonedMetadata.CustomFields.Add(kvp.Key, kvp.Value);
        }

        return clonedMetadata;
    }

    public static T GetHeader<T>(this IMetadata metadata, string key)
    {
        Guard.AgainstNull(metadata, nameof(Metadata));
        Guard.AgainstNullOrEmpty(metadata.CustomFields, nameof(Metadata.CustomFields));

        if (metadata.CustomFields.TryGetValue(key, out object value))
        {
            if (value is T temp)
            { return temp; }
            else { throw new InvalidCastException(); }
        }

        return default;
    }

    public static void UpsertHeader(this IMetadata metadata, string key, object value)
    {
        metadata.CustomFields ??= new Dictionary<string, object>();

        metadata.CustomFields[key] = value;
    }

    public static bool RemoveHeader(this IMetadata metadata, string key)
    {
        Guard.AgainstNull(metadata, nameof(Metadata));
        Guard.AgainstNullOrEmpty(metadata.CustomFields, nameof(Metadata.CustomFields));

        return metadata
            .CustomFields
            .Remove(key);
    }

    public static IDictionary<string, object> GetHeadersOutOfMetadata(this IMetadata metadata)
    {
        Guard.AgainstNull(metadata, nameof(Metadata));
        Guard.AgainstNullOrEmpty(metadata.CustomFields, nameof(Metadata.CustomFields));

        var dict = new Dictionary<string, object>();

        foreach (var kvp in metadata.CustomFields)
        {
            if (kvp.Key.StartsWith(Constants.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        return dict;
    }

    public static void WriteHeadersToMetadata(this IMetadata metadata, IDictionary<string, object> headers)
    {
        metadata.CustomFields ??= new Dictionary<string, object>();

        foreach (var kvp in headers)
        {
            if (kvp.Key.StartsWith(Constants.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                metadata.CustomFields[kvp.Key] = kvp.Value;
            }
        }
    }
}