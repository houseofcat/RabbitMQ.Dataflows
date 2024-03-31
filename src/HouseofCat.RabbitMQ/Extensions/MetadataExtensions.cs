using HouseofCat.Utilities.Errors;
using System;
using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public static class MetadataExtensions
{
    public static T GetHeader<T>(this IMetadata metadata, string key)
    {
        Guard.AgainstNull(metadata, nameof(Metadata));
        Guard.AgainstNullOrEmpty(metadata.Fields, nameof(Metadata.Fields));

        if (metadata.Fields.TryGetValue(key, out object value))
        {
            if (value is T temp)
            { return temp; }
            else { throw new InvalidCastException(); }
        }

        return default;
    }

    public static void UpsertHeader(this IMetadata metadata, string key, object value)
    {
        metadata.Fields ??= new Dictionary<string, object>();
        metadata.Fields[key] = value;
    }

    public static bool RemoveHeader(this IMetadata metadata, string key)
    {
        Guard.AgainstNull(metadata, nameof(Metadata));
        Guard.AgainstNullOrEmpty(metadata.Fields, nameof(Metadata.Fields));

        return metadata
            .Fields
            .Remove(key);
    }

    public static IDictionary<string, object> GetHeadersFromMetadata(this IMetadata metadata)
    {
        Guard.AgainstNull(metadata, nameof(Metadata));
        Guard.AgainstNullOrEmpty(metadata.Fields, nameof(Metadata.Fields));

        return new Dictionary<string, object>(metadata.Fields);
    }

    public static void WriteHeadersToMetadata(this IMetadata metadata, IDictionary<string, object> headers)
    {
        if (metadata.Fields is null)
        {
            metadata.Fields ??= new Dictionary<string, object>(headers);
            return;
        }

        foreach (var kvp in headers)
        {
            if (kvp.Key.StartsWith(Constants.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                metadata.Fields[kvp.Key] = kvp.Value;
            }
        }
    }
}