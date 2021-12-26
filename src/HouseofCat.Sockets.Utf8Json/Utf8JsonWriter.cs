using HouseofCat.Framing;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Utf8Json;

namespace HouseofCat.Sockets.Utf8Json
{
    public class Utf8JsonWriter<TSend> : IQuickWriter<TSend>
    {
        public IQuickSocket QuickSocket { get; }
        public bool Write { get; private set; }

        private Task WriteLoopTask { get; set; }
        private SemaphoreSlim PipeLock { get; } = new SemaphoreSlim(1, 1);
        private Channel<TSend> MessageChannel { get; }
        public ChannelReader<TSend> MessageChannelReader { get; }
        public ChannelWriter<TSend> MessageChannelWriter { get; }

        private IFramingStrategy FramingStrategy { get; }

        public Utf8JsonWriter(
            IQuickSocket quickSocket,
            IFramingStrategy framingStrategy)
        {
            QuickSocket = quickSocket;
            FramingStrategy = framingStrategy;
            MessageChannel = Channel.CreateUnbounded<TSend>();
            MessageChannelWriter = MessageChannel.Writer;
            MessageChannelReader = MessageChannel.Reader;
        }

        public async Task QueueForWritingAsync(TSend obj)
        {
            if (await MessageChannelWriter.WaitToWriteAsync().ConfigureAwait(false))
            {
                await MessageChannelWriter
                    .WriteAsync(obj)
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
                    var itemToSend = await MessageChannelReader
                        .ReadAsync()
                        .ConfigureAwait(false);

                    var bytes = JsonSerializer.Serialize(itemToSend);

                    try
                    {
                        await FramingStrategy
                            .CreateFrameAndSendAsync(bytes, netStream)
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // Stop Writing loop.
                        await StopWriteAsync().ConfigureAwait(false);

                        // Return message to channel.
                        await MessageChannelWriter
                            .WriteAsync(itemToSend)
                            .ConfigureAwait(false);
                    }
                }
            }
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
