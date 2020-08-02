using HouseofCat.Framing;
using HouseofCat.Sockets;
using HouseofCat.Sockets.Utf8Json;
using System;
using System.Threading.Tasks;

namespace QuickServer
{
    public static class Program
    {
        private static QuickSocketFactory QuickSocketFactory { get; } = new QuickSocketFactory();
        private static Utf8JsonReader<Message> QuickJsonReader { get; set; }
        //private static Utf8JsonWriter<MessageReceipt> QuickJsonWriter { get; set; }

        public static async Task Main()
        {
            await SetupServerAsync()
                .ConfigureAwait(false);

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        private static async Task SetupServerAsync()
        {
            await Console.Out.WriteLineAsync("Starting the server connection now...").ConfigureAwait(false);

            var quickSocket = await QuickSocketFactory
                .GetTcpSocketAsync("127.0.0.1", 15001, true)
                .ConfigureAwait(false);

            var quickListeningSocket = await QuickSocketFactory
                .GetListeningTcpSocketAsync("127.0.0.1", 15001, true)
                .ConfigureAwait(false);

            await Console.Out.WriteLineAsync("Socket now listening...").ConfigureAwait(false);

            var framingStrategy = new TerminatedByteFrameStrategy();

            QuickJsonReader = new Utf8JsonReader<Message>(quickListeningSocket, framingStrategy);
            //QuickJsonWriter = new Utf8JsonWriter<MessageReceipt>(quickSocket, framingStrategy);

            await QuickJsonReader
                .StartReceiveAsync()
                .ConfigureAwait(false);

            _ = Task.Run(async () =>
            {
                await Console.Out.WriteLineAsync("PipeReader waiting to receive data...").ConfigureAwait(false);

                await foreach (var message in QuickJsonReader.MessageChannelReader.ReadAllAsync())
                {
                    await Console
                        .Out
                        .WriteLineAsync($"MessageId: {message.MessageId}\r\nData: {message.Data}\r\n")
                        .ConfigureAwait(false);
                }
            });
        }
    }
}
