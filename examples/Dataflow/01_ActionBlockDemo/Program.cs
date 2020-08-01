using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ActionBlockDemo
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
                    case "1": await SimpleDemoWithDelayAsync().ConfigureAwait(false); break;
                }
            }
            else
            {
                //await SimpleDemoAsync().ConfigureAwait(false);
                await SimpleDemoWithDelayAsync().ConfigureAwait(false);
            }
        }

        private static async Task SimpleDemoAsync()
        {
            Console.WriteLine("ActionBlockDemo has started!");
            var block = new ActionBlock<int>(Console.WriteLine);

            for (int i = 0; i < 10; i++)
            {
                block.Post(i);
            }

            block.Complete();
            await block.Completion.ConfigureAwait(false);

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }

        private static async Task SimpleDemoWithDelayAsync()
        {
            Console.WriteLine("ActionBlockDemo has started!");
            var block = new ActionBlock<int>(
                async (input) =>
                {
                    Console.WriteLine(input);
                    await Task.Delay(500).ConfigureAwait(false);
                });

            for (int i = 0; i < 10; i++)
            {
                block.Post(i);
                Console.WriteLine($"ActionBlock input queue count: {block.InputCount}");
            }

            block.Complete();
            await block.Completion.ConfigureAwait(false);

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }
    }
}
