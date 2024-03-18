using ConnectivityTests.Tests;
using HouseofCat.Logger;
using Microsoft.Extensions.Logging;

var loggerFactory = LogHelper.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelper.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

//await BasicGetTests.RunBasicGetAsync(logger, "./RabbitMQ.BasicGetTests.json");
//await PublisherTests.RunSlowPublisherTestAsync(logger, "./RabbitMQ.PublisherTests.json");
//await ConsumerTests.RunConsumerTestAsync(logger, "./RabbitMQ.ConsumerTests.json");
//await PubSubTests.RunPubSubTestAsync(logger, "./RabbitMQ.PubSubTests.json");
//await PubSubTests.RunPubSubCheckForDuplicateTestAsync(logger, "./RabbitMQ.PubSubTests.json");
await RabbitServiceTests.RunRabbitServiceTestAsync(loggerFactory, "./RabbitMQ.RabbitServiceTests.json");

logger.LogInformation("Tests complete! Press return to exit....");

Console.ReadLine();
