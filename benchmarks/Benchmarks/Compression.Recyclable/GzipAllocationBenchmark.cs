using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Compression;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Benchmarks.Compression.Recyclable
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.NetCoreApp31)]
    public class GzipAllocationBenchmark
    {
        private ICompressionProvider CompressionProvider;
        private ICompressionProvider RecyclableProvider;

        private byte[] Payload1 { get; set; } = new byte[5_000];
        private byte[] CompressedPayload1 { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            Enumerable.Repeat<byte>(0xFF, 1000).ToArray().CopyTo(Payload1, 0);
            Enumerable.Repeat<byte>(0xAA, 1000).ToArray().CopyTo(Payload1, 1000);
            Enumerable.Repeat<byte>(0x1A, 1000).ToArray().CopyTo(Payload1, 2000);
            Enumerable.Repeat<byte>(0xAF, 1000).ToArray().CopyTo(Payload1, 3000);
            Enumerable.Repeat<byte>(0x01, 1000).ToArray().CopyTo(Payload1, 4000);

            CompressionProvider = new GzipProvider();
            RecyclableProvider = new RecyclableGzipProvider();
            CompressedPayload1 = CompressionProvider.Compress(Payload1).ToArray();
        }

        [Benchmark(Baseline = true)]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(1_000)]
        public void BasicCompressDecompress_5KBytes(int x)
        {
            for (var i = 0; i < x; i++)
            {
                var compressedData = BasicCompress(Payload1);
                var decompressedData = BasicDecompress(compressedData);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(1_000)]
        public void GzipProvider_5KBytes(int x)
        {
            for (var i = 0; i < x; i++)
            {
                var compressedData = CompressionProvider.Compress(Payload1);
                var decompressedData = CompressionProvider.Decompress(compressedData);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(1_000)]
        public void GzipProviderStream_5KBytes(int x)
        {
            for (var i = 0; i < x; i++)
            {
                var compressedStream = CompressionProvider.CompressToStream(Payload1);
                using var decompressedStream = CompressionProvider.Decompress(compressedStream, false);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(1_000)]
        public void RecylableGzipProvider_5KBytes(int x)
        {
            for (var i = 0; i < x; i++)
            {
                var compressedData = RecyclableProvider.Compress(Payload1);
                var decompressedData = RecyclableProvider.Decompress(compressedData);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(1_000)]
        public void RecyclableGzipProviderStream_5KBytes(int x)
        {
            for (var i = 0; i < x; i++)
            {
                var compressedStream = RecyclableProvider.CompressToStream(Payload1);
                using var decompressedStream = RecyclableProvider.Decompress(compressedStream, false);
            }
        }

        public byte[] BasicCompress(byte[] data)
        {
            using var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
            {
                gzipStream.Write(data);
            }

            return compressedStream.ToArray();
        }

        public byte[] BasicDecompress(byte[] compressedData)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
            {
                gzipStream.CopyTo(uncompressedStream);
            }

            return uncompressedStream.ToArray();
        }
    }
}
