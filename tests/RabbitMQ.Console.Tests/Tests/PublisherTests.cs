using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading.Channels;

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

    public static async Task RunAutoPublisherStandaloneAsync()
    {
        // Step 1: Create RabbitOptions
        var rabbitOptions = new RabbitOptions
        {
            FactoryOptions = new FactoryOptions
            {
                Uri = new Uri("amqp://guest:guest@localhost:5672"),
            },
            PoolOptions = new PoolOptions
            {
                ServiceName = "TestService",
                MaxConnections = 2,
                MaxChannels = 10,
                MaxAckableChannels = 0
            },
            PublisherOptions = new PublisherOptions
            {
                CreatePublishReceipts = true,
                LetterQueueBufferSize = 10_000,
                BehaviorWhenFull = BoundedChannelFullMode.Wait,
                Compress = false,
                Encrypt = false,
                WithHeaders = true,
                WaitForConfirmationTimeoutInMilliseconds = 500
            }
        };

        // Step 2: Instantiate the Publisher
        var publisher = new Publisher(rabbitOptions, new JsonProvider(), encryptionProvider: null, compressionProvider: null);

        try
        {
            // Step 3: Start Auto Publishing
            await publisher.StartAutoPublishAsync(
                async receipt =>
                {
                    await Console.Out.WriteLineAsync($"Publish Receipt Received: {receipt.MessageId}");
                });

            // Step 4: Create Message
            var data = Encoding.UTF8.GetBytes("Hello, RabbitMQ!");
            var message = new Letter(Shared.ExchangeName, Shared.RoutingKey, data, Guid.NewGuid().ToString())
            {
                // DeliveryId for tracking/routing through Publisher/Consumer.
                MessageId = Guid.NewGuid().ToString(),
            };

            // Step 5: Queue Message (async publish).
            await publisher.QueueMessageAsync(message);

            // Step 5: Stop Auto Publishing
            await publisher.StopAutoPublishAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
