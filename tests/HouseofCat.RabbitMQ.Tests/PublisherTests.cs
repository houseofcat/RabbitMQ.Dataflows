using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CookedRabbit.Core.Tests
{
    public class PublisherTests
    {
        private readonly ITestOutputHelper output;

        public PublisherTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void CreatePublisher()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var pub = new Publisher(options, new byte[] { });

            Assert.NotNull(pub);
        }

        [Fact]
        public async Task CreatePublisherAndInitializeChannelPool()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var pub = new Publisher(options, new byte[] { });
            await pub.StartAutoPublishAsync().ConfigureAwait(false);

            Assert.NotNull(pub);
        }

        [Fact]
        public void CreatePublisherWithChannelPool()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var chanPool = new ChannelPool(options);
            var pub = new Publisher(chanPool, new byte[] { });

            Assert.NotNull(pub);
        }

        [Fact]
        public async Task PublishWithoutInitializeToQueueAsync()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var pub = new Publisher(options, new byte[] { });
            var letter = RandomData.CreateSimpleRandomLetter("TestQueue", 2000);

            await Assert
                .ThrowsAsync<InvalidOperationException>(() => pub.PublishAsync(letter, false))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PublishAsync()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var pub = new Publisher(options, new byte[] { });
            await pub
                .StartAutoPublishAsync()
                .ConfigureAwait(false);

            var letter = RandomData.CreateSimpleRandomLetter("TestQueue", 2000);
            await pub
                .PublishAsync(letter, false)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task PublishManyAsync()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            const int letterCount = 10_000;
            const int byteCount = 500;

            var pub = new Publisher(options, new byte[] { });
            await pub
                .StartAutoPublishAsync()
                .ConfigureAwait(false);

            var queueNames = new List<string>
            {
                "TestQueue0",
                "TestQueue1",
                "TestQueue2",
                "TestQueue3",
                "TestQueue4",
                "TestQueue5",
                "TestQueue6",
                "TestQueue7",
                "TestQueue8",
                "TestQueue9",
            };
            var letters = RandomData.CreateManySimpleRandomLetters(queueNames, letterCount, byteCount);

            var sw = Stopwatch.StartNew();
            await pub
                .PublishManyAsync(letters, false)
                .ConfigureAwait(false);
            sw.Stop();

            const double kiloByteCount = byteCount / 1000.0;
            output.WriteLine($"Published {letterCount} letters, {kiloByteCount} KB each, in {sw.ElapsedMilliseconds} ms.");

            var rate = letterCount / (sw.ElapsedMilliseconds / 1000.0);
            var dataRate = rate * kiloByteCount;
            output.WriteLine($"Message Rate: {rate.ToString("0.###")} letters / sec, or {(dataRate / 1000.0).ToString("0.###")} MB/s");
        }

        [Fact]
        public void PublishBatchAsync()
        {
            //var options = new Options();
            //config.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            //const int letterCount = 10_000;
            //const int byteCount = 500;

            //var pub = new Publisher(config);
            //await pub
            //    .ChannelPool
            //    .InitializeAsync()
            //    .ConfigureAwait(false);

            //var queueNames = new List<string>
            //{
            //    "TestQueue0",
            //};

            //var letters = RandomData.CreateManySimpleRandomLetters(queueNames, letterCount, byteCount);

            //var sw = Stopwatch.StartNew();
            //await pub
            //    .PublishBatchAsync("", "TestQueue0", letters.Select(x => x.Body), false)
            //    .ConfigureAwait(false);
            //sw.Stop();

            //const double kiloByteCount = byteCount / 1000.0;
            //output.WriteLine($"Published {letterCount} letters, {kiloByteCount} KB each, in {sw.ElapsedMilliseconds} ms.");

            //var rate = letterCount / (sw.ElapsedMilliseconds / 1000.0);
            //var dataRate = rate * kiloByteCount;
            //output.WriteLine($"Message Rate: {rate.ToString("0.###")} letters / sec, or {(dataRate / 1000.0).ToString("0.###")} MB/s");
        }
    }
}
