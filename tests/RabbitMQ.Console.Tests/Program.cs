using RabbitMQ.Console.Tests;
using Microsoft.Extensions.Logging;
using HouseofCat.Utilities.Helpers;

var loggerFactory = LogHelpers.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelpers.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

// Basic Tests
//await BasicGetTests.RunBasicGetAsync(logger, "./RabbitMQ.BasicGetTests.json");

// Publisher Tests
//await PublisherTests.RunSlowPublisherTestAsync(logger, "./RabbitMQ.PublisherTests.json");
//await PublisherTests.RunAutoPublisherStandaloneAsync();

// Consumer Tests
//await ConsumerTests.RunConsumerTestAsync(logger, "./RabbitMQ.ConsumerTests.json");

// PubSub Tests
//await PubSubTests.RunPubSubTestAsync(logger, "./RabbitMQ.PubSubTests.json");
//await PubSubTests.RunPubSubCheckForDuplicateTestAsync(logger, "./RabbitMQ.PubSubTests.json");

// RabbitService Tests
//await RabbitServiceTests.RunRabbitServicePingPongTestAsync(loggerFactory, "./RabbitMQ.RabbitServiceTests.json");
await RabbitServiceTests.RunRabbitServiceAltPingPongTestAsync(loggerFactory, "./RabbitMQ.RabbitServiceTests.json");

logger.LogInformation("Tests complete! Press return to exit....");

Console.ReadLine();
