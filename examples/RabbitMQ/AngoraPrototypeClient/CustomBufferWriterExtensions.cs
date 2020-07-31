using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;

namespace RabbitMQ.Core.Prototype
{
    internal static class CustomBufferWriterExtensions
    {
        public static Span<byte> WriteFrameHeader(this ref CustomBufferWriter<PipeWriter> writer, byte frameType, ushort channel)
        {
            writer.Write(frameType);
            writer.Write(channel);

            writer.Ensure(sizeof(uint));
            var sizeBookmark = writer.Span;
            writer.Advance(sizeof(uint));

            return sizeBookmark;
        }

        public static void WriteShortString(this ref CustomBufferWriter<PipeWriter> writer, string value)
        {
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }

            var valueBytes = Encoding.UTF8.GetBytes(value);

            if (valueBytes.Length > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "value is too long for a short string");
            }

            writer.Write((byte)valueBytes.Length);
            writer.Write(valueBytes);
        }

        public static void WriteLongString(this ref CustomBufferWriter<PipeWriter> writer, string value)
        {
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }

            var valueBytes = Encoding.UTF8.GetBytes(value);

            writer.Write((uint)valueBytes.Length);
            writer.Write(valueBytes);
        }

        public static void WriteBits(this ref CustomBufferWriter<PipeWriter> writer, bool bit0 = false, bool bit1 = false, bool bit2 = false, bool bit3 = false, bool bit4 = false, bool bit5 = false, bool bit6 = false, bool bit7 = false)
        {
            byte bits = 0;

            bits |= (byte)(Convert.ToInt32(bit0) << 0);
            bits |= (byte)(Convert.ToInt32(bit1) << 1);
            bits |= (byte)(Convert.ToInt32(bit2) << 2);
            bits |= (byte)(Convert.ToInt32(bit3) << 3);
            bits |= (byte)(Convert.ToInt32(bit4) << 4);
            bits |= (byte)(Convert.ToInt32(bit5) << 5);
            bits |= (byte)(Convert.ToInt32(bit6) << 6);
            bits |= (byte)(Convert.ToInt32(bit7) << 7);

            writer.Write(bits);
        }

        private static void WriteArray(this ref CustomBufferWriter<PipeWriter> writer, List<object> value)
        {
            writer.Ensure(sizeof(uint));
            var sizeBookmark = writer.Span;
            writer.Advance(sizeof(uint));

            writer.Commit();
            var before = (uint)writer.BytesCommitted;

            if (value != null)
            {
                foreach (var item in value)
                {
                    writer.WriteFieldValue(item);
                }
            }

            writer.Commit();
            BinaryPrimitives.WriteUInt32BigEndian(sizeBookmark, (uint)writer.BytesCommitted - before);
        }

        public static void WriteTable(this ref CustomBufferWriter<PipeWriter> writer, Dictionary<string, object> value)
        {
            writer.Ensure(sizeof(uint));
            var sizeBookmark = writer.Span;
            writer.Advance(sizeof(uint));

            writer.Commit();
            var before = (uint)writer.BytesCommitted;

            if (value != null)
            {
                foreach (var item in value)
                {
                    writer.WriteShortString(item.Key);
                    writer.WriteFieldValue(item.Value);
                }
            }

            writer.Commit();
            BinaryPrimitives.WriteUInt32BigEndian(sizeBookmark, (uint)writer.BytesCommitted - before);
        }

        private static void WriteFieldValue(this ref CustomBufferWriter<PipeWriter> writer, object value)
        {
            switch (value)
            {
                case null:
                    writer.Write((byte)'V');
                    break;
                case bool t:
                    writer.Write((byte)'t');
                    writer.Write(t ? (byte)1 : (byte)0);
                    break;
                case sbyte b:
                    writer.Write((byte)'b');
                    writer.Write(b);
                    break;
                case byte B:
                    writer.Write((byte)'B');
                    writer.Write(B);
                    break;
                case short s:
                    writer.Write((byte)'s');
                    writer.Write(s);
                    break;
                case ushort u:
                    writer.Write((byte)'u');
                    writer.Write(u);
                    break;
                case int I:
                    writer.Write((byte)'I');
                    writer.Write(I);
                    break;
                case uint i:
                    writer.Write((byte)'i');
                    writer.Write(i);
                    break;
                case long l:
                    writer.Write((byte)'l');
                    writer.Write(l);
                    break;
                case float f:
                    writer.Write((byte)'f');
                    writer.Write(f);
                    break;
                case double d:
                    writer.Write((byte)'f');
                    writer.Write(d);
                    break;
                case decimal D:
                    writer.Write((byte)'D');
                    writer.WriteDecimal(D);
                    break;
                case string S:
                    writer.Write((byte)'S');
                    writer.WriteLongString(S);
                    break;
                case List<object> A:
                    writer.Write((byte)'A');
                    writer.WriteArray(A);
                    break;
                case DateTime T:
                    writer.Write((byte)'T');
                    writer.WriteTimestamp(T);
                    break;
                case Dictionary<string, object> F:
                    writer.Write((byte)'F');
                    writer.WriteTable(F);
                    break;
                case byte[] x:
                    writer.Write((byte)'x');
                    writer.WriteBytes(x);
                    break;
                default:
                    throw new Exception($"Unknown field value type: '{value.GetType()}'.");
            }
        }

        private static void WriteDecimal(this ref CustomBufferWriter<PipeWriter> writer, decimal value)
        {
            //TODO write real values

            writer.Write((byte)0); //scale
            writer.Write((uint)0); //value
        }

        private static void WriteTimestamp(this ref CustomBufferWriter<PipeWriter> writer, DateTime value)
        {
            var dateTimeOffset = new DateTimeOffset(value);
            var timestamp = (ulong)dateTimeOffset.ToUnixTimeSeconds();
            writer.Write(timestamp);
        }

        private static void WriteBytes(this ref CustomBufferWriter<PipeWriter> writer, byte[] value)
        {
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }

            writer.Write((byte)value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                writer.Write(value[i]);
            }
        }

        public static void WriteBasicProperties(this ref CustomBufferWriter<PipeWriter> writer, MessageProperties properties)
        {
            writer.Ensure(sizeof(ushort));
            var flagsBookmark = writer.Span;
            writer.Advance(sizeof(ushort));

            var flags = (ushort)0;

            if (properties.ContentType != null)
            {
                flags |= 1 << 15;
                writer.WriteShortString(properties.ContentType);
            }

            if (properties.ContentEncoding != null)
            {
                flags |= 1 << 14;
                writer.WriteShortString(properties.ContentEncoding);
            }

            if (properties.Headers != null)
            {
                flags |= 1 << 13;
                writer.WriteTable(properties.Headers);
            }

            if (properties.DeliveryMode != 0)
            {
                flags |= 1 << 12;
                writer.Write(properties.DeliveryMode);
            }

            if (properties.Priority != 0)
            {
                flags |= 1 << 11;
                writer.Write(properties.Priority);
            }

            if (properties.CorrelationId != null)
            {
                flags |= 1 << 10;
                writer.WriteShortString(properties.CorrelationId);
            }

            if (properties.ReplyTo != null)
            {
                flags |= 1 << 9;
                writer.WriteShortString(properties.ReplyTo);
            }

            if (properties.Expiration != null)
            {
                flags |= 1 << 8;
                writer.WriteShortString(properties.Expiration);
            }

            if (properties.MessageId != null)
            {
                flags |= 1 << 7;
                writer.WriteShortString(properties.MessageId);
            }

            if (properties.Timestamp != default)
            {
                flags |= 1 << 6;
                writer.WriteTimestamp(properties.Timestamp);
            }

            if (properties.Type != null)
            {
                flags |= 1 << 5;
                writer.WriteShortString(properties.Type);
            }

            if (properties.UserId != null)
            {
                flags |= 1 << 4;
                writer.WriteShortString(properties.UserId);
            }

            if (properties.AppId != null)
            {
                flags |= 1 << 3;
                writer.WriteShortString(properties.AppId);
            }

            BinaryPrimitives.WriteUInt16BigEndian(flagsBookmark, flags);
        }
    }
}
