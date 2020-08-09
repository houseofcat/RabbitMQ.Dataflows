using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HouseofCat.IntegrationTests.RabbitMQ
{
    public class Integration_Publisher_AutoPublisher_Tests
    {
        private readonly Options options;
        private readonly ITestOutputHelper output;
        private readonly IChannelPool channelPool;
        private readonly Topologer topologer;
        private readonly IHashingProvider _hashingProvider;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly ICompressionProvider _compressionProvider;

        private const string _passphrase = "SuperNintendoHadTheBestZelda";
        private const string _salt = "SegaGenesisIsTheBestConsole";
        private readonly byte[] _hashKey;

        public Integration_Publisher_AutoPublisher_Tests(ITestOutputHelper output)
        {
            this.output = output;
            options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            options.PublisherOptions = new PublisherOptions();
            options.PublisherOptions.CreatePublishReceipts = true;

            channelPool = new ChannelPool(options);
            topologer = new Topologer(channelPool);

            _compressionProvider = new GzipProvider();
            _hashingProvider = new Argon2IDHasher();
            _hashKey = _hashingProvider.GetHashKeyAsync(_passphrase, _salt, 32).GetAwaiter().GetResult();

            _encryptionProvider = new AesGcmEncryptionProvider(_hashKey);
        }

        [Fact]
        public void CreateAutoPublisher()
        {
            var pub = new Publisher(channelPool, _encryptionProvider, _compressionProvider);

            Assert.NotNull(pub);
        }

        [Fact]
        public async Task CreateAutoPublisherAndStart()
        {
            var pub = new Publisher(channelPool, _encryptionProvider, _compressionProvider);
            await pub.StartAutoPublishAsync().ConfigureAwait(false);

            Assert.NotNull(pub);
        }

        [Fact]
        public async Task CreateAutoPublisherAndPublish()
        {
            var pub = new Publisher(channelPool, _encryptionProvider, _compressionProvider);

            var letter = RandomData.CreateSimpleRandomLetter("AutoPublisherTestQueue");
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await pub.QueueLetterAsync(letter));
        }

        [Fact]
        public void CreateAutoPublisherByOptions()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var apub = new Publisher(channelPool, _encryptionProvider, _compressionProvider);

            Assert.NotNull(apub);
        }

        [Fact]
        public async Task CreateAutoPublisherByConfigAndStart()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var pub = new Publisher(channelPool, _encryptionProvider, _compressionProvider);
            await pub.StartAutoPublishAsync().ConfigureAwait(false);

            Assert.NotNull(pub);
        }

        [Fact]
        public async Task CreateAutoPublisherByConfigAndPublish()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var pub = new Publisher(channelPool, _encryptionProvider, _compressionProvider);
            await pub.StartAutoPublishAsync().ConfigureAwait(false);

            var letter = RandomData.CreateSimpleRandomLetter("AutoPublisherTestQueue");
            await pub.QueueLetterAsync(letter).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateAutoPublisherByConfigQueueAndConcurrentPublish()
        {
            var options = new Options();
            options.FactoryOptions.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var pub = new Publisher(channelPool, _encryptionProvider, _compressionProvider);
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
            var pub = new Publisher(channelPool, _encryptionProvider, _compressionProvider);
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

            output.WriteLine($"Finished queueing all letters in {sw.ElapsedMilliseconds} ms.");
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

            output.WriteLine($"Finished getting receipts on all published letters in {sw.ElapsedMilliseconds} ms.");

            return error;
        }
    }
}
