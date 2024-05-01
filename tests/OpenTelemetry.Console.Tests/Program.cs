using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Tests;
using OpenTelemetry.Trace;

var loggerFactory = LogHelpers.CreateConsoleLoggerFactory(LogLevel.Information);
LogHelpers.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

var applicationName = "OpenTelemetry.ConsoleTests";
var workflowName = "MyWorkflowName";

using var traceProvider = Sdk
    .CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(applicationName))
    .AddSource(workflowName)
    .AddConsoleExporter()
    .Build();

WorkStateTests.RunRootSpanWithChildSpanErrorTest(logger, workflowName);

logger.LogInformation("Tests complete! Press return to exit....");

Console.ReadLine();