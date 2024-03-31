using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.ConsumerDataflows.Tests;
using System.Text;

var loggerFactory = LogHelpers.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelpers.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

var applicationName = "RabbitMQ.ConsumerDataflow.Tests";

using var traceProvider = Sdk
    .CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(applicationName))
    .AddSource(Shared.ConsumerWorkflowName)
    .AddConsoleExporter()
    .Build();

var rabbitService = await Shared.SetupRabbitServiceAsync(loggerFactory, "./RabbitMQ.RabbitServiceTests.json");
var dataflowService = new ConsumerDataflowService(rabbitService);

dataflowService.AddStep(
    "WriteToRabbitMessageToConsole",
    (state) =>
    {
        Console.WriteLine(Encoding.UTF8.GetString(state.ReceivedData.Data.Span));
        return state;
    });

dataflowService.AddFinalization(
    (state) =>
    {
        logger.LogInformation("Finalization Step!");
        state.ReceivedData.AckMessage();
        state.ReceivedData.Complete();
    });

dataflowService.AddErrorHandling(
    (state) =>
    {
        logger.LogError(state?.EDI?.SourceException, "Error Step!");
        state?.ReceivedData?.NackMessage(requeue: true);
        state?.ReceivedData?.Complete();
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
