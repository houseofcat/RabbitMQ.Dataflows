using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BatchJoinBlockDemo
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args?.Length > 0)
            {
                switch (args[0])
                {
                    case "1": await SimpleDemoAsync().ConfigureAwait(false); break;
                }
            }
            else
            {
                await SimpleDemoAsync().ConfigureAwait(false);
            }
        }

        // BatchJoinBlocks allow you to optimize on message rate vs. message order.
        // MessageCount is what is batched from all the inputs.
        //
        // The output is difficult to discern but you could have:
        // An empty Batch1, an empty Batch2, or both empty!
        // A full Batch1, a full Batch2, or both full!
        // A full Batch1, a partial/empty Batch2!
        // A partial/empty Batch1, a full Batch2!
        // Both partially filled!
        //
        // This is a fast way to consume data, but you will have to add some boiler plate condtions
        // to your code to handle all the possible scenarios since it is unpredictable in
        // nature.
        private static async Task SimpleDemoAsync()
        {
            Console.WriteLine("BatchJoinBlockDemo has started!");
            var bufferExecutionOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 10 };
            var transformOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 5 }; // add parallelism back
            var options = new DataflowLinkOptions { PropagateCompletion = true };

            var bufferBlock = new BufferBlock<string>(bufferExecutionOptions);
            var broadCastBlock = new BroadcastBlock<string>(
                input => input);
            var transform1 = new TransformBlock<string, string>(
                (input) => $"TB1: {input}",
                transformOptions);
            var transform2 = new TransformBlock<string, string>(
                (input) => $"TB2: {input}",
                transformOptions);

            bufferBlock.LinkTo(broadCastBlock, options);
            broadCastBlock.LinkTo(transform1, options);
            broadCastBlock.LinkTo(transform2, options);

            var batchJoinBlock = new BatchedJoinBlock<string, string>(2);
            transform1.LinkTo(batchJoinBlock.Target1, options); // You have to stitch up where the executions are going in your join block.
            transform2.LinkTo(batchJoinBlock.Target2, options);

            var actionBlock = new ActionBlock<Tuple<IList<string>, IList<string>>>(
                (inputs) => Console.WriteLine($"Batch1: {string.Join(", ", inputs.Item1)}\r\nBatch2: {string.Join(", ", inputs.Item2)}\r\n-------"));
            batchJoinBlock.LinkTo(actionBlock, options);

            for (int i = 0; i < 20; i++)
            {
                await bufferBlock
                    .SendAsync(ProduceTimeData(i))
                    .ConfigureAwait(false);
            }

            bufferBlock.Complete();
            //await bufferBlock.Completion.ConfigureAwait(false);
            //await broadCastBlock.Completion.ConfigureAwait(false);
            //await transform1.Completion.ConfigureAwait(false);
            //await transform2.Completion.ConfigureAwait(false);

            // Because we are using PropagateCompletion = true and the last block is a single ActionBlock,
            // we will wait for all our marbles to reach the finish line with a single line.
            await actionBlock.Completion.ConfigureAwait(false);

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }

        private static string ProduceTimeData(int i)
        { return $"{i}: {DateTime.Now:HH:mm:ss.ffffff}"; }
    }
}
