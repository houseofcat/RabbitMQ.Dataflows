using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ConnectivityTests.Tests;

public static class Shared
{
    public static readonly string ExchangeName = "TestExchange";
    public static readonly string QueueName = "TestQueue";
    public static readonly string RoutingKey = "TestRoutingKey";
    public static readonly string ConsumerName = "TestConsumer";

    public static async Task<IChannelPool> SetupTestsAsync(ILogger logger, string configFileNamePath)
    {
        var rabbitOptions = await RabbitExtensions.GetRabbitOptionsFromJsonFileAsync(configFileNamePath);
        var channelPool = new ChannelPool(rabbitOptions);

        var channelHost = await channelPool.GetTransientChannelAsync(true);
        var channel = channelHost.GetChannel();

        logger.LogInformation("Declaring Exchange: [{ExchangeName}]", ExchangeName);
        channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, true, false, null);

        logger.LogInformation("Declaring Queue: [{QueueName}]", QueueName);
        channel.QueueDeclare(QueueName, true, false, false, null);

        logger.LogInformation(
            "Binding Queue [{queueName}] To Exchange: [{exchangeName}]. RoutingKey: [{routingKey}]",
            QueueName,
            ExchangeName,
            RoutingKey);

        channel.QueueBind(QueueName, ExchangeName, RoutingKey);

        logger.LogInformation(
            "Publishing message to Exchange [{exchangeName}] with RoutingKey [{routingKey}]",
            ExchangeName,
            RoutingKey);

        channelHost.Close();

        return channelPool;
    }
}
