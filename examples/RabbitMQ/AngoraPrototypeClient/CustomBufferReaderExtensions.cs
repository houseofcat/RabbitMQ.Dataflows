using System;
using System.Collections.Generic;
using System.Text;

namespace Angora.PrototypeClient
{
    internal static class CustomBufferReaderExtensions
    {
        private static List<object> ReadArray(this ref CustomBufferReader reader)
        {
            var result = new List<object>();

            var arrayLength = reader.ReadUInt32();
            var initialBytesConsumed = reader.ConsumedBytes;

            while (reader.ConsumedBytes < arrayLength + initialBytesConsumed)
            {
                var fieldValue = reader.ReadFieldValue();

                result.Add(fieldValue);
            }

            return result;
        }

        public static Dictionary<string, object> ReadTable(this ref CustomBufferReader reader)
        {
            var result = new Dictionary<string, object>();

            var tableLength = reader.ReadUInt32();
            var initialBytesConsumed = reader.ConsumedBytes;

            while (reader.ConsumedBytes < tableLength + initialBytesConsumed)
            {
                var fieldName = reader.ReadShortString();
                var fieldValue = reader.ReadFieldValue();

                result.Add(fieldName, fieldValue);
            }

            return result;
        }

        private static object ReadFieldValue(this ref CustomBufferReader reader)
        {
            var fieldValueType = reader.ReadByte();

            switch ((char)fieldValueType)
            {
                case 't':
                    return Convert.ToBoolean(reader.ReadByte());
                case 'b':
                    return reader.ReadSByte();
                case 'B':
                    return reader.ReadByte();
                case 's':
                    return reader.ReadInt16();
                case 'u':
                    return reader.ReadUInt16();
                case 'I':
                    return reader.ReadInt32();
                case 'i':
                    return reader.ReadUInt32();
                case 'l':
                    return reader.ReadInt64();
                case 'f':
                    return reader.ReadFloat();
                case 'd':
                    return reader.ReadDouble();
                case 'D':
                    return reader.ReadDecimal();
                case 'S':
                    return reader.ReadLongString();
                case 'A':
                    return reader.ReadArray();
                case 'T':
                    return reader.ReadTimestamp();
                case 'F':
                    return reader.ReadTable();
                case 'V':
                    return null;
                case 'x':
                    return reader.ReadBytes();
                default:
                    throw new Exception($"Unknown field value type: '{fieldValueType}'.");
            }
        }

        public static string ReadShortString(this ref CustomBufferReader reader)
        {
            var length = reader.ReadByte();
            var bytes = reader.ReadBytes(length);

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public static string ReadLongString(this ref CustomBufferReader reader)
        {
            var length = reader.ReadUInt32();
            var bytes = reader.ReadBytes((int)length); // TODO check cast

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        private static byte[] ReadBytes(this ref CustomBufferReader reader)
        {
            var length = reader.ReadUInt32();
            var bytes = reader.ReadBytes((int)length); // TODO check cast

            return bytes.ToArray();
        }

        private static decimal ReadDecimal(this ref CustomBufferReader reader)
        {
            var scale = reader.ReadByte();
            var value = reader.ReadUInt32();

            return default; //TODO return real value
        }

        private static DateTime ReadTimestamp(this ref CustomBufferReader reader)
        {
            var time = reader.ReadUInt64();

            return DateTimeOffset.FromUnixTimeSeconds((long)time).DateTime; //TODO check the cast
        }

        public static MessageProperties ReadBasicProperties(this ref CustomBufferReader reader)
        {
            var properties = new MessageProperties();

            var flags = reader.ReadUInt16();

            if ((flags & 1 << 15) != 0)
            {
                properties.ContentType = reader.ReadShortString();
            }

            if ((flags & 1 << 14) != 0)
            {
                properties.ContentEncoding = reader.ReadShortString();
            }

            if ((flags & 1 << 13) != 0)
            {
                properties.Headers = reader.ReadTable();
            }

            if ((flags & 1 << 12) != 0)
            {
                properties.DeliveryMode = reader.ReadByte();
            }

            if ((flags & 1 << 11) != 0)
            {
                properties.Priority = reader.ReadByte();
            }

            if ((flags & 1 << 10) != 0)
            {
                properties.CorrelationId = reader.ReadShortString();
            }

            if ((flags & 1 << 9) != 0)
            {
                properties.ReplyTo = reader.ReadShortString();
            }

            if ((flags & 1 << 8) != 0)
            {
                properties.Expiration = reader.ReadShortString();
            }

            if ((flags & 1 << 7) != 0)
            {
                properties.MessageId = reader.ReadShortString();
            }

            if ((flags & 1 << 6) != 0)
            {
                properties.Timestamp = reader.ReadTimestamp();
            }

            if ((flags & 1 << 5) != 0)
            {
                properties.Type = reader.ReadShortString();
            }

            if ((flags & 1 << 4) != 0)
            {
                properties.UserId = reader.ReadShortString();
            }

            if ((flags & 1 << 3) != 0)
            {
                properties.AppId = reader.ReadShortString();
            }

            return properties;
        }
    }
}
