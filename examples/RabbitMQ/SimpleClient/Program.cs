using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Utf8Json.Resolvers;

namespace Examples.RabbitMQ.SimpleClient
{
    public static class Program
    {
        private static ISerializationProvider _serializationProvider;
        private static IHashingProvider _hashingProvider;
        private static ICompressionProvider _compressionProvider;
        private static IEncryptionProvider _encryptionProvider;
        private static IRabbitService _rabbitService;

        public static async Task Main()
        {
            _hashingProvider = new Argon2IDHasher();
            var hashKey = await _hashingProvider.GetHashKeyAsync("passwordforencryption", "saltforencryption", 32).ConfigureAwait(false);

            _encryptionProvider = new AesGcmEncryptionProvider(hashKey, _hashingProvider.Type);
            _compressionProvider = new GzipProvider();
            _serializationProvider = new Utf8JsonProvider(StandardResolver.Default);

            _rabbitService = new RabbitService(
                "Config.json",
                _serializationProvider,
                _encryptionProvider,
                _compressionProvider,
                null);

            await RunSimpleClientWithEncryptionAsync()
                .ConfigureAwait(false);

            await RunDataExecutionEngineAsync()
                .ConfigureAwait(false);
        }

        private static async Task RunSimpleClientWithEncryptionAsync()
        {
            await Console.Out.WriteLineAsync("Starting SimpleClient w/ Encryption...").ConfigureAwait(false);

            var sentMessage = new TestMessage { Message = "Sensitive Message" };
            var letter = new Letter("", "TestRabbitServiceQueue", JsonSerializer.SerializeToUtf8Bytes(sentMessage), new LetterMetadata());

            await _rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            // Queue the letter for delivery by the library.
            await _rabbitService
                .Publisher
                .QueueMessageAsync(letter);

            // Start Consumer
            var consumer = _rabbitService.GetConsumer("ConsumerFromConfig");
            await consumer
                .StartConsumerAsync()
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
            await _rabbitService
                .Topologer
                .DeleteQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        public class TestMessage
        {
            public string Message { get; set; }
        }

        private static async Task RunDataExecutionEngineAsync()
        {
            await Console.Out.WriteLineAsync("Starting SimpleClient w/ Encryption As An DataExecutionEngine...").ConfigureAwait(false);

            var letterTemplate = new Letter("", "TestRabbitServiceQueue", null, new LetterMetadata());

            await _rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            // Produce Messages
            for (ulong i = 0; i < 100; i++)
            {
                var letter = letterTemplate.Clone();
                letter.MessageId = Guid.NewGuid().ToString();
                var sentMessage = new TestMessage { Message = "Sensitive Message" };
                sentMessage.Message += $" {i}";
                letter.Body = JsonSerializer.SerializeToUtf8Bytes(sentMessage);
                await _rabbitService
                    .Publisher
                    .QueueMessageAsync(letter);
            }

            // Start Consumer As An Execution Engine
            var consumer = _rabbitService.GetConsumer("ConsumerFromConfig");
            await consumer
                .StartConsumerAsync()
                .ConfigureAwait(false);

            _ = Task.Run(() => consumer.DataflowExecutionEngineAsync(ConsumerWorkerAsync, 7));

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        private static async Task<bool> ConsumerWorkerAsync(ReceivedData data)
        {
            try
            {
                var decodedLetter = JsonSerializer.Deserialize<TestMessage>(data.Letter.Body);

                await Console.Out.WriteLineAsync($"LetterId: {data.Letter.MessageId} Received: {decodedLetter.Message}").ConfigureAwait(false);

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
