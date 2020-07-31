using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities.File;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CookedRabbit.Core.Tests
{
    public class PublisherConsumerTests
    {
        private readonly ITestOutputHelper output;
        private readonly Options options;
        private readonly Topologer topologer;
        private readonly Publisher publisher;
        private readonly Consumer consumer;

        public PublisherConsumerTests(ITestOutputHelper output)
        {
            this.output = output;
            options = JsonFileReader.ReadFileAsync<Options>("TestConfig.json").GetAwaiter().GetResult();

            var channelPool = new ChannelPool(options);
            topologer = new Topologer(channelPool);

            publisher = new Publisher(channelPool, new byte[] { });
            consumer = new Consumer(channelPool, "TestAutoPublisherConsumerName");
        }

        [Fact]
        public async Task AutoPublishAndConsume()
        {
            await topologer.CreateQueueAsync("TestAutoPublisherConsumerQueue").ConfigureAwait(false);
            await publisher.StartAutoPublishAsync().ConfigureAwait(false);

            const ulong count = 10000;

            var processReceiptsTask = ProcessReceiptsAsync(publisher, count);
            var publishLettersTask = PublishLettersAsync(publisher, count);
            var consumeMessagesTask = ConsumeMessagesAsync(consumer, count);

            while (!publishLettersTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            while (!processReceiptsTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            await publisher.StopAutoPublishAsync().ConfigureAwait(false);

            while (!consumeMessagesTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            Assert.True(publishLettersTask.IsCompletedSuccessfully);
            Assert.True(processReceiptsTask.IsCompletedSuccessfully);
            Assert.True(consumeMessagesTask.IsCompletedSuccessfully);
            Assert.False(processReceiptsTask.Result);
            Assert.False(consumeMessagesTask.Result);

            await topologer.DeleteQueueAsync("TestAutoPublisherConsumerQueue").ConfigureAwait(false);
        }

        private async Task PublishLettersAsync(Publisher apub, ulong count)
        {
            var sw = Stopwatch.StartNew();
            for (ulong i = 0; i < count; i++)
            {
                var letter = RandomData.CreateSimpleRandomLetter("TestAutoPublisherConsumerQueue");
                letter.LetterId = i;

                await apub.QueueLetterAsync(letter).ConfigureAwait(false);
            }
            sw.Stop();

            output.WriteLine($"Finished queueing all letters in {sw.ElapsedMilliseconds} ms.");
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
                if (buffer.TryRead(out var receipt))
                {
                    receiptCount++;
                    if (receipt.IsError)
                    { error = true; break; }
                }
            }
            sw.Stop();

            output.WriteLine($"Finished getting receipts.\r\nReceiptCount: {receiptCount} in {sw.ElapsedMilliseconds} ms.\r\nErrorStatus: {error}");

            return error;
        }

        private async Task<bool> ConsumeMessagesAsync(Consumer consumer, ulong count)
        {
            var messageCount = 0ul;
            var error = false;

            await consumer
                .StartConsumerAsync(true, true)
                .ConfigureAwait(false);

            var sw = Stopwatch.StartNew();
            while (messageCount < count)
            {
                try
                {
                    var message = await consumer.ReadAsync().ConfigureAwait(false);
                    messageCount++;
                }
                catch
                { error = true; break; }
            }
            sw.Stop();

            output.WriteLine($"Finished consuming messages.\r\nMessageCount: {messageCount} in {sw.ElapsedMilliseconds} ms.\r\nErrorStatus: {error}");
            return error;
        }
    }
}
