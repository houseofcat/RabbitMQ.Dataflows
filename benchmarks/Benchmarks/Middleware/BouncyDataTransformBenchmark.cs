using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Compression;
using HouseofCat.Data;
using HouseofCat.Dataflows;
using HouseofCat.Encryption;
using HouseofCat.Encryption.BouncyCastle;
using HouseofCat.Hashing;
using HouseofCat.Hashing.Argon;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Time;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks.Middleware
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.Net60)]
    public class BouncyDataTransformBenchmark
    {
        private DataTransformer _middleware;

        private const string Passphrase = "SuperNintendoHadTheBestZelda";
        private const string Salt = "SegaGenesisIsTheBestConsole";

        private static byte[] _data = new byte[5000];
        private MyCustomClass MyClass = new MyCustomClass();

        private static byte[] _serializedData;

        [GlobalSetup]
        public void Setup()
        {
            Enumerable.Repeat<byte>(0xFF, 1000).ToArray().CopyTo(_data, 0);
            Enumerable.Repeat<byte>(0xAA, 1000).ToArray().CopyTo(_data, 1000);
            Enumerable.Repeat<byte>(0x1A, 1000).ToArray().CopyTo(_data, 2000);
            Enumerable.Repeat<byte>(0xAF, 1000).ToArray().CopyTo(_data, 3000);
            Enumerable.Repeat<byte>(0x01, 1000).ToArray().CopyTo(_data, 4000);

            MyClass.ByteData = _data;

            var hashingProvider = new Argon2ID_HashingProvider();
            var hashKey = hashingProvider.GetHashKey(Passphrase, Salt, 32);

            _middleware = new DataTransformer(
                new Utf8JsonProvider(),
                new AesGcmEncryptionProvider(hashKey),
                new GzipProvider());

            _serializedData = _middleware
                .SerializeAsync(MyClass)
                .GetAwaiter()
                .GetResult()
                .ToArray();
        }

        [Benchmark(Baseline = true)]
        public void Serialize_7KB()
        {
            _middleware.Serialize(MyClass);
        }

        [Benchmark]
        public void Deserialize_7KB()
        {
            _middleware.Deserialize<MyCustomClass>(_serializedData);
        }

        [Benchmark]
        public async Task SerializeAsync_7KB()
        {
            await _middleware.SerializeAsync(MyClass);
        }

        [Benchmark]
        public async Task DeserializeAsync_7KB()
        {
            await _middleware.DeserializeAsync<MyCustomClass>(_serializedData);
        }
    }
}
