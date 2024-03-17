using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BoundedPubSubBufferBlockDemo
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args?.Length > 0)
            {
                switch (args[0])
                {
                    case "5": await SimpleBufferedBroadcastBlockDemoAsync().ConfigureAwait(false); break;
                }
            }
            else
            {
                await SimpleBufferedBroadcastBlockDemoAsync().ConfigureAwait(false);
            }
        }

        // Same as the previous example but with a BufferBlock.
        //
        // By limiting the bounded capacity up front in the BufferBlock, you don't accidentally over
        // allocate down stream by leaving the BroadcastBlock and ActionBlock unbounded. This is a
        // common top level/ingestion throttling strategy.
        private static async Task SimpleBufferedBroadcastBlockDemoAsync()
        {
            Console.WriteLine("BoundedPubSubBufferBlockDemo has started!");
            var sharedExecutionOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 10 };

            var bufferBlock = new BufferBlock<string>(sharedExecutionOptions);
            var broadCastBlock = new BroadcastBlock<string>(
                input => input);
            var printBlock1 = new ActionBlock<string>(
                (input) => Console.WriteLine($"PrintBlock1: {input}"));
            var printBlock2 = new ActionBlock<string>(
                (input) => Console.WriteLine($"PrintBlock2: {input}"));

            bufferBlock.LinkTo(broadCastBlock, new DataflowLinkOptions { PropagateCompletion = true });
            broadCastBlock.LinkTo(printBlock1, new DataflowLinkOptions { PropagateCompletion = true });
            broadCastBlock.LinkTo(printBlock2, new DataflowLinkOptions { PropagateCompletion = true });

            for (int i = 0; i < 20; i++) // should produce 20 messages on each consumer print block, so 40 total
            {
                await bufferBlock
                    .SendAsync(ProduceTimeData(i))
                    .ConfigureAwait(false);
            }

            bufferBlock.Complete();
            await bufferBlock.Completion.ConfigureAwait(false);
            await broadCastBlock.Completion.ConfigureAwait(false);
            await printBlock1.Completion.ConfigureAwait(false);
            await printBlock2.Completion.ConfigureAwait(false);

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }

        private static string ProduceTimeData(int i)
        { return $"Message {i}: {DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff zzz}"; }
    }
}
