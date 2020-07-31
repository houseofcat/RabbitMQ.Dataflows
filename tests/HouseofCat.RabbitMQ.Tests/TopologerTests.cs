using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities.File;
using System;
using System.Threading.Tasks;
using Xunit;

namespace HouseofCat.RabbitMQ.Tests
{
    public class TopologerTests
    {
        [Fact]
        public void CreateTopologer()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);

            Assert.NotNull(top);
        }

        [Fact]
        public void CreateTopologerAndInitializeChannelPool()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);

            Assert.NotNull(top);
        }

        [Fact]
        public void CreateTopologerWithChannelPool()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var chanPool = new ChannelPool(options);
            var top = new Topologer(chanPool);

            Assert.NotNull(top);
        }

        [Fact]
        public async Task CreateQueueWithoutInitializeAsync()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);

            await Assert
                .ThrowsAsync<InvalidOperationException>(() => top.CreateQueueAsync("TestQueue", false, false, false, null))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateQueueAsync()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);
            var error = await top.CreateQueueAsync("TestQueueTest", false, false, false, null).ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateAndDeleteQueueAsync()
        {
            var options = new Options();
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
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(options);
            var error = await top.CreateExchangeAsync("TestExchangeTest", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateAndDeleteExchangeAsync()
        {
            var options = new Options();
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
            var options = new Options();
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
            var options = new Options();
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
            var options = new Options();
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
            var options = await JsonFileReader.ReadFileAsync<Options>("TestConfig.json");
            var top = new Topologer(options);
            await top
                .CreateTopologyFromFileAsync("TestTopologyConfig.json")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateTopologyFromPartialFileAsync()
        {
            var options = await JsonFileReader.ReadFileAsync<Options>("TestConfig.json");
            var top = new Topologer(options);
            await top
                .CreateTopologyFromFileAsync("TestPartialTopologyConfig.json")
                .ConfigureAwait(false);
        }
    }
}
