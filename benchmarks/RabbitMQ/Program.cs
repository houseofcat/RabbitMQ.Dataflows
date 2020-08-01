using BenchmarkDotNet.Running;

namespace Benchmarks.RabbitMQ
{
    public static class Program
    {
        public static void Main()
        {
            //_ = BenchmarkRunner.Run<ConnectionPoolBenchmark>();
            //_ = BenchmarkRunner.Run<ChannelPoolBenchmark>();
            //_ = BenchmarkRunner.Run<UtilsBenchmark>();
            _ = BenchmarkRunner.Run<EncryptBenchmark>();
        }
    }
}
