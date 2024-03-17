using HouseofCat.Logger;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Pools;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

var loggerFactory = LogHelper.CreateConsoleLoggerFactory();
LogHelper.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

var rabbitOptionsJson = await File.ReadAllTextAsync("./RabbitMQ.Json");
Guard.AgainstNullOrEmpty(rabbitOptionsJson, nameof(rabbitOptionsJson));

var rabbitOptions = JsonSerializer.Deserialize<RabbitOptions>(rabbitOptionsJson);
var channelPool = new ChannelPool(rabbitOptions);

var channelHost = await channelPool.GetTransientChannelAsync(true);
var channel = channelHost.GetChannel();

var exchangeName = "TestExchange";
var queueName = "TestQueue";
var routingKey = "TestRoutingKey";

logger.LogInformation("Declaring Exchange: {exchangeName}", exchangeName);
channel.ExchangeDeclare(exchangeName, ExchangeType.Direct, true, false, null);

logger.LogInformation("Declaring Queue: {queueName}", queueName);
channel.QueueDeclare(queueName, true, false, false, null);

logger.LogInformation(
    "Binding Exchange {exchangeName} To Queue: {queueName}. RoutingKey: {routingKey}",
    exchangeName,
    queueName,
    routingKey);
channel.ExchangeBind(exchangeName, queueName, routingKey);

channel.BasicPublish(exchangeName, routingKey, null, Encoding.UTF8.GetBytes("Test Message"));

Console.ReadLine();