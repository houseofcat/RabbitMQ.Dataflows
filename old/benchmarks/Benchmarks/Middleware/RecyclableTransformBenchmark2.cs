using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Compression;
using HouseofCat.Data;
using HouseofCat.Data.Recyclable;
using HouseofCat.Dataflows;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Hashing.Argon;
using HouseofCat.Recyclable;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Time;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks.Middleware
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.Net60)]
    public class RecyclableTransformBenchmark2
    {
        private RecyclableTransformer _middleware;

        private const string Passphrase = "SuperNintendoHadTheBestZelda";
        private const string Salt = "SegaGenesisIsTheBestConsole";

        private static byte[] _data = new byte[5000];
        private MyCustomClass MyClass = new MyCustomClass();

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

            RecyclableManager.ConfigureNewStaticManagerWithDefaults();

            _middleware = new RecyclableTransformer(
                new Utf8JsonProvider(),
                new RecyclableGzipProvider(),
                new RecyclableAesGcmEncryptionProvider(hashKey));
        }

        [Benchmark(Baseline = true)]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Serialize_7KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var transformedStream = _middleware.TransformToStream(MyClass);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Serialize_Deserialize_7KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                (var buffer, var length) = _middleware.Transform(MyClass);
                _middleware.Restore<MyCustomClass>(buffer.ToArray());
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Serialize_Stream_7KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var transformedStream = _middleware.TransformToStream(MyClass);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Serialize_Deserialize_Stream_7KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var transformedStream = _middleware.TransformToStream(MyClass);
                _middleware.Restore<MyCustomClass>(transformedStream);
            }
        }
    }
}
