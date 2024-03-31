using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;

namespace RabbitMQ.ConsoleTests;

public static class BasicGetTests
{
    public static async Task RunBasicGetAsync(ILogger logger, string configFileNamePath)
    {
        var channelPool = await Shared.SetupTestsAsync(logger, configFileNamePath);
        var channelHost = await channelPool.GetTransientChannelAsync(true);

        try
        {
            var properties = channelHost.Channel.CreateBasicProperties();
            properties.DeliveryMode = 2;

            var messageAsBytes = Encoding.UTF8.GetBytes("Hello World");

            channelHost.Channel.BasicPublish(Shared.ExchangeName, Shared.RoutingKey, properties, messageAsBytes);

            logger.LogInformation(
                "Getting message from Queue [{queueName}]",
                Shared.QueueName);

            BasicGetResult result = null;
            do
            {
                result = channelHost.Channel.BasicGet(Shared.QueueName, false);
                if (result is not null)
                {
                    var message = Encoding.UTF8.GetString(result.Body.Span);
                    logger.LogInformation("Received message: [{message}]", message);
                    channelHost.Channel.BasicAck(result.DeliveryTag, false);
                }
                else
                {
                    logger.LogInformation("No messages received.");
                }
            } while (result is not null);

            logger.LogInformation("[BasicGet] finished. Closing channel.");

            channelHost.Close();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred. Ex: {ex.Message}");
        }
    }
}
