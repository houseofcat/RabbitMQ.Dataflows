using HouseofCat.RabbitMQ;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HouseofCat.RabbitMQ.IntegrationTests
{
    public class AutoPublisherTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;

        public AutoPublisherTests(RabbitFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.Output = output;
        }

        [Fact]
        public void CreateAutoPublisher()
        {
            var pub = new Publisher(
                _fixture.ChannelPool,
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            Assert.NotNull(pub);
        }

        [Fact]
        public async Task CreateAutoPublisherAndStart()
        {
            var pub = new Publisher(
                _fixture.ChannelPool,
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            await pub.StartAutoPublishAsync().ConfigureAwait(false);

            Assert.NotNull(pub);
        }

        [Fact]
        public async Task CreateAutoPublisherAndPublish()
        {
            var pub = new Publisher(
                _fixture.ChannelPool,
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            var letter = RandomData.CreateSimpleRandomLetter("AutoPublisherTestQueue");
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await pub.QueueLetterAsync(letter));
        }

        [Fact]
        public void CreateAutoPublisherByOptions()
        {
            var pub = new Publisher(
                _fixture.Options,
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            Assert.NotNull(pub);
        }

        [Fact]
        public async Task CreateAutoPublisherByConfigAndStart()
        {
            var pub = new Publisher(
                _fixture.Options,
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            await pub.StartAutoPublishAsync().ConfigureAwait(false);

            Assert.NotNull(pub);
        }

        [Fact]
        public async Task CreateAutoPublisherByConfigAndPublish()
        {
            var pub = new Publisher(
                _fixture.Options,
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            await pub.StartAutoPublishAsync().ConfigureAwait(false);

            var letter = RandomData.CreateSimpleRandomLetter("AutoPublisherTestQueue");
            await pub.QueueLetterAsync(letter).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateAutoPublisherByConfigQueueAndConcurrentPublish()
        {
            var pub = new Publisher(
                _fixture.Options,
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            await pub.StartAutoPublishAsync().ConfigureAwait(false);
            var finished = false;
            const ulong count = 10000;

            await Task.Run(async () =>
            {
                for (ulong i = 0; i < count; i++)
                {
                    var letter = RandomData.CreateSimpleRandomLetter("AutoPublisherTestQueue");
                    letter.LetterId = i;

                    await pub.QueueLetterAsync(letter).ConfigureAwait(false);
                }

                finished = true;
            }).ConfigureAwait(false);

            while (!finished) { await Task.Delay(1).ConfigureAwait(false); }

            await pub.StopAutoPublishAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateAutoPublisherQueueConcurrentPublishAndProcessReceipts()
        {
            var pub = new Publisher(
                _fixture.Options,
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider);

            await pub.StartAutoPublishAsync().ConfigureAwait(false);
            const ulong count = 10000;

            var processReceiptsTask = ProcessReceiptsAsync(pub, count);
            var publishLettersTask = PublishLettersAsync(pub, count);

            while (!publishLettersTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            while (!processReceiptsTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            Assert.True(publishLettersTask.IsCompletedSuccessfully);
            Assert.True(processReceiptsTask.IsCompletedSuccessfully);

            Assert.False(processReceiptsTask.Result);
        }

        private async Task PublishLettersAsync(Publisher pub, ulong count)
        {
            var sw = Stopwatch.StartNew();
            for (ulong i = 0; i < count; i++)
            {
                var letter = RandomData.CreateSimpleRandomLetter("AutoPublisherTestQueue");
                letter.LetterId = i;

                await pub.QueueLetterAsync(letter).ConfigureAwait(false);
            }
            sw.Stop();

            _fixture.Output.WriteLine($"Finished queueing all letters in {sw.ElapsedMilliseconds} ms.");
        }

        private async Task<bool> ProcessReceiptsAsync(Publisher pub, ulong count)
        {
            await Task.Yield();

            var buffer = pub.GetReceiptBufferReader();
            var receiptCount = 0ul;
            var error = false;

            var sw = Stopwatch.StartNew();
            while (receiptCount < count)
            {
                if (buffer.TryRead(out var receipt))
                {
                    receiptCount++;
                    if (receipt.IsError)
                    { error = true; break; }
                }
            }
            sw.Stop();

            await pub.StopAutoPublishAsync().ConfigureAwait(false);

            _fixture.Output.WriteLine($"Finished getting receipts on all published letters in {sw.ElapsedMilliseconds} ms.");

            return error;
        }
    }
}
