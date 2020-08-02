using HouseofCat.Framing;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HouseofCat.Sockets
{
    public class ByteReader : IQuickReader<byte[]>
    {
        public IQuickListeningSocket QuickListeningSocket { get; }
        public bool Receive { get; private set; }

        private Task ReceiveLoopTask { get; set; }
        private SemaphoreSlim PipeLock { get; } = new SemaphoreSlim(1, 1);
        private Channel<byte[]> MessageChannel { get; }
        public ChannelReader<byte[]> MessageChannelReader { get; }

        private IFramingStrategy FramingStrategy { get; }

        public ByteReader(
            IQuickListeningSocket quickListeningSocket,
            IFramingStrategy framingStrategy)
        {
            QuickListeningSocket = quickListeningSocket;
            FramingStrategy = framingStrategy;

            MessageChannel = Channel.CreateUnbounded<byte[]>();
            MessageChannelReader = MessageChannel.Reader;
        }

        public async Task StartReceiveAsync()
        {
            await PipeLock.WaitAsync().ConfigureAwait(false);

            if (!Receive)
            {
                Receive = true;

                await QuickListeningSocket
                    .BindSocketToAddressAsync(100)
                    .ConfigureAwait(false);

                ReceiveLoopTask = Task.Run(ReceiveAsync);
            }

            PipeLock.Release();
        }

        private async Task ReceiveAsync()
        {
            while (Receive)
            {
                var localSocket = await QuickListeningSocket.Socket.AcceptAsync().ConfigureAwait(false);
                _ = Task.Run(() => ReceiveDataAsync(localSocket));

                async Task ReceiveDataAsync(Socket localSocket)
                {
                    using var stream = new NetworkStream(localSocket);
                    var pipeReader = PipeReader.Create(stream);

                    while (true)
                    {
                        ReadResult result = await pipeReader.ReadAsync().ConfigureAwait(false);
                        ReadOnlySequence<byte> buffer = result.Buffer;

                        if (result.IsCanceled)
                        { break; }

                        // Trying to find all full sequences in the current buffer.
                        while (FramingStrategy.TryReadSequence(ref buffer, out ReadOnlySequence<byte> sequence))
                        {
                            await SendSequenceToChannelAsync(sequence).ConfigureAwait(false);
                        }

                        // Buffer position was modified in TryReadSequence to include exact amounts consumed and read (if any).
                        pipeReader.AdvanceTo(buffer.Start, buffer.End);

                        if (result.IsCompleted)
                        { break; }
                    }
                }
            }
        }

        private async Task SendSequenceToChannelAsync(ReadOnlySequence<byte> sequence)
        {
            await MessageChannel
                .Writer
                .WriteAsync(sequence.ToArray())
                .ConfigureAwait(false);
        }

        public async Task StopReceiveAsync()
        {
            await PipeLock.WaitAsync().ConfigureAwait(false);

            if (Receive)
            { Receive = false; }

            PipeLock.Release();
        }
    }
}
