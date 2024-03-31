using HouseofCat.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace RabbitMQ.ConsoleTests;

public static class RabbitServiceTests
{
    // This test is about testing outages in both the Consumer and Publisher
    // with the shared ConnectionPool all stitched together by the helper RabbitService.
    public static async Task RunRabbitServicePingPongTestAsync(ILoggerFactory loggerFactory, string configFileNamePath)
    {
        var rabbitService = await Shared.SetupRabbitServiceAsync(loggerFactory, configFileNamePath);

        var consumer = rabbitService.GetConsumer(Shared.ConsumerName);
        await consumer.StartConsumerAsync();

        var dataAsBytes = rabbitService.SerializationProvider.Serialize(new { Name = "TestName", Age = 42 });
        var letter = new Message(
            exchange: Shared.ExchangeName,
            routingKey: Shared.RoutingKey,
            data: dataAsBytes,
            id: Guid.NewGuid().ToString());

        await rabbitService.Publisher.QueueMessageAsync(letter);

        // Ping pong the same message.
        await foreach (var receivedData in consumer.StreamUntilConsumerStopAsync())
        {
            if (receivedData?.Letter is null)
            {
                receivedData?.AckMessage();
                continue;
            }

            await rabbitService.DecomcryptAsync(receivedData.Letter);
            await rabbitService.Publisher.QueueMessageAsync(receivedData.Letter);
            receivedData.AckMessage();
        }
    }

    public static async Task RunRabbitServiceAltPingPongTestAsync(ILoggerFactory loggerFactory, string configFileNamePath)
    {
        var rabbitService = await Shared.SetupRabbitServiceAsync(loggerFactory, configFileNamePath);

        var consumer = rabbitService.GetConsumer(Shared.ConsumerName);
        await consumer.StartConsumerAsync();

        var dataAsBytes = rabbitService.SerializationProvider.Serialize(new { Name = "TestName", Age = 42 });
        var letter = new Message(
            exchange: Shared.ExchangeName,
            routingKey: Shared.RoutingKey,
            data: dataAsBytes,
            id: Guid.NewGuid().ToString());

        await rabbitService.Publisher.QueueMessageAsync(letter);

        // Ping pong the same message.
        await foreach (var receivedData in consumer.StreamUntilConsumerStopAsync())
        {
            if (receivedData?.Letter is null)
            {
                receivedData?.AckMessage();
                continue;
            }

            await rabbitService.DecomcryptAsync(receivedData.Letter);
            rabbitService.Publisher.QueueMessage(receivedData.Letter);
            receivedData.AckMessage();
        }
    }
}
