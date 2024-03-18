using HouseofCat.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace ConnectivityTests.Tests;

public static class RabbitServiceTests
{
    public static async Task RunRabbitServiceTestAsync(ILoggerFactory loggerFactory, string configFileNamePath)
    {
        var rabbitService = await Shared.SetupRabbitServiceAsync(loggerFactory, configFileNamePath);

        var consumer = rabbitService.GetConsumer(Shared.ConsumerName);
        await consumer.StartConsumerAsync();

        var myCustomObject = new { Name = "TestName", Age = 42 };
        var myCustomObjectBytes = rabbitService.SerializationProvider.Serialize(myCustomObject);
        var letter = new Letter(
            Shared.ExchangeName,
            Shared.RoutingKey,
            myCustomObjectBytes);

        await rabbitService.Publisher.QueueMessageAsync(letter);

        // Ping pong the same message.
        await foreach (var receivedData in consumer.StreamUntilConsumerStopAsync())
        {
            await rabbitService.DecomcryptAsync(receivedData.Letter);
            await rabbitService.Publisher.QueueMessageAsync(receivedData.Letter);
            receivedData.AckMessage();
        }
    }
}
