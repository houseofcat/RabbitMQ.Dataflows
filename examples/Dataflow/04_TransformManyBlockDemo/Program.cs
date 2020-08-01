using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TransformManyBlockDemo
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args?.Length > 0)
            {
                switch (args[0])
                {
                    case "2": await SimpleDemoWithParallelismAsync().ConfigureAwait(false); break;
                }
            }
            else
            {
                await SimpleDemoWithParallelismAsync().ConfigureAwait(false);
            }
        }

        // TransformManyBlock is opposite to a BatchBlock. It takes a single input and creates many outputs.
        // We will also be demonstrating how to link to an ActionBlock to work on those outputs.
        // Also make note that the order is still ensured.
        private static async Task SimpleDemoWithParallelismAsync()
        {
            Console.WriteLine("TransformManyBlockDemo has started!");
            var transformManyBlock = new TransformManyBlock<int, string>(
                ProduceTimeData,
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 5 });

            var actionBlock = new ActionBlock<string>(Console.WriteLine);
            transformManyBlock.Post(20);

            // Connect the source block (Transform) to the target/action block (Action).
            transformManyBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });

            transformManyBlock.Complete();
            await transformManyBlock.Completion.ConfigureAwait(false);

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }

        private static IEnumerable<string> ProduceTimeData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return $"Input {i}:{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff zzz}";
            }
        }
    }
}
