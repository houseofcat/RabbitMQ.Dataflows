using System;
using System.Threading.Tasks;
using RabbitMQ.Core.Prototype;

namespace Consumer
{
    internal class Program
    {
        private static async Task Main()
        {
            var factory = new ConnectionFactory
            {
                HostName = "rabbit"
            };

            var connection = await factory.CreateConnection("Consumer").ConfigureAwait(false);

            var channel = await connection.CreateChannel().ConfigureAwait(false);

            await channel.Queue.Declare("test", false, true, false, false, null).ConfigureAwait(false);

            await channel.Basic.Qos(0, 1000, false).ConfigureAwait(false);

            var consumer = new MesssageConsumer(channel.Basic);
            var consumerTag = await channel.Basic.Consume("test", "Consumer", false, false, null, consumer.HandleIncomingMessage).ConfigureAwait(false);

            Console.WriteLine("Consumer started. Press any key to quit.");
            Console.ReadKey();

            await channel.Basic.Cancel(consumerTag).ConfigureAwait(false);

            await channel.Close().ConfigureAwait(false);

            await connection.Close().ConfigureAwait(false);
        }
    }

    internal class MesssageConsumer
    {
        private readonly Basic basic;

        public MesssageConsumer(Basic basic)
        {
            this.basic = basic;
        }

        public Task HandleIncomingMessage(Basic.DeliverState messageState)
        {
            return basic.Ack(messageState.DeliveryTag, false);
        }
    }
}