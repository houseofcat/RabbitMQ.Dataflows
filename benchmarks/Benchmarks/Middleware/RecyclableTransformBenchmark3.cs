using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Compression;
using HouseofCat.Data;
using HouseofCat.Data.Recyclable;
using HouseofCat.Dataflows;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
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
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.NetCoreApp31)]
    public class RecyclableTransformBenchmark3
    {
        private RecyclableTransformer _middleware;

        private const string Passphrase = "SuperNintendoHadTheBestZelda";
        private const string Salt = "SegaGenesisIsTheBestConsole";

        private static byte[] _data = new byte[10_000];
        private MyCustomClass MyClass = new MyCustomClass();

        [GlobalSetup]
        public void Setup()
        {
            Enumerable.Repeat<byte>(0xFF, 2_000).ToArray().CopyTo(_data, 0);
            Enumerable.Repeat<byte>(0xAA, 2_000).ToArray().CopyTo(_data, 2_000);
            Enumerable.Repeat<byte>(0x1A, 2_000).ToArray().CopyTo(_data, 4_000);
            Enumerable.Repeat<byte>(0xAF, 2_000).ToArray().CopyTo(_data, 6_000);
            Enumerable.Repeat<byte>(0x01, 2_000).ToArray().CopyTo(_data, 8_000);

            MyClass.ByteData = _data;

            var hashingProvider = new Argon2IDHasher();
            var hashKey = hashingProvider.GetHashKey(Passphrase, Salt, 32);

            RecyclableManager.ConfigureNewStaticManagerWithDefaults();

            _middleware = new RecyclableTransformer(
                new Utf8JsonProvider(),
                new RecyclableAesGcmEncryptionProvider(hashKey, hashingProvider.Type),
                new RecyclableGzipProvider());
        }

        [Benchmark(Baseline = true)]
        [Arguments(10)]
        [Arguments(100)]
        public void Serialize_10KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                _middleware.Input(MyClass);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        public void Serialize_Deserialize_12KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                (var buffer, var length) = _middleware.Input(MyClass);
                _middleware.Output<MyCustomClass>(buffer.ToArray());
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        public void Serialize_Stream_12KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var transformedStream = _middleware.InputToStream(MyClass);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        public void Serialize_Deserialize_Stream_12KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var transformedStream = _middleware.InputToStream(MyClass);
                _middleware.Output<MyCustomClass>(transformedStream);
            }
        }

        [Benchmark]
        public void Middleware_Input1()
        {
            Middleware_Input1(MyClass);
        }

        public void Middleware_Input1<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            _middleware.SerializationProvider.Serialize(serializedStream, input);
        }

        [Benchmark]
        public void Middleware_Input2()
        {
            Middleware_Input2(MyClass);
        }

        public void Middleware_Input2<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            _middleware.SerializationProvider.Serialize(serializedStream, input);

            using var compressedStream = _middleware.CompressionProvider.Compress(serializedStream, false);
            var encryptedStream = _middleware.EncryptionProvider.Encrypt(compressedStream, false);
        }

        [Benchmark]
        public void Middleware_Input3()
        {
            Middleware_Input3(MyClass);
        }

        public (ArraySegment<byte>, long) Middleware_Input3<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            _middleware.SerializationProvider.Serialize(serializedStream, input);

            using var compressedStream = _middleware.CompressionProvider.Compress(serializedStream, false);
            var encryptedStream = _middleware.EncryptionProvider.Encrypt(compressedStream, false);

            var length = encryptedStream.Length;
            if (encryptedStream.TryGetBuffer(out var buffer))
            { return (buffer, length); }
            else
            { return (encryptedStream.ToArray(), length); }
        }

        [Benchmark]
        public void Middleware_InputToStream1()
        {
            Middleware_InputToStream1(MyClass);
        }

        public void Middleware_InputToStream1<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            _middleware.SerializationProvider.Serialize(serializedStream, input);
        }

        [Benchmark]
        public void Middleware_InputToStream2()
        {
            Middleware_InputToStream2(MyClass);
        }

        public void Middleware_InputToStream2<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            _middleware.SerializationProvider.Serialize(serializedStream, input);

            using var compressedStream = _middleware.CompressionProvider.Compress(serializedStream, false);
        }

        [Benchmark]
        public void Middleware_InputToStream3()
        {
            Middleware_InputToStream3(MyClass);
        }

        public MemoryStream Middleware_InputToStream3<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            _middleware.SerializationProvider.Serialize(serializedStream, input);

            using var compressedStream = _middleware.CompressionProvider.Compress(serializedStream, false);

            return _middleware.EncryptionProvider.Encrypt(compressedStream, true);
        }
    }
}
