using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using System;
using System.Threading.Tasks;

namespace HouseofCat.Benchmarks.RabbitMQ
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser, ThreadingDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.Net60)]
    public class ChannelPoolBenchmark
    {
        public ConnectionPool ConnectionPool;
        public ChannelPool ChannelPool;

        [GlobalSetup]
        public async Task GlobalSetupAsync()
        {
            await Task.Yield();

            var options = new RabbitOptions();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            options.PoolOptions.MaxConnections = 5;
            options.PoolOptions.MaxChannels = 50;

            ChannelPool = new ChannelPool(options);
            ConnectionPool = new ConnectionPool(options);
        }

        [GlobalCleanup]
        public async Task GlobalCleanupAsync()
        {
            await ChannelPool
                .ShutdownAsync()
                .ConfigureAwait(false);

            await ConnectionPool
                .ShutdownAsync()
                .ConfigureAwait(false);
        }

        [Benchmark(Baseline = true)]
        [Arguments(100)]
        [Arguments(500)]
        public void CreateConnectionsAndChannels(int x)
        {
            for (int i = 0; i < x; i++)
            {
                var connection = ConnectionPool
                    .CreateConnection("Test");

                var channel = connection
                    .CreateModel();

                channel.Close();
                connection.Close();
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(500)]
        public async Task CreateChannelsWithConnectionFromConnectionPoolAsync(int x)
        {
            for (int i = 0; i < x; i++)
            {
                var connHost = await ConnectionPool
                    .GetConnectionAsync()
                    .ConfigureAwait(false);

                var channel = connHost.Connection.CreateModel();
                channel.Close();
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(5_000)]
        [Arguments(500_000)]
        [Arguments(1_000_000)]
        public async Task GetChannelFromChannelPoolAsync(int x)
        {
            for (int i = 0; i < x; i++)
            {
                var channel = await ChannelPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);

                await ChannelPool
                    .ReturnChannelAsync(channel, false)
                    .ConfigureAwait(false);
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(5_000)]
        [Arguments(500_000)]
        [Arguments(1_000_000)]
        public async Task ConcurrentGetChannelFromChannelPoolAsync(int x)
        {
            var t1 = Task.Run(async () =>
            {
                for (int i = 0; i < x; i++)
                {
                    var channel = await ChannelPool
                        .GetChannelAsync()
                        .ConfigureAwait(false);

                    await ChannelPool
                        .ReturnChannelAsync(channel, false)
                        .ConfigureAwait(false);
                }
            });

            var t2 = Task.Run(async () =>
            {
                for (int i = 0; i < x; i++)
                {
                    var channel = await ChannelPool
                        .GetChannelAsync()
                        .ConfigureAwait(false);

                    await ChannelPool
                        .ReturnChannelAsync(channel, false)
                        .ConfigureAwait(false);
                }
            });

            var t3 = Task.Run(async () =>
            {
                for (int i = 0; i < x; i++)
                {
                    var channel = await ChannelPool
                        .GetChannelAsync()
                        .ConfigureAwait(false);

                    await ChannelPool
                        .ReturnChannelAsync(channel, false)
                        .ConfigureAwait(false);
                }
            });

            var t4 = Task.Run(async () =>
            {
                for (int i = 0; i < x; i++)
                {
                    var channel = await ChannelPool
                        .GetChannelAsync()
                        .ConfigureAwait(false);

                    await ChannelPool
                        .ReturnChannelAsync(channel, false)
                        .ConfigureAwait(false);
                }
            });

            await Task
                .WhenAll(new Task[] { t1, t2, t3, t4 })
                .ConfigureAwait(false);
        }
    }
}
