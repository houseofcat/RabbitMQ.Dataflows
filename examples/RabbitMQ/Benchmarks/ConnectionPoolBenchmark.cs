using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using System;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser, ThreadingDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.NetCoreApp31)]
    public class ConnectionPoolBenchmark
    {
        public ConnectionPool ConnectionPool;

        [GlobalSetup]
        public async Task GlobalSetupAsync()
        {
            await Task.Yield();

            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            options.PoolOptions.MaxConnections = 5;

            ConnectionPool = new ConnectionPool(options);
        }

        [GlobalCleanup]
        public async Task GlobalCleanupAsync()
        {
            await ConnectionPool
                .ShutdownAsync()
                .ConfigureAwait(false);
        }

        [Benchmark(Baseline = true)]
        [Arguments(100)]
        [Arguments(500)]
        public void CreateConnections(int x)
        {
            for (int i = 0; i < x; i++)
            {
                var connection = ConnectionPool
                    .CreateConnection("Test");

                connection.Close();
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(5_000)]
        [Arguments(500_000)]
        [Arguments(1_000_000)]
        public async Task GetConnectionFromConnectionPoolAsync(int x)
        {
            for (int i = 0; i < x; i++)
            {
                var connection = await ConnectionPool
                    .GetConnectionAsync()
                    .ConfigureAwait(false);
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(5_000)]
        [Arguments(500_000)]
        [Arguments(1_000_000)]
        public async Task ConcurrentGetConnectionFromConnectionPoolAsync(int x)
        {
            var t1 = Task.Run(async () =>
            {
                for (int i = 0; i < x / 4; i++)
                {
                    var connection = await ConnectionPool
                        .GetConnectionAsync()
                        .ConfigureAwait(false);
                }
            });

            var t2 = Task.Run(async () =>
            {
                for (int i = 0; i < x / 4; i++)
                {
                    var connection = await ConnectionPool
                        .GetConnectionAsync()
                        .ConfigureAwait(false);
                }
            });

            var t3 = Task.Run(async () =>
            {
                for (int i = 0; i < x / 4; i++)
                {
                    var connection = await ConnectionPool
                        .GetConnectionAsync()
                        .ConfigureAwait(false);
                }
            });

            var t4 = Task.Run(async () =>
            {
                for (int i = 0; i < x / 4; i++)
                {
                    var connection = await ConnectionPool
                        .GetConnectionAsync()
                        .ConfigureAwait(false);
                }
            });

            await Task
                .WhenAll(new Task[] { t1, t2, t3, t4 })
                .ConfigureAwait(false);
        }
    }
}
