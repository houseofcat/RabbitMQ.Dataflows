using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Tasks;

using static RabbitMQ.Core.Prototype.AmqpConstants;

namespace RabbitMQ.Core.Prototype
{
    internal class BasicMethods
    {
        private readonly Socket socket;
        private readonly ushort channelNumber;
        private readonly uint maxContentBodySize;

        internal BasicMethods(Socket socket, ushort channelNumber, uint maxContentBodySize)
        {
            this.socket = socket;
            this.channelNumber = channelNumber;
            this.maxContentBodySize = maxContentBodySize;
        }

        public async Task Send_Qos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            var buffer = await socket.GetWriteBuffer().ConfigureAwait(false);

            try
            {
                WritePayload();
                await buffer.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                socket.ReleaseWriteBuffer();
            }

            void WritePayload()
            {
                var writer = new CustomBufferWriter<PipeWriter>(buffer);

                var payloadSizeHeader = writer.WriteFrameHeader(FrameType.Method, channelNumber);

                writer.Write(Method.Basic.Qos);
                writer.Write(prefetchSize);
                writer.Write(prefetchCount);
                writer.WriteBits(global);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_Consume(string queue, string consumerTag, bool autoAck, bool exclusive, Dictionary<string, object> arguments)
        {
            var buffer = await socket.GetWriteBuffer().ConfigureAwait(false);

            try
            {
                WritePayload();
                await buffer.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                socket.ReleaseWriteBuffer();
            }

            void WritePayload()
            {
                var writer = new CustomBufferWriter<PipeWriter>(buffer);

                var payloadSizeHeader = writer.WriteFrameHeader(FrameType.Method, channelNumber);

                writer.Write(Method.Basic.Consume);
                writer.Write(Reserved);
                writer.Write(Reserved);
                writer.WriteShortString(queue);
                writer.WriteShortString(consumerTag);
                writer.WriteBits(false, autoAck, exclusive);
                writer.WriteTable(arguments);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_Cancel(string consumerTag)
        {
            var buffer = await socket.GetWriteBuffer().ConfigureAwait(false);

            try
            {
                WritePayload();
                await buffer.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                socket.ReleaseWriteBuffer();
            }

            void WritePayload()
            {
                var writer = new CustomBufferWriter<PipeWriter>(buffer);

                var payloadSizeHeader = writer.WriteFrameHeader(FrameType.Method, channelNumber);

                writer.Write(Method.Basic.Cancel);
                writer.WriteShortString(consumerTag);
                writer.WriteBits();

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_Recover()
        {
            var buffer = await socket.GetWriteBuffer().ConfigureAwait(false);

            try
            {
                WritePayload();
                await buffer.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                socket.ReleaseWriteBuffer();
            }

            void WritePayload()
            {
                var writer = new CustomBufferWriter<PipeWriter>(buffer);

                var payloadSizeHeader = writer.WriteFrameHeader(FrameType.Method, channelNumber);

                writer.Write(Method.Basic.Recover);
                writer.WriteBits(true);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_Ack(ulong deliveryTag, bool multiple)
        {
            var buffer = await socket.GetWriteBuffer().ConfigureAwait(false);

            try
            {
                WritePayload();
                await buffer.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                socket.ReleaseWriteBuffer();
            }

            void WritePayload()
            {
                var writer = new CustomBufferWriter<PipeWriter>(buffer);

                var payloadSizeHeader = writer.WriteFrameHeader(FrameType.Method, channelNumber);

                writer.Write(Method.Basic.Ack);
                writer.Write(deliveryTag);
                writer.WriteBits(multiple);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_Publish(string exchange, string routingKey, bool mandatory, MessageProperties properties, Memory<byte> bodyBytes)
        {
            var buffer = await socket.GetWriteBuffer().ConfigureAwait(false);

            try
            {
                WritePayload();
                await buffer.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                socket.ReleaseWriteBuffer();
            }

            void WritePayload()
            {
                var writer = new CustomBufferWriter<PipeWriter>(buffer);

                var payloadSizeHeader = writer.WriteFrameHeader(FrameType.Method, channelNumber);

                writer.Write(Method.Basic.Publish);
                writer.Write(Reserved);
                writer.Write(Reserved);
                writer.WriteShortString(exchange);
                writer.WriteShortString(routingKey);
                writer.WriteBits(mandatory);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                var body = bodyBytes.Span;

                WriteContentHeaderFrame(ref writer, properties, (ulong)body.Length);

                var framesToWrite = body.Length > 0;

                while (framesToWrite)
                {
                    Span<byte> frame;

                    if (body.Length > maxContentBodySize)
                    {
                        frame = body.Slice(0, (int)maxContentBodySize);
                        body = body.Slice((int)maxContentBodySize);
                    }
                    else
                    {
                        frame = body;
                        framesToWrite = false;
                    }

                    WriteContentBodyFrame(ref writer, frame);
                }

                writer.Commit();
            }
        }

        private void WriteContentHeaderFrame(ref CustomBufferWriter<PipeWriter> writer, MessageProperties properties, ulong length)
        {
            var payloadSizeHeader = writer.WriteFrameHeader(FrameType.ContentHeader, channelNumber);

            writer.Commit();
            var bytesWrittenBefore = (uint)writer.BytesCommitted;

            writer.Write(ClassId.Basic);
            writer.Write(Reserved);
            writer.Write(Reserved);
            writer.Write(length);
            writer.WriteBasicProperties(properties);

            writer.Commit();
            BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - bytesWrittenBefore);

            writer.Write(FrameEnd);
        }

        private void WriteContentBodyFrame(ref CustomBufferWriter<PipeWriter> writer, Span<byte> body)
        {
            var payloadSizeHeader = writer.WriteFrameHeader(FrameType.ContentBody, channelNumber);

            writer.Commit();
            var bytesWrittenBefore = (uint)writer.BytesCommitted;

            writer.Write(body);

            writer.Commit();
            BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - bytesWrittenBefore);

            writer.Write(FrameEnd);
        }
    }
}
