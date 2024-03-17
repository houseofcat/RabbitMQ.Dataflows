using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Compression;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Benchmarks.Compression
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.Net60)]
    public class GzipBenchmark
    {
        private ICompressionProvider CompressionProvider;

        private byte[] Payload1 { get; set; } = new byte[5000];
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
            CompressedPayload1 = CompressionProvider.Compress(Payload1).ToArray();
        }

        [Benchmark(Baseline = true)]
        public void BasicCompress5KBytes()
        {
            var data = BasicCompress(Payload1);
        }

        [Benchmark]
        public void Compress5KBytes()
        {
            var data = CompressionProvider.Compress(Payload1);
        }

        [Benchmark]
        public async Task Compress5KBytesAsync()
        {
            var data = await CompressionProvider.CompressAsync(Payload1);
        }

        [Benchmark]
        public void Compress5KBytesToStream()
        {
            var stream = CompressionProvider.CompressToStream(Payload1);
        }

        [Benchmark]
        public async Task Compress5KBytesToStreamAsync()
        {
            var stream = await CompressionProvider.CompressToStreamAsync(Payload1);
        }

        [Benchmark]
        public void BasicDecompress5KBytes()
        {
            var data = BasicDecompress(CompressedPayload1);
        }

        [Benchmark]
        public void Decompress5KBytes()
        {
            var data = CompressionProvider.Decompress(CompressedPayload1);
        }

        [Benchmark]
        public async Task Decompress5KBytesAsync()
        {
            var data = await CompressionProvider.DecompressAsync(CompressedPayload1);
        }

        [Benchmark]
        public void Decompress5KBytesFromStream()
        {
            var stream = CompressionProvider.Decompress(new MemoryStream(CompressedPayload1));
        }

        [Benchmark]
        public async Task Decompress5KBytesFromStreamAsync()
        {
            var stream = await CompressionProvider.DecompressAsync(new MemoryStream(CompressedPayload1));
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
