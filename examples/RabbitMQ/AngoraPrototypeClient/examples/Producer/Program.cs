using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Angora.PrototypeClient;

namespace Producer
{
    internal class Program
    {
        private const int numberOfMessages = 1_000_000;

        private static async Task Main()
        {
            var factory = new ConnectionFactory
            {
                HostName = "rabbit"
            };

            var connection = await factory.CreateConnection("Producer").ConfigureAwait(false);

            var channel = await connection.CreateChannel().ConfigureAwait(false);

            await channel.Queue.Declare("test", false, true, false, false, null).ConfigureAwait(false);

            Console.WriteLine("Producer started. Press any key to send messages.");
            Console.ReadKey();

            for (int i = 0; i < numberOfMessages; i++)
            {
                var properties = new MessageProperties()
                {
                    ContentType = "message",
                    AppId = "123",
                    Timestamp = DateTime.UtcNow,
                    Headers = new Dictionary<string, object>
                    {
                        {"MessageId",  i}
                    }
                };

                await channel.Basic.Publish("", "test", true, properties, System.Text.Encoding.UTF8.GetBytes($"Message Payload {i}")).ConfigureAwait(false);
            }

            await channel.Close().ConfigureAwait(false);

            await connection.Close().ConfigureAwait(false);
        }
    }
}