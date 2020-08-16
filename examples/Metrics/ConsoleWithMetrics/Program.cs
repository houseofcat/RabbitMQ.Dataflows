using HouseofCat.Metrics;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ConsoleWithMetrics
{
    public static class Program
    {
        public static long GlobalCount = 1_000_000;
        public static Channel<long> GlobalChannel = Channel.CreateBounded<long>(1000);
        public static IMetricsProvider _metricsProvider = new PrometheusMetricsProvider("localhost", 5000, "/metrics", null, false);

        public static async Task Main(string[] args)
        {
            await Console.Out.WriteLineAsync("Channel Example says Hello!").ConfigureAwait(false);

            var writer = Task.Run(WriteDataAsync);
            var reader = Task.Run(ReadDataAsync);

            await writer.ConfigureAwait(false);
            await reader.ConfigureAwait(false);

            await Console.Out.WriteLineAsync("We are finished!").ConfigureAwait(false);

            Console.ReadKey();
        }

        public static async Task WriteDataAsync()
        {
            await Task.Yield();
            var counter = 0L;
            while(await GlobalChannel.Writer.WaitToWriteAsync().ConfigureAwait(false))
            {
                await GlobalChannel.Writer.WriteAsync(counter++);

                if (counter == GlobalCount)
                {
                    GlobalChannel.Writer.Complete();
                    break;
                }

                await Task.Delay(50);
            }
        }

        public static async Task ReadDataAsync()
        {
            await foreach(var value in GlobalChannel.Reader.ReadAllAsync())
            {
                using var md = _metricsProvider.TrackAndDuration("ConsoleWithMetrics_ConsoleWrite");

                await Console.Out.WriteLineAsync(
                    $"{DateTime.Now:MM/dd/yyyy hh:mm:ss.fff} - {value}").ConfigureAwait(false);
            }
        }
    }
}
