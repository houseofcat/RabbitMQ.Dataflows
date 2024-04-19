using HouseofCat.Utilities.Extensions;
using HouseofCat.Utilities.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.ConsoleTests;

var loggerFactory = LogHelpers.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelpers.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

var builder = WebApplication.CreateBuilder(args);
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

builder.Services.AddOpenTelemetryExporter(configuration);

using var app = builder.Build();

logger.LogInformation("Tests complete! Press CTRL+C to gracefully exit....");

app.Lifetime.ApplicationStarted.Register(
    async () =>
    {
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
        await RabbitServiceTests.RunRabbitServicePingPongTestAsync(loggerFactory, "./RabbitMQ.RabbitServiceTests.json");
        //await RabbitServiceTests.RunRabbitServiceAltPingPongTestAsync(loggerFactory, "./RabbitMQ.RabbitServiceTests.json");
    });

await app.RunAsync();
