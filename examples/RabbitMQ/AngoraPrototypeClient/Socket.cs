using System;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Angora.PrototypeClient
{
    internal class Socket
    {
        public bool HeartbeatNeeded { get; private set; } = true;

        private readonly SocketConnection connection = new SocketConnection();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private bool isOpen;

        public PipeReader Input => connection.Input;

        public async Task Connect(IPEndPoint endpoint)
        {
            await connection.ConnectAsync(endpoint).ConfigureAwait(false);
            isOpen = true;
        }

        public async Task<PipeWriter> GetWriteBuffer()
        {
            if (!isOpen)
            {
                throw new Exception("socket is closed for writing");
            }

            await semaphore.WaitAsync().ConfigureAwait(false);

            return connection.Output;
        }

        public void ReleaseWriteBuffer(bool wroteHeartbeat = false)
        {
            HeartbeatNeeded = wroteHeartbeat;
            semaphore.Release();
        }

        public void Close()
        {
            isOpen = false;

            connection.Output.Complete();
        }
    }
}
