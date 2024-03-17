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
    public class RecyclableTransformBenchmark4
    {
        private RecyclableTransformer _middleware;

        private const string Passphrase = "SuperNintendoHadTheBestZelda";
        private const string Salt = "SegaGenesisIsTheBestConsole";

        private static byte[] _data = new byte[1_000];
        private MyCustomClass2 MyClass = new MyCustomClass2();

        private static ArraySegment<byte> _serializedData;

        [GlobalSetup]
        public void Setup()
        {
            Enumerable.Repeat<byte>(0xFF, 200).ToArray().CopyTo(_data, 0);
            Enumerable.Repeat<byte>(0xAA, 200).ToArray().CopyTo(_data, 200);
            Enumerable.Repeat<byte>(0x1A, 200).ToArray().CopyTo(_data, 400);
            Enumerable.Repeat<byte>(0xAF, 200).ToArray().CopyTo(_data, 600);
            Enumerable.Repeat<byte>(0x01, 200).ToArray().CopyTo(_data, 800);

            MyClass.ByteData = Encoding.UTF8.GetString(_data);

            var hashingProvider = new Argon2ID_HashingProvider();
            var hashKey = hashingProvider.GetHashKey(Passphrase, Salt, 32);

            //RecyclableManager.ConfigureNewStaticManagerWithDefaults();

            _middleware = new RecyclableTransformer(
                new Utf8JsonProvider(),
                new RecyclableGzipProvider(),
                new RecyclableAesGcmEncryptionProvider(hashKey));

            (var buffer, _) = _middleware.Transform(MyClass);
            _serializedData = buffer.ToArray();
        }

        [Benchmark(Baseline = true)]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Transform_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                _middleware.Transform(MyClass);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Restore_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                _middleware.Restore<MyCustomClass2>(_serializedData);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void TransformRestore_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                (var buffer, var length) = _middleware.Transform(MyClass);
                _middleware.Restore<MyCustomClass2>(buffer.ToArray());
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void TransformToStream_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var transformedStream = _middleware.TransformToStream(MyClass);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void TransformToStreamRestore_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var transformedStream = _middleware.TransformToStream(MyClass);
                _middleware.Restore<MyCustomClass2>(transformedStream);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Middleware_Serialize(int x)
        {
            for (var i = 0; i < x; i++)
            {
                Middleware_Serialize(MyClass);
            }
        }

        public void Middleware_Serialize<TIn>(TIn input)
        {
            _middleware.SerializationProvider.Serialize(input);
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Middleware_Input1(int x)
        {
            for (var i = 0; i < x; i++)
            {
                Middleware_Input1(MyClass);
            }
        }

        public void Middleware_Input1<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            _middleware.SerializationProvider.Serialize(serializedStream, input);
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Middleware_Input2(int x)
        {
            for (var i = 0; i < x; i++)
            {
                Middleware_Input2(MyClass);
            }
        }

        public void Middleware_Input2<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            _middleware.SerializationProvider.Serialize(serializedStream, input);

            using var compressedStream = _middleware.CompressionProvider.Compress(serializedStream, false);
            var encryptedStream = _middleware.EncryptionProvider.Encrypt(compressedStream, false);
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Middleware_Input3(int x)
        {
            for (var i = 0; i < x; i++)
            {
                Middleware_Input3(MyClass);
            }
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
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Middleware_InputOutput1(int x)
        {
            for (var i = 0; i < x; i++)
            {
                Middleware_Input3(MyClass);
                Middleware_Output1(_serializedData);
            }
        }

        public void Middleware_Output1(ReadOnlyMemory<byte> data)
        {
            using var decryptStream = _middleware.EncryptionProvider.DecryptToStream(data);
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Middleware_InputOutput2(int x)
        {
            for (var i = 0; i < x; i++)
            {
                Middleware_Input3(MyClass);
                Middleware_Output2(_serializedData);
            }
        }

        public void Middleware_Output2(ReadOnlyMemory<byte> data)
        {
            using var decryptStream = _middleware.EncryptionProvider.DecryptToStream(data);
            using var decompressStream = _middleware.CompressionProvider.Decompress(decryptStream, false);
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void Middleware_InputOutput3(int x)
        {
            for (var i = 0; i < x; i++)
            {
                Middleware_Input3(MyClass);
                Middleware_Output3<MyCustomClass2>(_serializedData);
            }
        }

        public TOut Middleware_Output3<TOut>(ReadOnlyMemory<byte> data)
        {
            using var decryptStream = _middleware.EncryptionProvider.DecryptToStream(data);
            using var decompressStream = _middleware.CompressionProvider.Decompress(decryptStream, false);
            return _middleware.SerializationProvider.Deserialize<TOut>(decompressStream);
        }
    }
}
