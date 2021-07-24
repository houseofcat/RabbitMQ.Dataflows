using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Compression;
using HouseofCat.Data;
using HouseofCat.Data.Recyclable;
using HouseofCat.Dataflows;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
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
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.NetCoreApp31)]
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

            var hashingProvider = new Argon2IDHasher();
            var hashKey = hashingProvider
                .GetHashKeyAsync(Passphrase, Salt, 32)
                .GetAwaiter()
                .GetResult();

            _middleware = new RecyclableTransformer(
                new Utf8JsonProvider(),
                new RecyclableAesGcmEncryptionProvider(hashKey, hashingProvider.Type),
                new RecyclableGzipProvider());

            (var buffer, var length) = _middleware.Input(MyClass);
            _serializedData = buffer.ToArray();
            _serializedLength = length;
        }

        [Benchmark(Baseline = true)]
        public void Serialize_7KB()
        {
            _middleware.Input(MyClass);
        }

        [Benchmark]
        public void Deserialize_7KB()
        {
            _middleware.Output<MyCustomClass>(_serializedData);
        }
    }
}
