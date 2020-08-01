using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PubSubBufferBlockDemo
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args?.Length > 0)
            {
                switch (args[0])
                {
                    case "0": await SimpleDemoAsync().ConfigureAwait(false); break;
                    case "4": await SimpleMultipleConsumerDemoAsync().ConfigureAwait(false); break;
                }
            }
            else
            {
                //await SimpleDemoAsync().ConfigureAwait(false);
                await SimpleMultipleConsumerDemoAsync().ConfigureAwait(false);
            }
        }

        // This demonstrates a BufferBlock being published to.
        // Then demonstrates multiple consumers of that BufferBlock, but only consumer1 gets the data.
        // The reason for this is that the first consumer had the capacity to take in all the messages.
        // The producer block will not receive a reject for the first consumer, thus using the second consumer.
        private static async Task SimpleDemoAsync()
        {
            Console.WriteLine("PubSubBufferBlockDemo has started!");
            var bufferBlock = new BufferBlock<string>();
            var printBlock1 = new ActionBlock<string>(
                (input) => Console.WriteLine($"PrintBlock1: {input}"));
            var printBlock2 = new ActionBlock<string>(
                (input) => Console.WriteLine($"PrintBlock2: {input}"));

            bufferBlock.LinkTo(printBlock1, new DataflowLinkOptions { PropagateCompletion = true }); // signal completion down the chain
            bufferBlock.LinkTo(printBlock2, new DataflowLinkOptions { PropagateCompletion = true }); // signal completion down the chain

            for (int i = 0; i < 10; i++)
            {
                bufferBlock.Post(ProduceTimeData());
            }

            bufferBlock.Complete(); // signal we are finished (no more data will be coming)
            await bufferBlock.Completion.ConfigureAwait(false); // wait for queue to drain
            await printBlock1.Completion.ConfigureAwait(false); // wait for all the work to finish
            await printBlock2.Completion.ConfigureAwait(false); // wait for all the work to finish

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }

        // This demonstrates multiple consumers getting work by limiting the capacity of each
        // consumer so that the producer block will trigger rejection and allow it to reach
        // for another consumer.
        //
        // If no consumers are ready to consume yet, then the whole processes waits in place by default.
        private static async Task SimpleMultipleConsumerDemoAsync()
        {
            Console.WriteLine("PubSubBufferBlockDemo has started!");
            var bufferBlock = new BufferBlock<string>();
            var printBlock1 = new ActionBlock<string>(
                (input) => Console.WriteLine($"PrintBlock1: {input}"),
                new ExecutionDataflowBlockOptions { BoundedCapacity = 2 }); // How many units of work can I have, when full move to other consumers
            var printBlock2 = new ActionBlock<string>(
                (input) => Console.WriteLine($"PrintBlock2: {input}"),
                new ExecutionDataflowBlockOptions { BoundedCapacity = 2 });

            bufferBlock.LinkTo(printBlock1, new DataflowLinkOptions { PropagateCompletion = true });
            bufferBlock.LinkTo(printBlock2, new DataflowLinkOptions { PropagateCompletion = true });

            for (int i = 0; i < 20; i++)
            {
                bufferBlock.Post(ProduceTimeData());
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
