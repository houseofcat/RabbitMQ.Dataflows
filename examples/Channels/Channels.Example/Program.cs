using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChannelExample
{
    public static class Program
    {
        public static Channel<int> GlobalChannel = Channel.CreateUnbounded<int>();

        public static async Task Main(string[] args)
        {
            await Console.Out.WriteLineAsync("Channel Example says Hello!");

            var writer = Task.Run(WriteDataAsync);
            var reader = Task.Run(ReadDataAsync);

            await writer;
            await reader;

            await Console.Out.WriteLineAsync("We are finished!");
        }

        public static async Task WriteDataAsync()
        {
            //await Task.Yield(); // forces the calling thread to immediately 
            var counter = 0;
            while(await GlobalChannel.Writer.WaitToWriteAsync())
            {
                await GlobalChannel.Writer.WriteAsync(counter++);

                if (counter == 100)
                { GlobalChannel.Writer.Complete(); }
            }
        }

        public static async Task ReadDataAsync()
        {
            // process each and every element in the stream until stream is closed.
            await foreach(var value in GlobalChannel.Reader.ReadAllAsync())
            {
                await Console.Out.WriteLineAsync(
                    $"{DateTime.Now:MM/dd/yyyy hh:mm:ss.fff} - {value}");
            }
        }
    }
}
