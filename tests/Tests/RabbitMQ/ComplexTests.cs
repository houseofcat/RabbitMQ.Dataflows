using HouseofCat.RabbitMQ;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Xunit;
using Xunit.Abstractions;

namespace RabbitMQ
{
    public class ComplexTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;
        private readonly AsyncLazy<Consumer> _lazyConsumer;
        private readonly AsyncLazy<Publisher> _lazyPublisher;
        public Task<Consumer> ConsumerAsync => _lazyConsumer.Task;
        public Task<Publisher> PublisherAsync => _lazyPublisher.Task;

        public ComplexTests(RabbitFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.Output = output;
            _lazyConsumer = new AsyncLazy<Consumer>(
                async () => new Consumer(await _fixture.ChannelPoolAsync, "TestAutoPublisherConsumerName"));
            _lazyPublisher = new AsyncLazy<Publisher>(
                async () => new Publisher(
                await _fixture.ChannelPoolAsync,
                _fixture.SerializationProvider,
                _fixture.EncryptionProvider,
                _fixture.CompressionProvider));
        }

        [Fact(Skip = "only manual")]
        public async Task PublishAndCountReceipts()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            await (await _fixture.TopologerAsync).CreateQueueAsync("TestAutoPublisherConsumerQueue")
                .ConfigureAwait(false);
            await (await PublisherAsync).StartAutoPublishAsync().ConfigureAwait(false);

            const ulong count = 1000;

            var processReceiptsTask = ProcessReceiptsAsync(await PublisherAsync, count);
            var publishLettersTask = PublishLettersAsync(await PublisherAsync, count);

            await Task.Yield();

            // Note to self: Testing frameworks are sensitive to high concurrent scenarios. You can
            // starve the threadpool / max concurrency and break things without Task.Yield(); Also
            // there should be a better way to test this stuff.
            while (!publishLettersTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            while (!processReceiptsTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            Assert.True(publishLettersTask.IsCompletedSuccessfully);
            Assert.True(processReceiptsTask.IsCompletedSuccessfully);
            Assert.False(processReceiptsTask.Result);
            
            await (await _fixture.TopologerAsync).DeleteQueueAsync("TestAutoPublisherConsumerQueue")
                .ConfigureAwait(false);
        }

        [Fact(Skip = "only manual")]
        public async Task PublishAndConsume()
        {
            if (!await _fixture.RabbitConnectionCheckAsync)
            {
                return;
            }

            await (await _fixture.TopologerAsync).CreateQueueAsync("TestAutoPublisherConsumerQueue")
                .ConfigureAwait(false);
            await (await PublisherAsync).StartAutoPublishAsync().ConfigureAwait(false);

            const ulong count = 1000;

            var processReceiptsTask = ProcessReceiptsAsync(await PublisherAsync, count);
            var publishLettersTask = PublishLettersAsync(await PublisherAsync, count);
            var consumeMessagesTask = ConsumeMessagesAsync(await ConsumerAsync, count);

            await Task.Yield();

            // Note to self: Testing frameworks are sensitive to high concurrency scenarios. You can
            // starve the threadpool / max concurrency and break things without Task.Yield(); Also
            // there should be a better way to test this stuff.
            while (!publishLettersTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            while (!processReceiptsTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            while (!consumeMessagesTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            Assert.True(publishLettersTask.IsCompletedSuccessfully);
            Assert.True(processReceiptsTask.IsCompletedSuccessfully);
            Assert.True(consumeMessagesTask.IsCompletedSuccessfully);
            Assert.False(processReceiptsTask.Result);
            Assert.False(consumeMessagesTask.Result);
            
            await (await _fixture.TopologerAsync).DeleteQueueAsync("TestAutoPublisherConsumerQueue")
                .ConfigureAwait(false);
        }

        private async Task PublishLettersAsync(Publisher apub, ulong count)
        {
            var sw = Stopwatch.StartNew();
            for (ulong i = 0; i < count; i++)
            {
                var letter = MessageExtensions.CreateSimpleRandomLetter("TestAutoPublisherConsumerQueue");
                letter.MessageId = Guid.NewGuid().ToString();

                await apub
                    .QueueMessageAsync(letter)
                    .ConfigureAwait(false);
            }
            sw.Stop();

            _fixture.Output.WriteLine($"Finished queueing all letters in {sw.ElapsedMilliseconds} ms.");
        }

        private async Task<bool> ProcessReceiptsAsync(Publisher apub, ulong count)
        {
            await Task.Yield();

            var buffer = apub.GetReceiptBufferReader();
            var receiptCount = 0ul;
            var error = false;

            var sw = Stopwatch.StartNew();
            while (receiptCount < count)
            {
                var receipt = await buffer.ReadAsync();
                receiptCount++;
                if (receipt.IsError)
                { error = true; break; }
            }
            sw.Stop();

            _fixture.Output.WriteLine($"Finished getting receipts.\r\nReceiptCount: {receiptCount} in {sw.ElapsedMilliseconds} ms.\r\nErrorStatus: {error}");

            return error;
        }

        private async Task<bool> ConsumeMessagesAsync(Consumer consumer, ulong count)
        {
            var messageCount = 0ul;
            var error = false;

            await consumer
                .StartConsumerAsync()
                .ConfigureAwait(false);

            var sw = Stopwatch.StartNew();
            while (messageCount < count)
            {
                try
                {
                    var message = await consumer.ReadAsync().ConfigureAwait(false);
                    message.AckMessage();
                    messageCount++;
                }
                catch
                { error = true; break; }
            }
            sw.Stop();

            _fixture.Output.WriteLine($"Finished consuming messages.\r\nMessageCount: {messageCount} in {sw.ElapsedMilliseconds} ms.\r\nErrorStatus: {error}");
            return error;
        }
    }
}
