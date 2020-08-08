using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Channels.Example
{
    public static class Program
    {
        public static long GlobalCount = 1_000_000;
        public static Channel<long> GlobalChannel = Channel.CreateBounded<long>(1000);

        public static async Task Main(string[] args)
        {
            await Console.Out.WriteLineAsync("Channel Example says Hello!");

            var writer = Task.Run(WriteDataAsync);
            var reader = Task.Run(ReadDataAsync);

            await writer;
            await reader;

            await Console.Out.WriteLineAsync("We are finished!");

            //GC.AddMemoryPressure(GlobalCount * 64);
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            Console.ReadKey();
        }

        public static async Task WriteDataAsync()
        {
            //await Task.Yield(); // forces the calling thread to immediately 
            var counter = 0L;
            while(await GlobalChannel.Writer.WaitToWriteAsync())
            {
                await GlobalChannel.Writer.WriteAsync(counter++);

                if (counter == GlobalCount)
                {
                    GlobalChannel.Writer.Complete();
                    break;
                }
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
