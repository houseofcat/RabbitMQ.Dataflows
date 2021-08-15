using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TransformBlockDemo
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args?.Length > 0)
            {
                switch (args[0])
                {
                    case "1": await SimpleDemoWithDelayAsync().ConfigureAwait(false); break;
                    case "2": await SimpleDemoWithParallelismAsync().ConfigureAwait(false); break;
                }
            }
            else
            {
                //await SimpleDemoWithDelayAsync().ConfigureAwait(false);
                await SimpleDemoWithParallelismAsync().ConfigureAwait(false);
            }
        }

        private static async Task SimpleDemoWithDelayAsync()
        {
            Console.WriteLine("TransformBlockDemo has started!");
            var block = new TransformBlock<int, string>( // by default singlethreaded
                async (input) =>
                {
                    await Task.Delay(500).ConfigureAwait(false);
                    return input.ToString();
                });

            for (int i = 0; i < 10; i++)
            {
                block.Post(i);
                Console.WriteLine($"TransformBlock input queue count: {block.InputCount}");
            }

            block.Complete(); // No mo data.

            while (await block.OutputAvailableAsync().ConfigureAwait(false))
            {
                Console.WriteLine($"TransformBlock OutputCount: {block.InputCount}");
                var output = await block.ReceiveAsync().ConfigureAwait(false);

                Console.WriteLine($"TransformBlock TransformOutput: {output}");
                Console.WriteLine($"TransformBlock OutputCount: {block.OutputCount}"); // will always be 0, since receive data is a blocking action and this transformblock is single threaded
            }

            // wait for completion.
            await block.Completion.ConfigureAwait(false);

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }

        private static async Task SimpleDemoWithParallelismAsync()
        {
            Console.WriteLine("TransformBlockDemo has started!");
            var block = new TransformBlock<int, string>(
                async (input) =>
                {
                    await Task.Delay(500).ConfigureAwait(false);
                    return input.ToString();
                },
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4 }); // how to make the same code above parallel with adjusting options instead of code

            for (int i = 0; i < 10; i++)
            {
                block.Post(i);
                Console.WriteLine($"TransformBlock input queue count: {block.InputCount}");
            }

            block.Complete(); // No mo data.

            while (await block.OutputAvailableAsync().ConfigureAwait(false))
            {
                Console.WriteLine($"TransformBlock InputCount: {block.InputCount}");
                var output = await block.ReceiveAsync().ConfigureAwait(false);

                Console.WriteLine($"TransformBlock TransformOutput: {output}");
                Console.WriteLine($"TransformBlock OutputCount: {block.OutputCount}");
            }

            // wait for completion.
            await block.Completion.ConfigureAwait(false);

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }
    }
}
