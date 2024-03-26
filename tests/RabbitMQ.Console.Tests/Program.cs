using ConnectivityTests.Tests;
using HouseofCat.Utilities;
using Microsoft.Extensions.Logging;

var loggerFactory = LogHelper.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelper.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

// Basic Tests
//await BasicGetTests.RunBasicGetAsync(logger, "./RabbitMQ.BasicGetTests.json");

// Publisher Tests
//await PublisherTests.RunSlowPublisherTestAsync(logger, "./RabbitMQ.PublisherTests.json");
await PublisherTests.RunAutoPublisherStandaloneAsync();

// Consumer Tests
//await ConsumerTests.RunConsumerTestAsync(logger, "./RabbitMQ.ConsumerTests.json");

// PubSub Tests
//await PubSubTests.RunPubSubTestAsync(logger, "./RabbitMQ.PubSubTests.json");
//await PubSubTests.RunPubSubCheckForDuplicateTestAsync(logger, "./RabbitMQ.PubSubTests.json");

// RabbitService Tests
//await RabbitServiceTests.RunRabbitServicePingPongTestAsync(loggerFactory, "./RabbitMQ.RabbitServiceTests.json");

logger.LogInformation("Tests complete! Press return to exit....");

Console.ReadLine();
