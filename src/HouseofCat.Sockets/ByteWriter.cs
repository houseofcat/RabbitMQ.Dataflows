using HouseofCat.Framing;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HouseofCat.Sockets
{
    public class ByteWriter : IQuickWriter<byte[]>
    {
        public IQuickSocket QuickSocket { get; }
        public bool Write { get; private set; }

        private Task WriteLoopTask { get; set; }
        private SemaphoreSlim PipeLock { get; } = new SemaphoreSlim(1, 1);
        private Channel<byte[]> MessageChannel { get; }
        public ChannelReader<byte[]> MessageChannelReader { get; }
        public ChannelWriter<byte[]> MessageChannelWriter { get; }

        private Framing.IFramingStrategy FramingStrategy { get; }

        public ByteWriter(
            IQuickSocket quickSocket,
            IFramingStrategy framingStrategy)
        {
            QuickSocket = quickSocket;
            FramingStrategy = framingStrategy;
            MessageChannel = Channel.CreateUnbounded<byte[]>();
            MessageChannelWriter = MessageChannel.Writer;
            MessageChannelReader = MessageChannel.Reader;
        }

        public async Task QueueForWritingAsync(byte[] array)
        {
            if (await MessageChannelWriter.WaitToWriteAsync().ConfigureAwait(false))
            {
                await MessageChannelWriter
                    .WriteAsync(array)
                    .ConfigureAwait(false);
            }
        }

        public async Task StartWritingAsync()
        {
            await PipeLock.WaitAsync().ConfigureAwait(false);

            if (!Write)
            { Write = true; }

            await QuickSocket
                .ConnectToPrimaryAddressAsync()
                .ConfigureAwait(false);

            WriteLoopTask = Task.Run(WriteAsync);

            PipeLock.Release();
        }

        private async Task WriteAsync()
        {
            using var netStream = new NetworkStream(QuickSocket.Socket);

            while (Write)
            {
                if (await MessageChannelReader.WaitToReadAsync().ConfigureAwait(false))
                {
                    var bytesArray = await MessageChannelReader
                        .ReadAsync()
                        .ConfigureAwait(false);

                    try
                    {
                        await FramingStrategy
                            .CreateFrameAndSendAsync(bytesArray, netStream)
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // Stop Writing loop.
                        await StopWriteAsync().ConfigureAwait(false);

                        // Return message to channel.
                        await MessageChannelWriter
                            .WriteAsync(bytesArray)
                            .ConfigureAwait(false);
                    }
                }
            }

            await StopWriteAsync().ConfigureAwait(false);
        }

        public async Task StopWriteAsync()
        {
            await PipeLock.WaitAsync().ConfigureAwait(false);

            if (Write)
            { Write = false; }

            PipeLock.Release();
        }
    }
}
