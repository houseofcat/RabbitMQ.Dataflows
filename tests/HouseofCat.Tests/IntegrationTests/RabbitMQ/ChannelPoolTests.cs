using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HouseofCat.Tests.IntegrationTests.RabbitMQ
{
    public class ChannelPoolTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;

        public ChannelPoolTests(RabbitFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void CreateChannelPoolWithLocalHost()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var chanPool = new ChannelPool(options);

            Assert.NotNull(chanPool);
        }

        [Fact]
        public void InitializeChannelPoolAsync()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var chanPool = new ChannelPool(options);

            Assert.NotNull(chanPool);
            Assert.True(chanPool.CurrentChannelId > 0);
        }

        [Fact]
        public async Task OverLoopThroughChannelPoolAsync()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            options.PoolOptions.MaxConnections = 5;
            options.PoolOptions.MaxChannels = 25;
            var successCount = 0;
            const int loopCount = 100_000;
            var chanPool = new ChannelPool(options);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < loopCount; i++)
            {
                var channel = await chanPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);

                if (channel != null)
                {
                    successCount++;
                    await chanPool
                        .ReturnChannelAsync(channel, false)
                        .ConfigureAwait(false);
                }
            }

            for (int i = 0; i < loopCount; i++)
            {
                var channel = await chanPool
                    .GetAckChannelAsync()
                    .ConfigureAwait(false);

                if (channel != null)
                {
                    successCount++;
                    await chanPool
                        .ReturnChannelAsync(channel, false)
                        .ConfigureAwait(false);
                }
            }

            sw.Stop();
            _fixture.Output.WriteLine($"OverLoop Iteration Time: {sw.ElapsedMilliseconds} ms");

            Assert.True(successCount == 2 * loopCount);
        }
    }
}
