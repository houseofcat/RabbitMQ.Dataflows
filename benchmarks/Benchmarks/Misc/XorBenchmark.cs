using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Utilities.Random;
using System;

namespace Benchmarks.Misc
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser, ThreadingDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.Net60)]
    public class XorBenchmark
    {
        public XorShift XorShift;

        [GlobalSetup]
        public void Setup()
        {
            XorShift = new XorShift(true);
        }

        [Benchmark(Baseline = true)]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(1_000)]
        [Arguments(10_00)]
        [Arguments(100_000)]
        public void CreateRandomByteArray(int x)
        {
            var rand = new Random();
            var buffer = new byte[x];
            rand.NextBytes(buffer);
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(1_000)]
        [Arguments(10_00)]
        [Arguments(100_000)]
        public void CreateXorRandomByteArray(int x)
        {
            var buffer = new byte[x];
            XorShift.FillBuffer(buffer, 0, x);
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(1_000)]
        [Arguments(10_00)]
        [Arguments(100_000)]
        public void CreateUnsafeXorRandomByteArray(int x)
        {
            var buffer = new byte[x];
            XorShift.UnsafeFillBuffer(buffer, 0, x);
        }
    }
}
