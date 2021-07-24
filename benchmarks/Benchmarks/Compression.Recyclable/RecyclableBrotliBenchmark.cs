using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Compression;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Benchmarks.Compression.Recyclable
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.NetCoreApp31)]
    public class RecyclableBrotliBenchmark
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

            CompressionProvider = new RecyclableBrotliProvider();
            CompressedPayload1 = CompressionProvider.Compress(Payload1).ToArray();
        }

        [Benchmark(Baseline = true)]
        public void Compress5KBytes()
        {
            CompressionProvider.Compress(Payload1);
        }

        [Benchmark]
        public async Task Compress5KBytesAsync()
        {
            await CompressionProvider.CompressAsync(Payload1);
        }

        [Benchmark]
        public void Compress5KBytesToStream()
        {
            CompressionProvider.CompressToStream(Payload1);
        }

        [Benchmark]
        public async Task Compress5KBytesToStreamAsync()
        {
            await CompressionProvider.CompressToStreamAsync(Payload1);
        }

        [Benchmark]
        public void Decompress5KBytes()
        {
            CompressionProvider.Decompress(CompressedPayload1);
        }

        [Benchmark]
        public async Task Decompress5KBytesAsync()
        {
            await CompressionProvider.DecompressAsync(CompressedPayload1);
        }

        [Benchmark]
        public void Decompress5KBytesFromStream()
        {
            CompressionProvider.DecompressStream(new MemoryStream(CompressedPayload1));
        }

        [Benchmark]
        public async Task Decompress5KBytesFromStreamAsync()
        {
            await CompressionProvider.DecompressStreamAsync(new MemoryStream(CompressedPayload1));
        }
    }
}
