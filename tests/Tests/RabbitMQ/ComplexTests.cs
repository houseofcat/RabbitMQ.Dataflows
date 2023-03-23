using HouseofCat.RabbitMQ;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RabbitMQ
{
    public class ComplexTests : IClassFixture<RabbitFixture>
    {
        private readonly RabbitFixture _fixture;

        public ComplexTests(RabbitFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.Output = output;
        }

        [Fact(Skip = "only manual")]
        public async Task PublishAndCountReceipts()
        {
            if (!await _fixture.RabbitConnectionCheckAsync.ConfigureAwait(false))
            {
                return;
            }

            var service = await _fixture.GetRabbitServiceAsync().ConfigureAwait(false);
            var topologer = service.Topologer;
            await topologer.CreateQueueAsync("TestAutoPublisherConsumerQueue").ConfigureAwait(false);

            var publisher = service.Publisher;
            await publisher.StartAutoPublishAsync().ConfigureAwait(false);

            const ulong count = 1000;

            var processReceiptsTask = ProcessReceiptsAsync(publisher, count);
            var publishLettersTask = PublishLettersAsync(publisher, count);

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
            
            await topologer.DeleteQueueAsync("TestAutoPublisherConsumerQueue").ConfigureAwait(false);
        }

        [Fact(Skip = "only manual")]
        public async Task PublishAndConsume()
        {
            if (!await _fixture.RabbitConnectionCheckAsync.ConfigureAwait(false))
            {
                return;
            }

            var service = await _fixture.GetRabbitServiceAsync().ConfigureAwait(false);
            var topologer = service.Topologer;
            await topologer.CreateQueueAsync("TestAutoPublisherConsumerQueue").ConfigureAwait(false);

            var publisher = service.Publisher;
            await publisher.StartAutoPublishAsync().ConfigureAwait(false);

            const ulong count = 1000;

            var consumer = new Consumer(service.ChannelPool, "TestAutoPublisherConsumerName");
            var processReceiptsTask = ProcessReceiptsAsync(publisher, count);
            var publishLettersTask = PublishLettersAsync(publisher, count);
            var consumeMessagesTask = ConsumeMessagesAsync(consumer, count);

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
            
            await topologer.DeleteQueueAsync("TestAutoPublisherConsumerQueue").ConfigureAwait(false);
        }

        private async Task PublishLettersAsync(IPublisher apub, ulong count)
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

        private async Task<bool> ProcessReceiptsAsync(IPublisher apub, ulong count)
        {
            await Task.Yield();

            var buffer = apub.GetReceiptBufferReader();
            var receiptCount = 0ul;
            var error = false;

            var sw = Stopwatch.StartNew();
            while (receiptCount < count)
            {
                var receipt = await buffer.ReadAsync().ConfigureAwait(false);
                receiptCount++;
                if (receipt.IsError)
                { error = true; break; }
            }
            sw.Stop();

            _fixture.Output.WriteLine($"Finished getting receipts.\r\nReceiptCount: {receiptCount} in {sw.ElapsedMilliseconds} ms.\r\nErrorStatus: {error}");

            return error;
        }

        private async Task<bool> ConsumeMessagesAsync(IConsumer<ReceivedData> consumer, ulong count)
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
