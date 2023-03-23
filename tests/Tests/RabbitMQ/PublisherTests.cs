using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RabbitMQ
{
    public class PublisherTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;

        public PublisherTests(RabbitFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.Output = output;
        }

        [Fact]
        public async Task CreatePublisher()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var pub = new Publisher(
                await _fixture.GetChannelPoolAsync(),
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            Assert.NotNull(pub);
        }

        [Fact]
        public async Task CreatePublisherAndInitializeChannelPool()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var pub = new Publisher(
                await _fixture.GetChannelPoolAsync(),
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            await pub.StartAutoPublishAsync().ConfigureAwait(false);

            Assert.NotNull(pub);
        }

        [Fact]
        public async Task CreatePublisherWithChannelPool()
        {
            var options = new RabbitOptions();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            if (!await _fixture.CheckRabbitHostConnectionAndUpdateFactoryOptions(options))
            {
                return;
            }

            var pub = new Publisher(
                new ChannelPool(options),
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            Assert.NotNull(pub);
        }

        [Fact]
        public async Task PublishToNonExistentQueueAsync()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var pub = new Publisher(
                await _fixture.GetChannelPoolAsync(),
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            var letter = MessageExtensions.CreateSimpleRandomLetter("TestQueue", 2000);

            await pub.PublishAsync(letter, false);

            var tokenSource = new CancellationTokenSource(delay: TimeSpan.FromSeconds(1));
            async Task ReadReceiptAsync(CancellationToken cancellationToken)
            {
                var receiptBuffer = pub.GetReceiptBufferReader();
                await receiptBuffer.WaitToReadAsync(cancellationToken);
                _ = receiptBuffer.ReadAsync(cancellationToken);
            }

            await Assert
                .ThrowsAnyAsync<OperationCanceledException>(() => ReadReceiptAsync(tokenSource.Token));
        }

        [Fact]
        public async Task PublishAsync()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            var pub = new Publisher(
                await _fixture.GetChannelPoolAsync(),
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            await pub
                .StartAutoPublishAsync()
                .ConfigureAwait(false);

            var letter = MessageExtensions.CreateSimpleRandomLetter("TestQueue", 2000);
            await pub
                .PublishAsync(letter, false)
                .ConfigureAwait(false);
        }

        [Fact(Skip = "only manual")]
        public async Task PublishManyAsync()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            const int letterCount = 10_000;
            const int byteCount = 500;

            var pub = new Publisher(
                await _fixture.GetChannelPoolAsync(),
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

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

            var letters = MessageExtensions.CreateManySimpleRandomLetters(queueNames, letterCount, byteCount);

            var sw = Stopwatch.StartNew();
            await pub
                .PublishManyAsync(letters, false)
                .ConfigureAwait(false);
            sw.Stop();

            const double kiloByteCount = byteCount / 1000.0;
            _fixture.Output.WriteLine($"Published {letterCount} letters, {kiloByteCount} KB each, in {sw.ElapsedMilliseconds} ms.");

            var rate = letterCount / (sw.ElapsedMilliseconds / 1000.0);
            var dataRate = rate * kiloByteCount;
            _fixture.Output.WriteLine($"Message Rate: {rate.ToString("0.###")} letters / sec, or {(dataRate / 1000.0).ToString("0.###")} MB/s");
        }

        [Fact(Skip = "only manual")]
        public void PublishBatchAsync()
        {
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
