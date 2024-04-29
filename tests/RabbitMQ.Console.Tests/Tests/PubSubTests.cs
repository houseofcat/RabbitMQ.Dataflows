using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace RabbitMQ.ConsoleTests;

public static class PubSubTests
{
    public static async Task RunPubSubTestAsync(ILogger logger, string configFileNamePath, int testCount = 1_000)
    {
        var channelPool = await Shared.SetupTestsAsync(logger, configFileNamePath);
        var publisherTask = StartPublisherAsync(logger, channelPool, testCount);
        var consumerTask = StartConsumerAsync(logger, channelPool);

        await Task.WhenAll(publisherTask, consumerTask);
    }

    private static async Task StartPublisherAsync(ILogger logger, IChannelPool channelPool, int testCount)
    {
        await Task.Yield();

        var jsonProvider = new JsonProvider();
        var publisher = new Publisher(channelPool, jsonProvider);

        try
        {
            await publisher.StartAutoPublishAsync();
            var messageTemplate = new Message("", Shared.QueueName, null, new Metadata());

            for (var i = 0; i < testCount; i++)
            {
                var message = messageTemplate.Clone();
                message.MessageId = Guid.NewGuid().ToString();
                message.Body = Encoding.UTF8.GetBytes($"Hello World! {i}");

                await publisher.QueueMessageAsync(message);
                logger.LogInformation("Published message [Id: {MessageId}].", message.MessageId);
            }

            var exitMessage = messageTemplate.Clone();
            exitMessage.MessageId = "exit";
            exitMessage.Body = Encoding.UTF8.GetBytes("exit");

            logger.LogInformation("Publishing exit message.");
            await publisher.PublishAsync(exitMessage, false);

            logger.LogInformation("Stopping publisher.");
            await publisher.StopAutoPublishAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred. Ex: {ex.Message}");
        }
    }

    private static async Task StartConsumerAsync(ILogger logger, IChannelPool channelPool)
    {
        await Task.Yield();

        var consumer = new Consumer(channelPool, Shared.ConsumerName);

        try
        {
            await consumer.StartConsumerAsync();

            await foreach (var receivedMessage in await consumer.ReadUntilStopAsync())
            {
                try
                {
                    var message = JsonSerializer.Deserialize<Message>(receivedMessage.Body.Span);
                    var dataAsString = Encoding.UTF8.GetString(message.Body.Span);

                    if (dataAsString.StartsWith("exit"))
                    {
                        logger.LogInformation("Exit message received.");
                        receivedMessage.AckMessage();
                        break; // Can leave messages in the internal queue, but we'll just break here.
                    }
                    else
                    {
                        logger.LogInformation(
                            "Received message [Id: {MessageId}]: [{data}]",
                            receivedMessage.Message.MessageId,
                            dataAsString);

                        receivedMessage.AckMessage();
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

    public static async Task RunPubSubCheckForDuplicateTestAsync(ILogger logger, string configFileNamePath, int testCount = 200)
    {
        var channelPool = await Shared.SetupTestsAsync(logger, configFileNamePath);
        await SendCountersToQueueSlowlyAsync(logger, channelPool, testCount);
        await VerifyNoDuplicatesInQueueAsync(logger, channelPool, testCount);
    }

    // This method sends messages to the queue slowly to allow the connections to be closed (and then recovered). The goal
    // here is to see no duplicate messages from the retry AutoPublisher mechanism.
    private static async Task SendCountersToQueueSlowlyAsync(ILogger logger, IChannelPool channelPool, int testCount, int delay = 100)
    {
        await Task.Yield();

        var jsonProvider = new JsonProvider();
        var publisher = new Publisher(channelPool, jsonProvider);

        try
        {
            await publisher.StartAutoPublishAsync();
            var messageTemplate = new Message("", Shared.QueueName, null, new Metadata());

            for (var i = 0; i < testCount; i++)
            {
                var message = messageTemplate.Clone();
                message.MessageId = Guid.NewGuid().ToString();
                message.Body = Encoding.UTF8.GetBytes(i.ToString());
                await publisher.QueueMessageAsync(message);

                await Task.Delay(delay);
            }

            await publisher.StopAutoPublishAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred. Ex: {ex.Message}");
        }
    }

    private static async Task VerifyNoDuplicatesInQueueAsync(ILogger logger, IChannelPool channelPool, int testCount)
    {
        await Task.Yield();

        var consumer = new Consumer(channelPool, Shared.ConsumerName);
        var hashSet = new HashSet<string>();

        try
        {
            await consumer.StartConsumerAsync();

            await foreach (var receivedMessage in await consumer.ReadUntilStopAsync())
            {
                var message = JsonSerializer.Deserialize<Message>(receivedMessage.Body.Span);
                var number = Encoding.UTF8.GetString(message.Body.Span);

                if (!hashSet.Add(number))
                {
                    logger.LogError("Duplicate message found in queue: [{number}]", number);
                    break;
                }

                receivedMessage.AckMessage();

                if (hashSet.Count == testCount)
                {
                    logger.LogInformation("Counted {testCount} messages. No duplicates.", testCount);
                    break;
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
