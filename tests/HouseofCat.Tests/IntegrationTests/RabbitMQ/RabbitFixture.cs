using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using HouseofCat.Utilities.File;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace HouseofCat.Tests.IntegrationTests.RabbitMQ
{
    public class RabbitFixture
    {
        public readonly ITestOutputHelper Output;
        public readonly ISerializationProvider SerializationProvider;
        public readonly IHashingProvider HashingProvider;
        public readonly IEncryptionProvider EncryptionProvider;
        public readonly ICompressionProvider CompressionProvider;

        public const string Passphrase = "SuperNintendoHadTheBestZelda";
        public const string Salt = "SegaGenesisIsTheBestConsole";
        public readonly byte[] HashKey;

        public readonly Options Options;
        public readonly RabbitService RabbitService;
        public readonly IChannelPool ChannelPool;
        public readonly ITopologer Topologer;
        public readonly IPublisher Publisher;

        public RabbitFixture(ITestOutputHelper output)
        {
            Output = output;
            CompressionProvider = new GzipProvider();
            HashingProvider = new Argon2IDHasher();
            HashKey = HashingProvider.GetHashKeyAsync(Passphrase, Salt, 32).GetAwaiter().GetResult();
            EncryptionProvider = new AesGcmEncryptionProvider(HashKey);
            SerializationProvider = new Utf8JsonProvider();

            Options = JsonFileReader.ReadFileAsync<Options>("Config.json").GetAwaiter().GetResult();

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
