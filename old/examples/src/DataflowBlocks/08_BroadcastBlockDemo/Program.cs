using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BroadcastBlockDemo
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args?.Length > 0)
            {
                switch (args[0])
                {
                    case "6": await SimpleBroadcastBlockDemoAsync().ConfigureAwait(false); break;
                }
            }
            else
            {
                await SimpleBroadcastBlockDemoAsync().ConfigureAwait(false);
            }
        }

        // Caution is needed here, for classes passed to broadcast blocks, they are passing by reference
        // meaning data race conditions/mutations are possible here. Since each consumer gets a copy
        // of the reference to the same object.
        //
        // By default, BroadcastBlock can lose messages as it does not exhibit the same "retry"
        // seen in BufferBlocks when consumers can't accept more data. The message is lost.
        // Here are a few solutions.
        // 1.) Remove the bounded capacities and make everything unbounded.
        // 2.) Put a buffer infront of BroadcastBlock.
        // 3.) Create a custom BroadcastBlock that retries rejected consumer messages.
        private static async Task SimpleBroadcastBlockDemoAsync()
        {
            Console.WriteLine("BroadcastBlockDemo has started!");
            var broadCastBlock = new BroadcastBlock<string>(
                input => input // a cloning function is required
                /*, new ExecutionDataflowBlockOptions { BoundedCapacity = 10 }*/);
            var printBlock1 = new ActionBlock<string>(
                (input) => Console.WriteLine($"PrintBlock1: {input}")
                /* new ExecutionDataflowBlockOptions { BoundedCapacity = 2 }*/);
            var printBlock2 = new ActionBlock<string>(
                (input) => Console.WriteLine($"PrintBlock2: {input}")
                /*, new ExecutionDataflowBlockOptions { BoundedCapacity = 2 }*/);

            broadCastBlock.LinkTo(printBlock1, new DataflowLinkOptions { PropagateCompletion = true });
            broadCastBlock.LinkTo(printBlock2, new DataflowLinkOptions { PropagateCompletion = true });

            for (int i = 0; i < 20; i++) // should produce 20 messages on each consumer print block, so 40 total
            {
                await broadCastBlock
                    .SendAsync(ProduceTimeData(i))
                    .ConfigureAwait(false);
            }

            broadCastBlock.Complete();
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
