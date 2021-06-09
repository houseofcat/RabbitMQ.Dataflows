using BenchmarkDotNet.Running;
using HouseofCat.Benchmarks.Compression;
using HouseofCat.Benchmarks.Encryption;

namespace HouseofCat.Benchmarks
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

            BenchmarkRunner.Run(
                new[]
                {
                    BenchmarkConverter.TypeToBenchmarks(typeof(GzipBenchmark)),
                    BenchmarkConverter.TypeToBenchmarks(typeof(BrotliBenchmark)),
                    BenchmarkConverter.TypeToBenchmarks(typeof(DeflateBenchmark)),
                    BenchmarkConverter.TypeToBenchmarks(typeof(LZ4PickleBenchmark)),
                    BenchmarkConverter.TypeToBenchmarks(typeof(LZ4StreamBenchmark))
                });
        }
    }
}
