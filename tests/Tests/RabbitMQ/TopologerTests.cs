using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities.File;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.RabbitMQ
{
    public class TopologerTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;

        public TopologerTests(RabbitFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.Output = output;
        }

        [Fact]
        public void CreateTopologer()
        {
            var options = new RabbitOptions();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);

            Assert.NotNull(top);
        }

        [Fact]
        public void CreateTopologerAndInitializeChannelPool()
        {
            var options = new RabbitOptions();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);

            Assert.NotNull(top);
        }

        [Fact]
        public void CreateTopologerWithChannelPool()
        {
            var options = new RabbitOptions();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var chanPool = new ChannelPool(options);
            var top = new Topologer(chanPool);

            Assert.NotNull(top);
        }

        [Fact]
        public async Task CreateQueueAsync()
        {
            var options = new RabbitOptions();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);
            var error = await top.CreateQueueAsync("TestQueueTest", false, false, false, null).ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateAndDeleteQueueAsync()
        {
            var options = new RabbitOptions();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);
            var error = await top.CreateQueueAsync("TestQueueTest", false, false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteQueueAsync("TestQueueTest", false, false).ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateExchangeAsync()
        {
            var options = new RabbitOptions();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);
            var error = await top.CreateExchangeAsync("TestExchangeTest", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateAndDeleteExchangeAsync()
        {
            var options = new RabbitOptions();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);
            var error = await top.CreateExchangeAsync("TestExchangeTest", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteExchangeAsync("TestExchangeTest").ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateAndBindQueueAsync()
        {
            var options = new RabbitOptions();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);
            var error = await top.CreateExchangeAsync("TestExchangeTest", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.CreateQueueAsync("TestQueueTest", false, false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.BindQueueToExchangeAsync("TestQueueTest", "TestExchangeTest", "TestRoutingKeyTest", null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteExchangeAsync("TestExchangeTest").ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteQueueAsync("TestQueueTest").ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateAndBindExchangeAsync()
        {
            var options = new RabbitOptions();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);
            var error = await top.CreateExchangeAsync("TestExchangeTest", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.CreateExchangeAsync("TestExchange2Test", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.BindExchangeToExchangeAsync("TestExchange2Test", "TestExchangeTest", "TestRoutingKeyTest", null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteExchangeAsync("TestExchangeTest").ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteExchangeAsync("TestExchange2Test").ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateBindAndUnbindExchangeAsync()
        {
            var options = new RabbitOptions();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);
            var error = await top.CreateExchangeAsync("TestExchangeTest", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.CreateExchangeAsync("TestExchange2Test", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.BindExchangeToExchangeAsync("TestExchange2Test", "TestExchangeTest", "TestRoutingKeyTest", null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.UnbindExchangeFromExchangeAsync("TestExchange2Test", "TestExchangeTest", "TestRoutingKeyTest", null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteExchangeAsync("TestExchangeTest").ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteExchangeAsync("TestExchange2Test").ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateTopologyFromFileAsync()
        {
            var options = await JsonFileReader.ReadFileAsync<RabbitOptions>("TestConfig.json");
            var top = new Topologer(options);
            await top
                .CreateTopologyFromFileAsync("TestTopologyConfig.json")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateTopologyFromPartialFileAsync()
        {
            var options = await JsonFileReader.ReadFileAsync<RabbitOptions>("TestConfig.json");
            var top = new Topologer(options);
            await top
                .CreateTopologyFromFileAsync("TestPartialTopologyConfig.json")
                .ConfigureAwait(false);
        }
    }
}
