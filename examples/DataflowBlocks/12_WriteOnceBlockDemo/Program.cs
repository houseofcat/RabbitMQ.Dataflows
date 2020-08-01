using System;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace WriteOneBlockDemo
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
                    case "7": await WriteBlockBehaviorDemoAsync().ConfigureAwait(false); break;
                }
            }
            else
            {
                //await WriteBlockBehaviorDemoAsync().ConfigureAwait(false);
                await SimpleDemoAsync().ConfigureAwait(false);
            }
        }

        // This demonstrates the behavior of the WriteOnceBlock as it's easy to observe.
        //
        // Preface: I hate the name of this block. It's misleading. This should have been
        // called CloneBlock or CacheBlock. WriteOnceBlock sounds like you use the block once
        // and it then self-destructs.

        // How it works is simple, it will accept one published message and reject all other messages
        // coming to it. It will then take the first message and clone it and send it on every request
        // downstream.
        private static async Task WriteBlockBehaviorDemoAsync()
        {
            Console.WriteLine("WriteOneBlockDemo has started!");
            var block = new WriteOnceBlock<string>(input => input); // needs a clone function

            for (int i = 0; i < 5; i++)
            {
                if (block.Post(ProduceTimeData()))
                {
                    Console.WriteLine($"Message {i} was accepted");
                }
                else
                {
                    Console.WriteLine($"Message {i} was rejected");
                }
            }

            // Notice the count is much higher than input count and that I am not
            // waiting on it to signal no more data is coming, as it always has data.
            for (int i = 0; i < 15; i++)
            {
                var output = await block.ReceiveAsync().ConfigureAwait(false);
                Console.WriteLine($"ReceivedMessage {i}: {output}");
            }

            block.Complete();
            await block.Completion.ConfigureAwait(false);

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }

        // Demo of it being used with other blocks.
        //
        // Only one push message propagates to the ActionBlock so only one message is received.
        private static async Task SimpleDemoAsync()
        {
            Console.WriteLine("WriteOneBlockDemo has started!");
            var options = new DataflowLinkOptions { PropagateCompletion = true };
            var writeBlock = new WriteOnceBlock<string>(input => input);
            var actionBlock = new ActionBlock<string>(Console.WriteLine);
            writeBlock.LinkTo(actionBlock, options);

            for (int i = 0; i < 5; i++)
            {
                await writeBlock.SendAsync(ProduceTimeData()).ConfigureAwait(false);
            }

            writeBlock.Complete();
            await actionBlock.Completion.ConfigureAwait(false);

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }

        private static string ProduceTimeData()
        { return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff zzz}"; }
    }
}
