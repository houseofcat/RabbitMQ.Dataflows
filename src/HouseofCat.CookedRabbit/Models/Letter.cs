using CookedRabbit.Core.Utils;
using HouseofCat.Utilities.Errors;
using System;
using System.Collections.Generic;

namespace CookedRabbit.Core
{
    public class Letter
    {
        public Envelope Envelope { get; set; }
        public ulong LetterId { get; set; }

        public LetterMetadata LetterMetadata { get; set; }
        public byte[] Body { get; set; }

        public Letter() { }

        public Letter(string exchange, string routingKey, byte[] data, LetterMetadata metadata = null, RoutingOptions routingOptions = null)
        {
            Envelope = new Envelope
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                RoutingOptions = routingOptions ?? DefaultRoutingOptions()
            };
            Body = data;
            LetterMetadata = metadata ?? new LetterMetadata();
        }

        public Letter(string exchange, string routingKey, byte[] data, string id, RoutingOptions routingOptions = null)
        {
            Envelope = new Envelope
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                RoutingOptions = routingOptions ?? DefaultRoutingOptions()
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
                RoutingOptions = DefaultRoutingOptions(priority)
            };
            Body = data;
            if (!string.IsNullOrWhiteSpace(id))
            { LetterMetadata = new LetterMetadata { Id = id }; }
            else
            { LetterMetadata = new LetterMetadata(); }
        }

        public static RoutingOptions DefaultRoutingOptions(byte priority = 0)
        {
            return new RoutingOptions
            {
                DeliveryMode = 2,
                Mandatory = false,
                PriorityLevel = priority
            };
        }

        public Letter Clone()
        {
            var metadata = new LetterMetadata
            {
                Compressed = LetterMetadata.Compressed,
                Encrypted = LetterMetadata.Encrypted,
            };

            foreach (var kvp in LetterMetadata.CustomFields)
            {
                metadata.CustomFields.Add(kvp.Key, kvp.Value);
            }

            return new Letter
            {
                Envelope = new Envelope
                {
                    Exchange = Envelope.Exchange,
                    RoutingKey = Envelope.RoutingKey,
                    RoutingOptions = new RoutingOptions
                    {
                        DeliveryMode = Envelope.RoutingOptions?.DeliveryMode ?? 2,
                        Mandatory = Envelope.RoutingOptions?.Mandatory ?? false,
                        PriorityLevel = Envelope.RoutingOptions?.PriorityLevel ?? 0,
                    }
                },
                LetterMetadata = metadata
            };
        }

        public T GetHeader<T>(string key)
        {
            Guard.AgainstNull(LetterMetadata, nameof(LetterMetadata));
            Guard.AgainstNullOrEmpty(LetterMetadata.CustomFields, nameof(LetterMetadata.CustomFields));

            if (LetterMetadata.CustomFields.ContainsKey(key))
            {
                if (LetterMetadata.CustomFields[key] is T temp)
                { return temp; }
                else { throw new InvalidCastException(); }
            }

            return default;
        }

        public void UpsertHeader(string key, object value)
        {
            if (LetterMetadata == null)
            { LetterMetadata = new LetterMetadata(); }

            if (LetterMetadata.CustomFields == null)
            { LetterMetadata.CustomFields = new Dictionary<string, object>(); }

            LetterMetadata.CustomFields[key] = value;
        }

        public bool RemoveHeader(string key)
        {
            Guard.AgainstNull(LetterMetadata, nameof(LetterMetadata));
            Guard.AgainstNullOrEmpty(LetterMetadata.CustomFields, nameof(LetterMetadata.CustomFields));

            return LetterMetadata
                .CustomFields
                .Remove(key);
        }

        public IDictionary<string, object> GetHeadersOutOfMetadata()
        {
            Guard.AgainstNull(LetterMetadata, nameof(LetterMetadata));
            Guard.AgainstNullOrEmpty(LetterMetadata.CustomFields, nameof(LetterMetadata.CustomFields));

            var dict = new Dictionary<string, object>();

            foreach (var kvp in LetterMetadata.CustomFields)
            {
                if (kvp.Key.StartsWith(Constants.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }

            return dict;
        }

        public void WriteHeadersToMetadata(IDictionary<string, object> headers)
        {
            if (LetterMetadata == null)
            { LetterMetadata = new LetterMetadata(); }

            if (LetterMetadata.CustomFields == null)
            { LetterMetadata.CustomFields = new Dictionary<string, object>(); }

            foreach (var kvp in headers)
            {
                if (kvp.Key.StartsWith(Constants.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    LetterMetadata.CustomFields[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}
