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
                    case "5": await SimpleMultipleConsumerWithBlockingDemoAsync().ConfigureAwait(false); break;
                }
            }
            else
            {
                await SimpleMultipleConsumerWithBlockingDemoAsync().ConfigureAwait(false);
            }
        }

        // We adjust the following example but introduce a bounded condition for our
        // buffer block. This prevents out of control memory usage and creates an
        // asynchronous blocking scenario.
        //
        // So we can speed up processing by load balancing and throttle on memory allocations - again
        // just adjusting the construction options.
        private static async Task SimpleMultipleConsumerWithBlockingDemoAsync()
        {
            Console.WriteLine("PubSubBufferBlockDemo has started!");
            var bufferBlock = new BufferBlock<string>(new ExecutionDataflowBlockOptions { BoundedCapacity = 10 });
            var printBlock1 = new ActionBlock<string>(
                (input) => Console.WriteLine($"PrintBlock1: {input}"),
                new ExecutionDataflowBlockOptions { BoundedCapacity = 2 });
            var printBlock2 = new ActionBlock<string>(
                (input) => Console.WriteLine($"PrintBlock2: {input}"),
                new ExecutionDataflowBlockOptions { BoundedCapacity = 2 });

            bufferBlock.LinkTo(printBlock1, new DataflowLinkOptions { PropagateCompletion = true });
            bufferBlock.LinkTo(printBlock2, new DataflowLinkOptions { PropagateCompletion = true });

            for (int i = 0; i < 20; i++) // size is greater than buffer, so if we use Post(), we would return false for 10 of these.
            {
                //bufferBlock.Post(ProduceTimeData());
                await bufferBlock
                    .SendAsync(ProduceTimeData())
                    .ConfigureAwait(false);
            }

            bufferBlock.Complete();
            await bufferBlock.Completion.ConfigureAwait(false);
            await printBlock1.Completion.ConfigureAwait(false);
            await printBlock2.Completion.ConfigureAwait(false);

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }

        private static string ProduceTimeData()
        { return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff zzz}"; }
    }
}
