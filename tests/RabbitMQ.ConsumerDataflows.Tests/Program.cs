using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using RabbitMQ.ConsumerDataflows.Tests;
using System.Text;

var loggerFactory = LogHelpers.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelpers.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

using var traceProvider = OpenTelemetryHelpers.CreateTraceProvider(addConsoleExporter: true);

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

logger.LogInformation("Listening for Messages! Press Return to stop consumer...");

Console.ReadLine();

logger.LogInformation("ConsumerService stopping...");

await dataflowService.StopAsync();

logger.LogInformation("RabbitMQ AutoPublish stopping...");

await rabbitService.Publisher.StopAutoPublishAsync();

logger.LogInformation("All stopped! Press return to exit...");

Console.ReadLine();
