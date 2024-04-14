using HouseofCat.Utilities.Extensions;
using HouseofCat.Utilities.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.ConsumerDataflows.Tests;
using System.Text;

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

var rabbitService = await Shared.SetupRabbitServiceAsync(loggerFactory, "./RabbitMQ.RabbitServiceTests.json");
var dataflowService = new ConsumerDataflowService(rabbitService);

dataflowService.AddStep(
    "WriteToRabbitMessageToConsole",
    (state) =>
    {
        Console.WriteLine(Encoding.UTF8.GetString(state.ReceivedMessage.Body.Span));
        return state;
    });

dataflowService.AddFinalization(
    (state) =>
    {
        logger.LogInformation("Finalization Step!");
        state.ReceivedMessage.AckMessage();
        state.ReceivedMessage.Complete();
    });

dataflowService.AddErrorHandling(
    (state) =>
    {
        logger.LogError(state?.EDI?.SourceException, "Error Step!");
        state?.ReceivedMessage?.NackMessage(requeue: true);
        state?.ReceivedMessage?.Complete();
    });

await dataflowService.StartAsync();

logger.LogInformation("Listening for Messages! Press CTRL+C to initiate graceful shutdown and stop consumer...");

app.Lifetime.ApplicationStopping.Register(
    async () =>
    {
        logger.LogInformation("ConsumerService stopping...");

        await dataflowService.StopAsync();

        logger.LogInformation("RabbitMQ AutoPublish stopping...");

        await rabbitService.Publisher.StopAutoPublishAsync();

        logger.LogInformation("All stopped! Press return to exit...");
    });

await app.RunAsync();