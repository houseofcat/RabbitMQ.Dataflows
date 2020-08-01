using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace JoinBlockDemo
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

        // JoinBlocks allow two execution paths join together. For that, we will convert our
        // action blocks from the previous example into TransformBlock so that they can return values.
        //
        // Visualize This Workflow
        //                                   -> Transform1 ->
        // ForLoop -> Buffer -> Broadcast ->                  -> JoinBlock -> Action (Print)
        //                                   -> Transform2 ->
        //
        // Notice: One final thing to note here is that message order is preserved.
        private static async Task SimpleDemoAsync()
        {
            Console.WriteLine("JoinBlockDemo has started!");
            var bufferExecutionOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 10 };
            var transformOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 5 }; // add parallelism back
            var options = new DataflowLinkOptions { PropagateCompletion = true };

            var bufferBlock = new BufferBlock<string>(bufferExecutionOptions);
            var broadCastBlock = new BroadcastBlock<string>(
                input => input);
            var transform1 = new TransformBlock<string, string>(
                (input) => $"TransformBlock1: {input}",
                transformOptions);
            var transform2 = new TransformBlock<string, string>(
                (input) => $"TransformBlock2: {input}",
                transformOptions);

            bufferBlock.LinkTo(broadCastBlock, options);
            broadCastBlock.LinkTo(transform1, options);
            broadCastBlock.LinkTo(transform2, options);

            var joinBlock = new JoinBlock<string, string>();
            transform1.LinkTo(joinBlock.Target1, options); // You have to stitch up where the executions are going in your join block.
            transform2.LinkTo(joinBlock.Target2, options);

            var actionBlock = new ActionBlock<Tuple<string, string>>(Console.WriteLine);
            joinBlock.LinkTo(actionBlock, options);

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
        { return $"Message {i}: {DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff zzz}"; }
    }
}
