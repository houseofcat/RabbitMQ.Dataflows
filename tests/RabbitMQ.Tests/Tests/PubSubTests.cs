using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ConnectivityTests.Tests;

public static class PubSubTests
{
    public static async Task RunPubSubTestAsync(ILogger logger, string configFileNamePath, int testCount = 1_000)
    {
        var channelPool = await Shared.SetupTestsAsync(logger, configFileNamePath);
        var publisherTask = StartPublisherAsync(logger, channelPool, testCount);
        var consumerTask = StartConsumerAsync(logger, channelPool);

        await Task.WhenAll(publisherTask, consumerTask);
    }

    public static async Task StartPublisherAsync(ILogger logger, IChannelPool channelPool, int testCount)
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
            }

            var exitLetter = letterTemplate.Clone();
            exitLetter.MessageId = "exit";
            exitLetter.Body = Encoding.UTF8.GetBytes("exit");

            logger.LogInformation("Publishing exit message.");
            await publisher.PublishAsync(exitLetter, false);

            logger.LogInformation("Stopping publisher.");
            await publisher.StopAutoPublishAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred. Ex: {ex.Message}");
        }
    }

    public static async Task StartConsumerAsync(ILogger logger, IChannelPool channelPool)
    {
        await Task.Yield();

        var consumer = new Consumer(channelPool, Shared.ConsumerName);

        try
        {
            await consumer.StartConsumerAsync();

            await foreach (var receivedData in consumer.StreamOutUntilClosedAsync())
            {
                try
                {
                    var letter = JsonSerializer.Deserialize<Letter>(receivedData.Data);
                    var dataAsString = Encoding.UTF8.GetString(letter.Body);

                    if (dataAsString.StartsWith("exit"))
                    {
                        logger.LogInformation("Exit message received.");
                        receivedData.AckMessage();
                        break;
                    }
                    else
                    {
                        logger.LogInformation(
                            "Received message [Id: {MessageId}]: [{data}]",
                            receivedData.Letter.MessageId,
                            dataAsString);

                        receivedData.AckMessage();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error occurred reading message. Ex: {ex.Message}");
                }
            }

            logger.LogInformation("Stopping consumer.");
            await consumer.StopConsumerAsync(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred. Ex: {ex.Message}");
        }
    }
}
