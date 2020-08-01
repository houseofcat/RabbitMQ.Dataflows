using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BufferBlockDemo
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

        // The BufferBlock does not do much but get messages and output those messages.
        // Why use it? Well it is good practice in Dataflow for the input and output of a workflow
        // to consist of a BufferBlock. It's primarily used for PubSub scenarios.
        private static async Task SimpleDemoAsync()
        {
            Console.WriteLine("BufferBlockDemo has started!");
            var block = new BufferBlock<string>();

            for (int i = 0; i < 10; i++)
            {
                block.Post(ProduceTimeData());
            }

            block.Complete();

            for (int i = 0; i < 10; i++)
            {
                var output = await block.ReceiveAsync().ConfigureAwait(false);
                Console.WriteLine(output);
            }

            await block.Completion.ConfigureAwait(false);

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }

        private static string ProduceTimeData()
        { return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff zzz}"; }
    }
}
