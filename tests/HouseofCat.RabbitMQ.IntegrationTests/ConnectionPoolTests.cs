using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HouseofCat.RabbitMQ.IntegrationTests
{
    public class ConnectionPoolTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;

        public ConnectionPoolTests(RabbitFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.Output = output;
        }

        [Fact]
        public void CreateConnectionPoolWithLocalHost()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var connPool = new ConnectionPool(options);

            Assert.NotNull(connPool);
        }

        [Fact]
        public void InitializeConnectionPoolAsync()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var connPool = new ConnectionPool(options);

            Assert.NotNull(connPool);
        }

        [Fact]
        public async Task OverLoopThroughConnectionPoolAsync()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            options.PoolOptions.MaxConnections = 5;
            var successCount = 0;
            const int loopCount = 100_000;
            var connPool = new ConnectionPool(options);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < loopCount; i++)
            {
                var connHost = await connPool
                    .GetConnectionAsync()
                    .ConfigureAwait(false);

                if (connHost != null)
                {
                    successCount++;
                }

                await connPool
                    .ReturnConnectionAsync(connHost)
                    .ConfigureAwait(false);
            }

            sw.Stop();
            _fixture.Output.WriteLine($"OverLoop Iteration Time: {sw.ElapsedMilliseconds} ms");

            Assert.True(successCount == loopCount);
        }
    }
}
