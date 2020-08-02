using HouseofCat.Framing;
using HouseofCat.Sockets;
using HouseofCat.Sockets.Utf8Json;
using HouseofCat.Utilities.Random;
using System;
using System.Text;
using System.Threading.Tasks;

namespace QuickSocket.Client
{
    public static class Program
    {
        private static QuickSocketFactory QuickSocketFactory { get; } = new QuickSocketFactory();
        //private static Utf8JsonReader<MessageReceipt> QuickJsonReader { get; set; }
        private static Utf8JsonWriter<Message> QuickJsonWriter { get; set; }
        private static XorShift XorShifter { get; set; }
        private static string RandomPayload { get; set; }

        public static async Task Main()
        {
            XorShifter = new XorShift(true);

            // Create a fixed sized random payload.
            RandomPayload = Encoding.UTF8.GetString(XorShifter.GetRandomBytes(10_000));

            await SetupClientAsync()
                .ConfigureAwait(false);

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        private static async Task SetupClientAsync()
        {
            await Console.Out.WriteLineAsync("Starting the client with delay...").ConfigureAwait(false);

            await Task.Delay(2000).ConfigureAwait(false);

            await Console.Out.WriteLineAsync("Starting the client connection now...").ConfigureAwait(false);

            var quickSocket = await QuickSocketFactory
                .GetTcpSocketAsync("127.0.0.1", 15001, true)
                .ConfigureAwait(false);

            var quickListeningSocket = await QuickSocketFactory
                .GetListeningTcpSocketAsync("127.0.0.1", 15001, true)
                .ConfigureAwait(false);

            var framingStrategy = new TerminatedByteFrameStrategy();

            //QuickJsonReader = new Utf8JsonReader<MessageReceipt>(quickListeningSocket, framingStrategy);
            QuickJsonWriter = new Utf8JsonWriter<Message>(quickSocket, framingStrategy);

            await QuickJsonWriter
                .StartWritingAsync()
                .ConfigureAwait(false);

            // Publish To Server
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        await QuickJsonWriter
                            .QueueForWritingAsync(new Message { MessageId = i, Data = RandomPayload })
                            .ConfigureAwait(false);
                    }

                    await Task.Delay(1000).ConfigureAwait(false);
                }
            });
        }
    }
}
