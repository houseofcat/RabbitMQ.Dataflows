using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Examples.RabbitMQ.DataProducer
{
    public static class Program
    {
        private static ISerializationProvider _serializationProvider;
        private static IHashingProvider _hashingProvider;
        private static ICompressionProvider _compressionProvider;
        private static IEncryptionProvider _encryptionProvider;
        private static IRabbitService _rabbitService;

        public static long GlobalCount = 1_000_000;
        public static LogLevel LogLevel = LogLevel.Information;

        public static async Task Main()
        {
            await Console.Out.WriteLineAsync("Run a DataProducer demo... press any key to continue!").ConfigureAwait(false);
            Console.ReadKey(); // memory snapshot baseline

            // Create RabbitService and stage the messages in the queue
            await Console.Out.WriteLineAsync("Setting up RabbitService...").ConfigureAwait(false);
            await SetupAsync().ConfigureAwait(false);

            await Console.Out.WriteLineAsync("Making messages!").ConfigureAwait(false);
            await MakeDataAsync().ConfigureAwait(false);

            await Console.Out.WriteLineAsync("Finished queueing messages... wait here for queue to fill!").ConfigureAwait(false);
            Console.ReadKey(); // checking for memory leak after publishing (snapshots)

            await Console.Out.WriteLineAsync("Shutting down...").ConfigureAwait(false);
            await _rabbitService.ShutdownAsync(false);

            await Console.Out.WriteLineAsync("Shutdown!").ConfigureAwait(false);
            Console.ReadKey(); // checking for memory leak after shutdown (snapshots)
        }

        private static async Task SetupAsync()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel));

            _hashingProvider = new Argon2IDHasher();
            var hashKey = await _hashingProvider.GetHashKeyAsync("passwordforencryption", "saltforencryption", 32).ConfigureAwait(false);

            _encryptionProvider = new AesGcmEncryptionProvider(hashKey, _hashingProvider.Type);
            _compressionProvider = new LZ4PickleProvider();
            _serializationProvider = new Utf8JsonProvider();

            _rabbitService = new RabbitService(
                "Config.json",
                _serializationProvider,
                _encryptionProvider,
                _compressionProvider,
                loggerFactory);

            await _rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);
        }

        private static async Task MakeDataAsync()
        {
            var letterTemplate = new Letter("", "TestRabbitServiceQueue", null, new LetterMetadata());

            for (var i = 0L; i < GlobalCount; i++)
            {
                var letter = letterTemplate.Clone();
                letter.Body = _serializationProvider.Serialize(new Message { StringMessage = $"Sensitive ReceivedLetter {i}", MessageId = i });
                letter.LetterId = (ulong)i;
                await _rabbitService
                    .Publisher
                    .QueueLetterAsync(letter)
                    .ConfigureAwait(false);
            }
        }

        public class Message
        {
            public long MessageId { get; set; }
            public string StringMessage { get; set; }
        }
    }
}
