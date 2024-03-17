using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Compression;
using HouseofCat.Data.Recyclable;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Hashing.Argon;
using HouseofCat.Serialization;
using System;
using System.Linq;

namespace Benchmarks.Middleware
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.Net60)]
    public class RecyclableTransformBenchmark
    {
        private RecyclableTransformer _middleware;

        private const string Passphrase = "SuperNintendoHadTheBestZelda";
        private const string Salt = "SegaGenesisIsTheBestConsole";

        private static byte[] _data = new byte[5000];
        private MyCustomClass MyClass = new MyCustomClass();

        private static ArraySegment<byte> _serializedData;
        private static long _serializedLength;

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

            _middleware = new RecyclableTransformer(
                new Utf8JsonProvider(),
                new RecyclableGzipProvider(),
                new RecyclableAesGcmEncryptionProvider(hashKey));

            (var buffer, var length) = _middleware.Transform(MyClass);
            _serializedData = buffer.ToArray();
            _serializedLength = length;
        }

        [Benchmark(Baseline = true)]
        public void Serialize_7KB()
        {
            _middleware.Transform(MyClass);
        }

        [Benchmark]
        public void Deserialize_7KB()
        {
            _middleware.Restore<MyCustomClass>(_serializedData);
        }
    }
}
