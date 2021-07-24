using BenchmarkDotNet.Running;
using Benchmarks.Compression;
using Benchmarks.Compression.Recyclable;

namespace Benchmarks
{
    public static class Program
    {
        public static void Main()
        {
            //_ = BenchmarkRunner.Run<ConnectionPoolBenchmark>();
            //_ = BenchmarkRunner.Run<ChannelPoolBenchmark>();
            //_ = BenchmarkRunner.Run<UtilsBenchmark>();
            //_ = BenchmarkRunner.Run<EncryptBenchmark>();
            //_ = BenchmarkRunner.Run<BouncyEncryptBenchmark>();

            //BenchmarkRunner.Run(
            //    new[]
            //    {
            //        BenchmarkConverter.TypeToBenchmarks(typeof(EncryptBenchmark)),
            //        BenchmarkConverter.TypeToBenchmarks(typeof(BouncyEncryptBenchmark))
            //    });

            //BenchmarkRunner.Run(
            //    new[]
            //    {
            //        BenchmarkConverter.TypeToBenchmarks(typeof(GzipBenchmark)),
            //        BenchmarkConverter.TypeToBenchmarks(typeof(RecyclableGzipBenchmark)),
            //        BenchmarkConverter.TypeToBenchmarks(typeof(BrotliBenchmark)),
            //        BenchmarkConverter.TypeToBenchmarks(typeof(RecyclableBrotliBenchmark)),
            //        BenchmarkConverter.TypeToBenchmarks(typeof(DeflateBenchmark)),
            //        BenchmarkConverter.TypeToBenchmarks(typeof(RecyclableDeflateBenchmark)),
            //        BenchmarkConverter.TypeToBenchmarks(typeof(LZ4PickleBenchmark)),
            //        BenchmarkConverter.TypeToBenchmarks(typeof(LZ4StreamBenchmark))
            //    });

            BenchmarkRunner.Run(
                new[]
                {
                    BenchmarkConverter.TypeToBenchmarks(typeof(RecyclableAllocationBenchmark)),
                });

            //BenchmarkRunner.Run(
            //    new[]
            //    {
            //        BenchmarkConverter.TypeToBenchmarks(typeof(DataTransformerBenchmark)),
            //        BenchmarkConverter.TypeToBenchmarks(typeof(BouncyDataTransformBenchmark)),
            //    });

            //BenchmarkRunner.Run(
            //    new[]
            //    {
            //        BenchmarkConverter.TypeToBenchmarks(typeof(MedianOfTwoSortedArraysBenchmark)),
            //    });

            //BenchmarkRunner.Run(
            //    new[]
            //    {
            //        BenchmarkConverter.TypeToBenchmarks(typeof(IsNumericBenchmark)),
            //    });
        }
    }
}
