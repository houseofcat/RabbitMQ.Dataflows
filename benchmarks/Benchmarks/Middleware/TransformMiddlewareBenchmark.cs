using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Compression;
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

namespace HouseofCat.Benchmarks.Middleware
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.NetCoreApp31)]
    public class TransformMiddlewareBenchmark
    {
        private TransformMiddleware _middleware;

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

            var hashingProvider = new Argon2IDHasher();
            var hashKey = hashingProvider
                .GetHashKeyAsync(Passphrase, Salt, 32)
                .GetAwaiter()
                .GetResult();

            _middleware = new TransformMiddleware(
                new Utf8JsonProvider(),
                new AesGcmEncryptionProvider(hashKey, hashingProvider.Type),
                new GzipProvider());

            _serializedData = _middleware
                .SerializeAsync(MyClass)
                .GetAwaiter()
                .GetResult()
                .ToArray();
        }

        [Benchmark(Baseline = true)]
        public async Task SerializeAsync()
        {
            await _middleware.SerializeAsync(MyClass);
        }

        [Benchmark]
        public async Task DeserializeAsync()
        {
            await _middleware.DeserializeAsync<MyCustomClass>(_serializedData);
        }
    }

    #region MyCustomObject

    public class MyCustomClass
    {
        public MyCustomEmbeddedClass EmbeddedClass { get; set; } = new MyCustomEmbeddedClass();
        public string MyString { get; set; } = "Crazy String Value";
        public byte[] ByteData { get; set; }

        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>
        {
            { "I like to eat", "Apples and Bananas" },
            { "TestKey", 12 },
            { "TestKey2", 12.0 },
            { "Date", Time.GetDateTimeNow(Time.Formats.CatRFC3339) }
        };

        public IDictionary<string, object> AbstractData { get; set; } = new Dictionary<string, object>
        {
            { "I like to eat", "Apples and Bananas" },
            { "TestKey", 12 },
            { "TestKey2", 12.0 },
            { "Date", Time.GetDateTimeNow(Time.Formats.CatRFC3339) }
        };

        public MyCustomSubClass SubClass { get; set; } = new MyCustomSubClass();

        public class MyCustomEmbeddedClass
        {
            public int HappyNumber { get; set; } = 42;
            public byte HappyByte { get; set; } = 0xFE;
        }
    }

    public class MyCustomSubClass
    {
        public List<int> Ints { get; set; } = new List<int> { 2, 4, 6, 8, 10 };
        public List<double> Doubles { get; set; } = new List<double> { 1.0, 1.0, 2.0, 3.0, 5.0, 8.0, 13.0 };
    }

    #endregion
}
