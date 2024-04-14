using HouseofCat.Compression.Recyclable;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.RabbitMQ.Services.Extensions;
using HouseofCat.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace RabbitMQ.ConsumerDataflows.Tests;

public static class Shared
{
    public static readonly string ExchangeName = "TestExchange";
    public static readonly string QueueName = "TestQueue";
    public static readonly string RoutingKey = "TestRoutingKey";

    public static readonly string ConsumerWorkflowName = "TestConsumerWorkflow";
    public static readonly string ConsumerName = "TestConsumer";
    public static readonly string ErrorQueue = "TestQueue.Error";

    public static async Task<IChannelPool> SetupTestsAsync(ILogger logger, string configFileNamePath)
    {
        var rabbitOptions = await RabbitExtensions.GetRabbitOptionsFromJsonFileAsync(configFileNamePath);
        var channelPool = new ChannelPool(rabbitOptions);

        var channelHost = await channelPool.GetTransientChannelAsync(true);

        logger.LogInformation("Declaring Exchange: [{ExchangeName}]", ExchangeName);
        channelHost.Channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, true, false, null);

        logger.LogInformation("Declaring Queue: [{QueueName}]", QueueName);
        channelHost.Channel.QueueDeclare(QueueName, true, false, false, null);

        logger.LogInformation(
            "Binding Queue [{queueName}] To Exchange: [{exchangeName}]. RoutingKey: [{routingKey}]",
            QueueName,
            ExchangeName,
            RoutingKey);

        channelHost.Channel.QueueBind(QueueName, ExchangeName, RoutingKey);

        logger.LogInformation(
            "Publishing message to Exchange [{exchangeName}] with RoutingKey [{routingKey}]",
            ExchangeName,
            RoutingKey);

        channelHost.Close();

        return channelPool;
    }

    public static readonly string EncryptionPassword = "PasswordyPasswordPassword";
    public static readonly string EncryptionSalt = "SaltySaltSalt";
    public static readonly int KeySize = 32;

    public static async Task<IRabbitService> SetupRabbitServiceAsync(
        ILoggerFactory loggerFactory,
        string configFileNamePath)
    {
        var rabbitOptions = await RabbitExtensions.GetRabbitOptionsFromJsonFileAsync(configFileNamePath);
        var jsonProvider = new JsonProvider();

        var hashProvider = new ArgonHashingProvider();
        var aes256Key = hashProvider.GetHashKey(EncryptionPassword, EncryptionSalt, KeySize);
        var aes256Provider = new AesGcmEncryptionProvider(aes256Key);

        var gzipProvider = new RecyclableGzipProvider();

        var rabbitService = await rabbitOptions.BuildRabbitServiceAsync(
           jsonProvider,
           aes256Provider,
           gzipProvider,
           loggerFactory);

        await rabbitService.Publisher.StartAutoPublishAsync();

        return rabbitService;
    }
}
