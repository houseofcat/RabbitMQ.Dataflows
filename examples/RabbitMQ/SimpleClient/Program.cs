using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Service;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CookedRabbit.Core.SimpleClient
{
    public static class Program
    {
        public class TestMessage
        {
            public string Message { get; set; }
        }

        public static async Task Main()
        {
            await RunSimpleClientWithEncryptionAsync()
                .ConfigureAwait(false);

            await RunExecutionEngineAsync()
                .ConfigureAwait(false);

            await RunParallelExecutionEngineAsync()
                .ConfigureAwait(false);

            await RunDataExecutionEngineAsync()
                .ConfigureAwait(false);
        }

        private static async Task RunSimpleClientWithEncryptionAsync()
        {
            await Console.Out.WriteLineAsync("Starting SimpleClient w/ Encryption...").ConfigureAwait(false);

            var sentMessage = new TestMessage { Message = "Sensitive Message" };
            var letter = new Letter("", "TestRabbitServiceQueue", JsonSerializer.SerializeToUtf8Bytes(sentMessage), new LetterMetadata());

            var rabbitService = new RabbitService("Config.json", "passwordforencryption", "saltforencryption");

            await rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            // Queue the letter for delivery by the library.
            await rabbitService
                .Publisher
                .QueueLetterAsync(letter);

            // Start Consumer
            var consumer = rabbitService.GetConsumer("ConsumerFromConfig");
            await consumer
                .StartConsumerAsync(false, true)
                .ConfigureAwait(false);

            // Get Message From Consumer
            var receivedLetter = await consumer
                .ReadAsync()
                .ConfigureAwait(false);

            // Do work with message inside the receivedLetter
            var decodedLetter = JsonSerializer.Deserialize<TestMessage>(receivedLetter.Letter.Body);

            await Console.Out.WriteLineAsync($"Sent: {sentMessage.Message}").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"Received: {decodedLetter.Message}").ConfigureAwait(false);

            // Acknowledge Message
            if (receivedLetter.Ackable)
            { receivedLetter.AckMessage(); }

            // Cleanup the queue
            await rabbitService
                .Topologer
                .DeleteQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        private static async Task RunExecutionEngineAsync()
        {
            await Console.Out.WriteLineAsync("Starting SimpleClient w/ Encryption As An ExecutionEngine...").ConfigureAwait(false);

            var sentMessage = new TestMessage { Message = "Sensitive Message" };
            var letter = new Letter("", "TestRabbitServiceQueue", JsonSerializer.SerializeToUtf8Bytes(sentMessage), new LetterMetadata());

            var rabbitService = new RabbitService("Config.json", "passwordforencryption", "saltforencryption");

            await rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            // Queue the letter for delivery by the library.
            await rabbitService
                .Publisher
                .QueueLetterAsync(letter);

            // Start Consumer
            var consumer = rabbitService.GetConsumer("ConsumerFromConfig");
            await consumer
                .StartConsumerAsync(false, true)
                .ConfigureAwait(false);

            //_ = Task.Run(() => consumer.ExecutionEngineAsync(ConsumerWorkerAsync));

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        private static async Task RunParallelExecutionEngineAsync()
        {
            await Console.Out.WriteLineAsync("Starting SimpleClient w/ Encryption As An ParallelExecutionEngine...").ConfigureAwait(false);

            var letterTemplate = new Letter("", "TestRabbitServiceQueue", null, new LetterMetadata());
            var rabbitService = new RabbitService("Config.json", "passwordforencryption", "saltforencryption");

            await rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            // Produce Messages
            for (ulong i = 0; i < 100; i++)
            {
                var letter = letterTemplate.Clone();
                letter.LetterId = i;
                var sentMessage = new TestMessage { Message = "Sensitive Message" };
                sentMessage.Message += $" {i}";
                letter.Body = JsonSerializer.SerializeToUtf8Bytes(sentMessage);
                await rabbitService
                    .Publisher
                    .QueueLetterAsync(letter);
            }

            // Start Consumer As An Execution Engine
            var consumer = rabbitService.GetConsumer("ConsumerFromConfig");
            await consumer
                .StartConsumerAsync(false, true)
                .ConfigureAwait(false);

            //_ = Task.Run(() => consumer.ParallelExecutionEngineAsync(ConsumerWorkerAsync, 7));

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        private static async Task RunDataExecutionEngineAsync()
        {
            await Console.Out.WriteLineAsync("Starting SimpleClient w/ Encryption As An DataExecutionEngine...").ConfigureAwait(false);

            var letterTemplate = new Letter("", "TestRabbitServiceQueue", null, new LetterMetadata());
            var rabbitService = new RabbitService("Config.json", "passwordforencryption", "saltforencryption");

            await rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            // Produce Messages
            for (ulong i = 0; i < 100; i++)
            {
                var letter = letterTemplate.Clone();
                letter.LetterId = i;
                var sentMessage = new TestMessage { Message = "Sensitive Message" };
                sentMessage.Message += $" {i}";
                letter.Body = JsonSerializer.SerializeToUtf8Bytes(sentMessage);
                await rabbitService
                    .Publisher
                    .QueueLetterAsync(letter);
            }

            // Start Consumer As An Execution Engine
            var consumer = rabbitService.GetConsumer("ConsumerFromConfig");
            await consumer
                .StartConsumerAsync(false, true)
                .ConfigureAwait(false);

            _ = Task.Run(() => consumer.DataflowExecutionEngineAsync(ConsumerWorkerAsync, 7));

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        private static async Task<bool> ConsumerWorkerAsync(ReceivedData data)
        {
            try
            {
                var decodedLetter = JsonSerializer.Deserialize<TestMessage>(data.Letter.Body);

                await Console.Out.WriteLineAsync($"LetterId: {data.Letter.LetterId} Received: {decodedLetter.Message}").ConfigureAwait(false);

                // Return true or false to ack / nack the message. Exceptions thrown automatically nack the message.
                // Strategy would be that you control the retry / permanent error in this method and return true.
                // Transient outage can be thrown and it will go back to it's place in the Rabbit queue.
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"Message: {ex.Message}").ConfigureAwait(false);
            }
            return true;
        }
    }
}
