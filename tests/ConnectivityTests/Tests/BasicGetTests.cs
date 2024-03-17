using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;

namespace ConnectivityTests.Tests;

public static class BasicGetTests
{
    public static async Task RunBasicGetAsync(ILogger logger, string configFileNamePath)
    {
        var channelPool = await Shared.SetupTestsAsync(logger, configFileNamePath);
        var channelHost = await channelPool.GetTransientChannelAsync(true);
        var channel = channelHost.GetChannel();

        try
        {
            channel.BasicPublish(Shared.ExchangeName, Shared.RoutingKey, null, Encoding.UTF8.GetBytes("Hello World"));

            logger.LogInformation(
                "Getting message to from Queue [{queueName}]",
                Shared.QueueName);

            BasicGetResult result = null;
            do
            {
                result = channel.BasicGet(Shared.QueueName, true);
                if (result is not null && result.Body.Length > 0)
                {
                    var message = Encoding.UTF8.GetString(result.Body.Span);
                    logger.LogInformation("Received message: [{message}]", message);
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
