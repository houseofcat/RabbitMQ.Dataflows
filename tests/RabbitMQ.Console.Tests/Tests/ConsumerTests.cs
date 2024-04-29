using HouseofCat.RabbitMQ;
using Microsoft.Extensions.Logging;
using System.Text;

namespace RabbitMQ.ConsoleTests;

public static class ConsumerTests
{
    public static async Task RunConsumerTestAsync(ILogger logger, string configFileNamePath)
    {
        var channelPool = await Shared.SetupTestsAsync(logger, configFileNamePath);
        var consumer = new Consumer(channelPool, Shared.ConsumerName);

        try
        {
            await consumer.StartConsumerAsync();

            await foreach (var receivedMessage in await consumer.ReadUntilStopAsync())
            {
                logger.LogInformation("Received message: [{message}]", Encoding.UTF8.GetString(receivedMessage.Body.Span));
                receivedMessage.AckMessage();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred. Ex: {ex.Message}");
        }
    }
}
