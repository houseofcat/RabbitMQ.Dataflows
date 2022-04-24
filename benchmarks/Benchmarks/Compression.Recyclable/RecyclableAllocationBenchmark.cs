using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Compression;
using HouseofCat.Recyclable;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Benchmarks.Compression.Recyclable
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.Net60)]
    public class RecyclableAllocationBenchmark
    {
        private ICompressionProvider RecyclableProvider;

        private byte[] Payload1 { get; set; } = new byte[10_000];
        private byte[] CompressedPayload1 { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            Enumerable.Repeat<byte>(0xFF, 1000).ToArray().CopyTo(Payload1, 0);
            Enumerable.Repeat<byte>(0xAA, 1000).ToArray().CopyTo(Payload1, 1000);
            Enumerable.Repeat<byte>(0x1A, 1000).ToArray().CopyTo(Payload1, 2000);
            Enumerable.Repeat<byte>(0xAF, 1000).ToArray().CopyTo(Payload1, 3000);
            Enumerable.Repeat<byte>(0x01, 1000).ToArray().CopyTo(Payload1, 4000);

            RecyclableManager.ConfigureNewStaticManagerWithDefaults();
            RecyclableProvider = new RecyclableGzipProvider();
            CompressedPayload1 = RecyclableProvider.Compress(Payload1).ToArray();
        }

        [Benchmark(Baseline = true)]
        [Arguments(500)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void RecyclableGzipProvider_10KBytes(int x)
        {
            for (var i = 0; i < x; i++)
            {
                var compressedData = RecyclableProvider.Compress(Payload1);
                var decompressedData = RecyclableProvider.Decompress(compressedData);
            }
        }

        [Benchmark]
        [Arguments(500)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        public void RecyclableGzipProviderStream_10KBytes(int x)
        {
            for (var i = 0; i < x; i++)
            {
                var compressedStream = RecyclableProvider.CompressToStream(Payload1);
                using var decompressedStream = RecyclableProvider.Decompress(compressedStream, false);
            }
        }
    }
}
