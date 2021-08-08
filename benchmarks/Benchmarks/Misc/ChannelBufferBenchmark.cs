using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Benchmarks.Misc
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.NetCoreApp31)]
    public class ChannelBufferBenchmark
    {
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();
        private readonly BufferBlock<int> _bufferBlock = new BufferBlock<int>();

        private readonly Channel<int> _boundedChannel = Channel.CreateBounded<int>(1000);
        private readonly BufferBlock<int> _boundedBlock = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 1000 });

        [Benchmark(Baseline = true)]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        [Arguments(100_000)]
        [Arguments(1_000_000)]
        public async Task Channel_ReadThenWrite(int x)
        {
            ChannelWriter<int> writer = _channel.Writer;
            ChannelReader<int> reader = _channel.Reader;
            for (int i = 0; i < x; i++)
            {
                ValueTask<int> vt = reader.ReadAsync();
                writer.TryWrite(i);
                await vt;
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        [Arguments(100_000)]
        [Arguments(1_000_000)]
        public async Task BufferBlock_ReadThenWrite(int x)
        {
            for (int i = 0; i < x; i++)
            {
                Task<int> t = _bufferBlock.ReceiveAsync();
                _bufferBlock.Post(i);
                await t;
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        [Arguments(100_000)]
        [Arguments(1_000_000)]
        public async Task BoundedChannel_ReadThenWrite(int x)
        {
            ChannelWriter<int> writer = _channel.Writer;
            ChannelReader<int> reader = _channel.Reader;
            for (int i = 0; i < x; i++)
            {
                ValueTask<int> vt = reader.ReadAsync();
                await writer.WriteAsync(i);
                await vt;
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1_000)]
        [Arguments(10_000)]
        [Arguments(100_000)]
        [Arguments(1_000_000)]
        public async Task BoundedBufferBlock_ReadThenWrite(int x)
        {
            for (int i = 0; i < x; i++)
            {
                Task<int> t = _bufferBlock.ReceiveAsync();
                await Task.WhenAll(_bufferBlock.SendAsync(i), t);
            }
        }
    }
}
