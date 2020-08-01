using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Tasks;

using static Angora.PrototypeClient.AmqpConstants;

namespace Angora.PrototypeClient
{
    internal class QueueMethods
    {
        private readonly Socket socket;
        private readonly ushort channelNumber;

        internal QueueMethods(Socket socket, ushort channelNumber)
        {
            this.socket = socket;
            this.channelNumber = channelNumber;
        }

        public async Task Send_Declare(string queueName, bool passive, bool durable, bool exclusive, bool autoDelete, Dictionary<string, object> arguments)
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

                writer.Write(Method.Queue.Declare);
                writer.Write(Reserved);
                writer.Write(Reserved);
                writer.WriteShortString(queueName);
                writer.WriteBits(passive, durable, exclusive, autoDelete);
                writer.WriteTable(arguments);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_Bind(string queue, string exchange, string routingKey, Dictionary<string, object> arguments)
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

                writer.Write(Method.Queue.Bind);
                writer.Write(Reserved);
                writer.Write(Reserved);
                writer.WriteShortString(queue);
                writer.WriteShortString(exchange);
                writer.WriteShortString(routingKey);
                writer.WriteBits();
                writer.WriteTable(arguments);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_Unbind(string queue, string exchange, string routingKey, Dictionary<string, object> arguments)
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

                writer.Write(Method.Queue.Unbind);
                writer.Write(Reserved);
                writer.Write(Reserved);
                writer.WriteShortString(queue);
                writer.WriteShortString(exchange);
                writer.WriteShortString(routingKey);
                writer.WriteTable(arguments);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_Purge(string queue)
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

                writer.Write(Method.Queue.Purge);
                writer.Write(Reserved);
                writer.Write(Reserved);
                writer.WriteShortString(queue);
                writer.WriteBits();

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_Delete(string queue, bool onlyIfUnused, bool onlyIfEmpty)
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

                writer.Write(Method.Queue.Delete);
                writer.Write(Reserved);
                writer.Write(Reserved);
                writer.WriteShortString(queue);
                writer.WriteBits(onlyIfEmpty, onlyIfUnused);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }
    }
}
