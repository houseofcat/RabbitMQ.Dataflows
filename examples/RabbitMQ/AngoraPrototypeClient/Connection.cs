using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using static RabbitMQ.Core.Prototype.AmqpConstants;

namespace RabbitMQ.Core.Prototype
{
    public class Connection
    {
        private readonly ConnectionMethods methods;

        private readonly string hostName;
        private readonly string userName;
        private readonly string password;
        private readonly string virtualHost;
        private readonly string connectionName;

        private readonly Socket socket;

        private readonly Dictionary<ushort, Channel> channels;

        private readonly TaskCompletionSource<bool> openOk;
        private readonly TaskCompletionSource<bool> closeOk;
        private readonly TaskCompletionSource<(ushort channelMax, uint frameMax, uint heartbeatInterval)> readyToOpen;

        private readonly CancellationTokenSource sendHeartbeats;
        private readonly CancellationTokenSource readLoop;

        private Task sendHeartbeatsTask;
        private Task readLoopTask;

        private ushort nextChannelNumber;

        public bool IsOpen { get; private set; }

        public ushort ChannelMax { get; private set; }

        public uint FrameMax { get; private set; }

        public uint HeartbeatInterval { get; private set; }

        internal Connection(string hostName, string userName, string password, string virtualHost, string connectionName)
        {
            this.hostName = hostName;
            this.userName = userName;
            this.password = password;
            this.virtualHost = virtualHost;
            this.connectionName = connectionName;

            socket = new Socket();

            methods = new ConnectionMethods(socket);

            channels = new Dictionary<ushort, Channel>();

            openOk = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            closeOk = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            readyToOpen = new TaskCompletionSource<(ushort channelMax, uint frameMax, uint heartbeatInterval)>(TaskCreationOptions.RunContinuationsAsynchronously);

            sendHeartbeats = new CancellationTokenSource();
            readLoop = new CancellationTokenSource();
        }

        internal async Task Connect()
        {
            var addresses = await Dns.GetHostAddressesAsync(hostName).ConfigureAwait(false);
            var address = addresses[0];
            var endpoint = new IPEndPoint(address, 5672);

            await socket.Connect(endpoint).ConfigureAwait(false);

            readLoopTask = Task.Run(() => ReadLoop(readLoop.Token));

            await methods.Send_ProtocolHeader().ConfigureAwait(false);
            (ChannelMax, FrameMax, HeartbeatInterval) = await readyToOpen.Task.ConfigureAwait(false);

            await methods.Send_Open(virtualHost).ConfigureAwait(false);
            IsOpen = await openOk.Task.ConfigureAwait(false);
        }

        public async Task<Channel> CreateChannel()
        {
            var channel = new Channel(socket, FrameMax - TotalFrameOverhead, ++nextChannelNumber);
            channels.Add(channel.ChannelNumber, channel);

            await channel.Open().ConfigureAwait(false);

            return channel;
        }

        public Task Close()
        {
            return Close(true);
        }

        private async Task Close(bool clientInitiated)
        {
            if (!IsOpen)
            {
                throw new Exception("already closed");
            }

            IsOpen = false;
            sendHeartbeats.Cancel();
            await sendHeartbeatsTask.ConfigureAwait(false);

            if (clientInitiated)
            {
                await methods.Send_Close().ConfigureAwait(false);
                await closeOk.Task.ConfigureAwait(false);
            }
            else
            {
                await methods.Send_CloseOk().ConfigureAwait(false);
            }

            readLoop.Cancel();
            socket.Input.CancelPendingRead();
            await readLoopTask.ConfigureAwait(false);
            socket.Close();
        }

        private async Task ReadLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var readResult = await socket.Input.ReadAsync().ConfigureAwait(false);
                var buffer = readResult.Buffer;

                if (buffer.IsEmpty && readResult.IsCompleted)
                {
                    break;
                }

                var examined = buffer.End;

                var consumed = Read(buffer);

                socket.Input.AdvanceTo(consumed, examined);
            }
        }

        private SequencePosition Read(ReadOnlySequence<byte> buffer)
        {
            var reader = new CustomBufferReader(buffer);

            while (!reader.End)
            {
                if (buffer.Length < FrameHeaderSize)
                {
                    break;
                }

                var preFrameHeaderPositon = reader.Position;

                var frameType = reader.ReadByte();
                var channelNumber = reader.ReadUInt16();
                var payloadSize = reader.ReadUInt32();

                buffer = buffer.Slice(reader.Position);

                if (buffer.Length < payloadSize + 1)
                {
                    return preFrameHeaderPositon;
                }

                var payload = buffer.Slice(reader.Position, payloadSize);
                reader.Advance((int)payloadSize); //cast ok?

                var frameEnd = reader.ReadByte();

                if (frameEnd != FrameEnd)
                {
                    //TODO other stuff here around what this means
                    throw new Exception();
                }

                switch (frameType)
                {
                    case FrameType.Method:
                        HandleIncomingMethodFrame(channelNumber, payload).GetAwaiter().GetResult(); // TODO fix this!
                        break;
                    case FrameType.ContentHeader:
                    case FrameType.ContentBody:
                        HandleIncomingContent(channelNumber, frameType, payload).GetAwaiter().GetResult(); // TODO fix this!
                        break;
                }

                buffer = buffer.Slice(reader.Position);
            }

            return reader.Position;
        }

        private async Task SendHeartbeats(ushort interval, CancellationToken token)
        {
            try
            {
                await Task.Delay(200, token).ConfigureAwait(false);

                while (!token.IsCancellationRequested)
                {
                    await methods.Send_Heartbeat().ConfigureAwait(false);

                    await Task.Delay(TimeSpan.FromSeconds(interval), token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task HandleIncomingMethodFrame(ushort channelNumber, ReadOnlySequence<byte> payload)
        {
            uint Read()
            {
                var reader = new CustomBufferReader(payload);

                var method_internal = reader.ReadUInt32();
                payload = payload.Slice(reader.Position);

                return method_internal;
            }

            var method = Read();
            var classId = method >> 16;

            if (classId == ClassId.Connection) //TODO validate channel 0
            {
                await HandleIncomingMethod(method, payload).ConfigureAwait(false);
            }
            else
            {
                await channels[channelNumber].HandleIncomingMethod(method, payload).ConfigureAwait(false);
            }
        }

        private Task HandleIncomingContent(ushort channelNumber, byte frameType, ReadOnlySequence<byte> payload)
        {
            return channels[channelNumber].HandleIncomingContent(frameType, payload);
        }

        private async Task HandleIncomingMethod(uint method, ReadOnlySequence<byte> arguments)
        {
            switch (method)
            {
                case Method.Connection.Start:
                    await Handle_Start(arguments).ConfigureAwait(false);
                    break;

                case Method.Connection.Tune:
                    await Handle_Tune(arguments).ConfigureAwait(false);
                    break;

                case Method.Connection.Close:
                    await Handle_Close(arguments).ConfigureAwait(false);
                    break;

                case Method.Connection.OpenOk:
                    openOk.SetResult(true);
                    break;

                case Method.Connection.CloseOk:
                    closeOk.SetResult(true);
                    break;
            }
        }

        private Task Handle_Start(ReadOnlySequence<byte> buffer)
        {
            var reader = new CustomBufferReader(buffer);

            var versionMajor = reader.ReadByte();
            var versionMinor = reader.ReadByte();
            var serverProperties = reader.ReadTable();
            var mechanism = reader.ReadLongString();
            var locales = reader.ReadLongString();

            return methods.Send_StartOk(connectionName, userName, password);
        }

        private async Task Handle_Tune(ReadOnlySequence<byte> buffer)
        {
            var arguments = ReadArguments();

            sendHeartbeatsTask = Task.Run(() => SendHeartbeats(arguments.heartbeat, sendHeartbeats.Token));

            await methods.Send_TuneOk(arguments.channelMax, arguments.frameMax, arguments.heartbeat).ConfigureAwait(false);

            readyToOpen.SetResult((arguments.channelMax, arguments.frameMax, arguments.heartbeat));

            (ushort channelMax, uint frameMax, ushort heartbeat) ReadArguments()
            {
                var reader = new CustomBufferReader(buffer);

                var channelMax = reader.ReadUInt16();
                var frameMax = reader.ReadUInt32();
                var heartbeat = reader.ReadUInt16();

                return (channelMax, frameMax, heartbeat);
            }
        }

        private async Task Handle_Close(ReadOnlySequence<byte> buffer)
        {
            var arguments = ReadArguments();

            await Close(false).ConfigureAwait(false);

            foreach (var channel in channels)
            {
                channel.Value.Handle_Connection_Close(arguments.replyCode, arguments.replyText, arguments.method);
            }

            (ushort replyCode, string replyText, uint method) ReadArguments()
            {
                var reader = new CustomBufferReader(buffer);

                var replyCode = reader.ReadUInt16();
                var replyText = reader.ReadShortString();
                var method = reader.ReadUInt32();

                return (replyCode, replyText, method);
            }
        }
    }
}

