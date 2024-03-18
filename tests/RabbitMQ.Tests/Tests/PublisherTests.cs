using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ConnectivityTests.Tests;

public static class PublisherTests
{
    public static async Task RunSlowPublisherTestAsync(ILogger logger, string configFileNamePath, int testCount = 400, int delay = 100)
    {
        var channelPool = await Shared.SetupTestsAsync(logger, configFileNamePath);
        await StartPublisherAsync(logger, channelPool, testCount, delay);
    }

    // This method sends messages to the queue slowly to allow the connections to be closed (and then recovered).
    private static async Task StartPublisherAsync(ILogger logger, IChannelPool channelPool, int testCount, int delay)
    {
        await Task.Yield();

        var jsonProvider = new JsonProvider();
        var publisher = new Publisher(channelPool, jsonProvider);

        try
        {
            await publisher.StartAutoPublishAsync();
            var letterTemplate = new Letter("", Shared.QueueName, null, new LetterMetadata());

            for (var i = 0; i < testCount; i++)
            {
                var letter = letterTemplate.Clone();
                letter.MessageId = Guid.NewGuid().ToString();
                letter.Body = Encoding.UTF8.GetBytes($"Hello World! {i}");

                await publisher.QueueMessageAsync(letter);
                logger.LogInformation("Published message [Id: {MessageId}].", letter.MessageId);

                await Task.Delay(delay);
            }

            logger.LogInformation("Stopping publisher.");
            await publisher.StopAutoPublishAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred. Ex: {ex.Message}");
        }
    }
}
