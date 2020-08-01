using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Angora.PrototypeClient;

namespace Sample
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting...");
            var factory = new ConnectionFactory
            {
                UserName = "guest",
                Password = "guest",
                HostName = "localhost",
            };

            var connection = await factory.CreateConnection("test connection").ConfigureAwait(false);
            Console.WriteLine("Connection Created");

            var channel = await connection.CreateChannel().ConfigureAwait(false);

            Console.WriteLine("Channel Created");

            await DemoGeneralUsage(channel).ConfigureAwait(false);
            await PublishAndConsume(channel).ConfigureAwait(false);

            await channel.Close().ConfigureAwait(false);

            await connection.Close().ConfigureAwait(false);
        }

        private static async Task DemoGeneralUsage(Channel channel)
        {
            var arguments = new Dictionary<string, object>
            {
                { "x-queue-mode", "lazy" },
                { "x-message-ttl", 3000 }
            };

            var test1Result = await channel.Queue.Declare("test1", false, true, false, false, arguments).ConfigureAwait(false);
            var test2Result = await channel.Queue.Declare("test2", false, true, false, false, null).ConfigureAwait(false);
            var test3Result = await channel.Queue.Declare("test3", false, true, false, false, null).ConfigureAwait(false);
            var generatedResult = await channel.Queue.Declare("", false, true, true, false, null).ConfigureAwait(false);

            await channel.Exchange.Declare("test1", "fanout", false, true, false, false, null).ConfigureAwait(false);
            await channel.Exchange.Declare("test2", "fanout", false, true, false, false, null).ConfigureAwait(false);
            await channel.Exchange.Declare("test3", "direct", false, true, false, false, null).ConfigureAwait(false);

            await channel.Exchange.Bind("test1", "test3", "key", arguments).ConfigureAwait(false);
            await channel.Exchange.Unbind("test1", "test3", "key", arguments).ConfigureAwait(false);

            await channel.Exchange.Declare("test-internal", "fanout", false, true, false, true, null).ConfigureAwait(false);

            await channel.Queue.Bind("test1", "test1", "", null).ConfigureAwait(false);
            await channel.Queue.Bind("test3", "test3", "foo", null).ConfigureAwait(false);

            await channel.Queue.Bind("test2", "test1", "foo", null).ConfigureAwait(false);
            await channel.Queue.Unbind("test2", "test1", "foo", null).ConfigureAwait(false);

            var purgeCount = await channel.Queue.Purge("test2").ConfigureAwait(false);
            var deleteCount = await channel.Queue.Delete("test2", true, true).ConfigureAwait(false);

            await channel.Exchange.Delete("test2", false).ConfigureAwait(false);

            await channel.Basic.Qos(0, 100, false).ConfigureAwait(false);

            await channel.Basic.Recover().ConfigureAwait(false);

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }

        private static async Task PublishAndConsume(Channel channel)
        {
            await channel.Queue.Declare("test", false, true, false, false, null).ConfigureAwait(false);

            var properties = new MessageProperties()
            {
                ContentType = "message",
                AppId = "123",
                Timestamp = DateTime.UtcNow,
                Headers = new Dictionary<string, object>
                {
                    {"MessageId",  Guid.NewGuid().ToString()}
                }
            };

            await channel.Basic.Publish("", "test", true, properties, System.Text.Encoding.UTF8.GetBytes("Message Payload")).ConfigureAwait(false);

            Console.WriteLine("Press any key consume messages");
            Console.ReadKey();

            await channel.Basic.Qos(0, 1, false).ConfigureAwait(false);

            var consumerTag = await channel.Basic.Consume("test", "myConsumer", true, false, null, message => Task.CompletedTask).ConfigureAwait(false);

            Console.WriteLine("Press any key to quit");
            Console.ReadKey();

            await channel.Basic.Cancel(consumerTag).ConfigureAwait(false);
        }
    }
}
