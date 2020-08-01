using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BatchBlockDemo
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
                }
            }
            else
            {
                await SimpleDemoAsync().ConfigureAwait(false);
            }
        }

        // A BatchBlock takes in multiple types of values and outputs those values joined together (amount specified in the constructor)
        private static async Task SimpleDemoAsync()
        {
            Console.WriteLine("BatchBlockDemo has started!");
            var block = new BatchBlock<int>(3); // batch messages of type int, into 3

            for (int i = 0; i < 10; i++)
            {
                block.Post(i);
            }

            block.Complete(); // No mo data.

            while (await block.OutputAvailableAsync().ConfigureAwait(false))
            {
                var output = await block.ReceiveAsync().ConfigureAwait(false);

                Console.WriteLine($"BatchBlock BatchOutput: {string.Join(",",  output)}");
                Console.WriteLine($"BatchBlock OutputCount: {block.OutputCount}");
            }

            // wait for completion.
            await block.Completion.ConfigureAwait(false);

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }
    }
}
