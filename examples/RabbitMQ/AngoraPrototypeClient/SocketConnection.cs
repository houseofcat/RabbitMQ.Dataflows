using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Angora.PrototypeClient
{
    internal class SocketConnection
    {
        private System.Net.Sockets.Socket socket;

        private readonly Pipe pipe1;
        private readonly Pipe pipe2;

        private readonly PipeWriter writer;
        private readonly PipeReader reader;

        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        public SocketConnection()
        {
            pipe1 = new Pipe();
            pipe2 = new Pipe();

            Input = pipe1.Reader;
            Output = pipe2.Writer;

            writer = pipe1.Writer;
            reader = pipe2.Reader;
        }

        public async Task ConnectAsync(IPEndPoint endpoint)
        {
            socket = new System.Net.Sockets.Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endpoint).ConfigureAwait(false);

            ReceiveLoop().Ignore();
            SendLoop().Ignore();
        }

        private async Task ReceiveLoop()
        {
            Exception exception = null;

            try
            {
                while (true)
                {
                    var buffer = writer.GetMemory();
                    var numberOfBytes = await socket.ReceiveAsync(buffer, SocketFlags.None).ConfigureAwait(false);

                    if (numberOfBytes == 0)
                    {
                        break;
                    }

                    writer.Advance(numberOfBytes);

                    var result = await writer.FlushAsync().ConfigureAwait(false);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            writer.Complete(exception);
        }

        private async Task SendLoop()
        {
            Exception exception = null;

            try
            {
                while (true)
                {
                    var result = await reader.ReadAsync().ConfigureAwait(false);

                    if (result.IsCanceled)
                    {
                        break;
                    }

                    var buffer = result.Buffer;

                    await SendAsync(buffer).ConfigureAwait(false);

                    reader.AdvanceTo(buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                exception = ex;
            }

            reader.Complete(exception);
        }

        private async Task SendAsync(ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return;
            }

            if (buffer.IsSingleSegment)
            {
                await socket.SendAsync(MemoryMarshal.AsMemory(buffer.First), SocketFlags.None).ConfigureAwait(false);
            }
            else
            {
                var list = new List<ArraySegment<byte>>();

                foreach (var memory in buffer)
                {
                    if (MemoryMarshal.TryGetArray(memory, out var segment))
                    {
                        list.Add(segment);
                    }
                    else
                    {
                        throw new Exception("BOOM!");
                    }
                }

                await socket.SendAsync(list, SocketFlags.None).ConfigureAwait(false);
            }
        }
    }
}
