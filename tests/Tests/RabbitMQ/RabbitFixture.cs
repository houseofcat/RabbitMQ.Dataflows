using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Hashing.Argon;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using HouseofCat.Utilities.File;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace RabbitMQ
{
    public class RabbitFixture
    {
        public ITestOutputHelper Output;
        public readonly ISerializationProvider SerializationProvider;
        public readonly IHashingProvider HashingProvider;
        public readonly IEncryptionProvider EncryptionProvider;
        public readonly ICompressionProvider CompressionProvider;

        public const string Passphrase = "SuperNintendoHadTheBestZelda";
        public const string Salt = "SegaGenesisIsTheBestConsole";
        public readonly byte[] HashKey;

        public readonly RabbitOptions Options;
        public readonly RabbitService RabbitService;
        public readonly IChannelPool ChannelPool;
        public readonly ITopologer Topologer;
        public readonly IPublisher Publisher;

        public RabbitFixture()
        {
            CompressionProvider = new GzipProvider();
            HashingProvider = new Argon2ID_HashingProvider();
            HashKey = HashingProvider.GetHashKey(Passphrase, Salt, 32);
            EncryptionProvider = new AesGcmEncryptionProvider(HashKey, HashingProvider.Type);
            SerializationProvider = new Utf8JsonProvider();

            Options = JsonFileReader.ReadFileAsync<RabbitOptions>("TestConfig.json").GetAwaiter().GetResult();

            RabbitService = new RabbitService(
                Options,
                SerializationProvider,
                EncryptionProvider,
                CompressionProvider,
                LoggerFactory
                    .Create(
                        builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information)));

            ChannelPool = RabbitService.ChannelPool;
            Topologer = RabbitService.Topologer;
            Publisher = RabbitService.Publisher;
        }
    }
}
