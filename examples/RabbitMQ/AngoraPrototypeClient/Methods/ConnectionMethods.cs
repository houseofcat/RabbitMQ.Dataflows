using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Tasks;

using static Angora.PrototypeClient.AmqpConstants;

namespace Angora.PrototypeClient
{
    internal class ConnectionMethods
    {
        private const ushort connectionChannelNumber = 0;

        private static readonly byte[] protocolHeader = { 0x41, 0x4d, 0x51, 0x50, 0x00, 0x00, 0x09, 0x01 };

        private static readonly Dictionary<string, object> capabilities = new Dictionary<string, object>
        {
            { "exchange_exchange_bindings", true }
        };

        private readonly Socket socket;

        internal ConnectionMethods(Socket socket)
        {
            this.socket = socket;
        }

        public async Task Send_Heartbeat()
        {
            var buffer = await socket.GetWriteBuffer().ConfigureAwait(false);

            try
            {
                if (socket.HeartbeatNeeded)
                {
                    WritePayload();
                }

                await buffer.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                socket.ReleaseWriteBuffer(true);
            }

            void WritePayload()
            {
                var writer = new CustomBufferWriter<PipeWriter>(buffer);

                writer.Write(FrameType.Heartbeat);
                writer.Write(connectionChannelNumber);
                writer.Write((uint)0);
                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_ProtocolHeader()
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

                writer.Write(protocolHeader);

                writer.Commit();
            }
        }

        public async Task Send_StartOk(string connectionName, string userName, string password, string mechanism = "PLAIN", string locale = "en_US")
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

                var payloadSizeHeader = writer.WriteFrameHeader(FrameType.Method, connectionChannelNumber);

                writer.Write(Method.Connection.StartOk);

                var clientProperties = new Dictionary<string, object>
                {
                    { "product", "Angora.PrototypeClient" },
                    { "capabilities", capabilities },
                    { "connection_name", connectionName }
                };

                writer.WriteTable(clientProperties);
                writer.WriteShortString(mechanism);
                writer.WriteLongString($"\0{userName}\0{password}"); //response
                writer.WriteShortString(locale);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_TuneOk(ushort channelMax, uint frameMax, ushort heartbeat)
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

                var payloadSizeHeader = writer.WriteFrameHeader(FrameType.Method, connectionChannelNumber);

                writer.Write(Method.Connection.TuneOk);
                writer.Write(channelMax);
                writer.Write(frameMax);
                writer.Write(heartbeat);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_Open(string virtualHost)
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

                var payloadSizeHeader = writer.WriteFrameHeader(FrameType.Method, connectionChannelNumber);

                writer.Write(Method.Connection.Open);
                writer.WriteShortString(virtualHost);
                writer.Write(Reserved);
                writer.Write(Reserved);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_Close(ushort replyCode = ConnectionReplyCode.Success, string replyText = "Goodbye", ushort failingClass = 0, ushort failingMethod = 0)
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

                var payloadSizeHeader = writer.WriteFrameHeader(FrameType.Method, connectionChannelNumber);

                writer.Write(Method.Connection.Close);
                writer.Write(replyCode);
                writer.WriteShortString(replyText);
                writer.Write(failingClass);
                writer.Write(failingMethod);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }

        public async Task Send_CloseOk()
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

                var payloadSizeHeader = writer.WriteFrameHeader(FrameType.Method, connectionChannelNumber);

                writer.Write(Method.Connection.CloseOk);

                writer.Commit();
                BinaryPrimitives.WriteUInt32BigEndian(payloadSizeHeader, (uint)writer.BytesCommitted - FrameHeaderSize);

                writer.Write(FrameEnd);

                writer.Commit();
            }
        }
    }
}
