using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Utilities.Extensions;
using HouseofCat.Utilities.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.ConsumerDataflowService;
using System.Text;

var loggerFactory = LogHelpers.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelpers.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();
var logMessage = false;

var builder = WebApplication.CreateBuilder(args);
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

builder.Services.AddOpenTelemetryExporter(configuration);

using var app = builder.Build();

var rabbitService = await Shared.SetupRabbitServiceAsync(loggerFactory, "RabbitMQ.ConsumerDataflows.json");
var dataflowService = new ConsumerDataflowService<CustomWorkState>(rabbitService, "TestConsumer");

dataflowService.AddStep(
    "write_message_to_log",
    (state) =>
    {
        var message = Encoding.UTF8.GetString(state.ReceivedMessage.Body.Span);
        if (message == "throw")
        {
            throw new Exception("Throwing an exception!");
        }

        if (logMessage)
        { logger.LogInformation(message); }

        return state;
    });

dataflowService.AddStep(
    "create_new_secret_message",
    async (state) =>
    {
        var message = new Message
        {
            Exchange = "",
            RoutingKey = "TestQueue",
            Body = Encoding.UTF8.GetBytes("Secret Message"),
            Metadata = new Metadata
            {
                PayloadId = Guid.NewGuid().ToString(),
            },
            ParentSpanContext = state.WorkflowSpan?.Context,
        };

        await rabbitService.ComcryptAsync(message);

        state.SendMessage = message;
        return state;
    });

dataflowService.AddFinalization(
    (state) =>
    {
        if (logMessage)
        { logger.LogInformation("Finalization Step!"); }

        state.ReceivedMessage.AckMessage();
    });

dataflowService.AddErrorHandling(
    (state) =>
    {
        logger.LogError(state?.EDI?.SourceException, "Error Step!");
    });

await dataflowService.StartAsync();

app.Lifetime.ApplicationStarted.Register(
    () =>
    {
        logger.LogInformation("Listening for Messages! Press CTRL+C to initiate graceful shutdown and stop consumer...");
    });

app.Lifetime.ApplicationStopping.Register(
    async () =>
    {
        logger.LogInformation("RabbitService AutoPublish stopping...");

        await rabbitService.Publisher.StopAutoPublishAsync();

        logger.LogInformation("ConsumerDataflowService stopping...");

        await dataflowService.StopAsync();

        await rabbitService.ShutdownAsync(false);

        logger.LogInformation("All stopped! Press return to exit...");
    });

await app.RunAsync();
