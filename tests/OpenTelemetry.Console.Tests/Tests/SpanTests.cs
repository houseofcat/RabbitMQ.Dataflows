using HouseofCat.RabbitMQ.Dataflows;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using static HouseofCat.Dataflows.Extensions.WorkStateExtensions;

namespace OpenTelmetry.Tests;

public static class SpanTests
{
    public class CustomWorkState : RabbitWorkState
    {
        public CustomWorkState()
        {
            Data = new Dictionary<string, object>();
        }
    }

    public static void RunRootSpanTest(ILogger logger, string sourceName)
    {
        logger.LogInformation($"Starting {nameof(RunRootSpanTest)}...");

        var workstate = new CustomWorkState();

        workstate.SetWorkflowNameAsOpenTelemetrySourceName(sourceName, "v1.0.0");
        workstate.StartRootSpan("RunBasicSpanTest.Root", SpanKind.Internal);
        workstate.EndRootSpan();

        logger.LogInformation($"Finished {nameof(RunRootSpanTest)}.");
    }

    public static void RunRootSpanWithChildSpanTest(ILogger logger, string workflowName)
    {
        logger.LogInformation($"Starting {RunRootSpanWithChildSpanTest}...");

        var workstate = new CustomWorkState();

        workstate.SetWorkflowNameAsOpenTelemetrySourceName(workflowName, "v1.0.0");
        workstate.StartRootSpan("RunBasicSpanTest.Root", SpanKind.Internal);
        using (var span = workstate.CreateActiveSpan("RunBasicSpanTest.Child", SpanKind.Internal))
        {
            span.SetStatus(Status.Ok);
        }
        workstate.EndRootSpan();

        logger.LogInformation($"Finished {nameof(RunRootSpanWithChildSpanTest)}.");
    }

    public static void RunRootSpanWithChildSpanErrorTest(ILogger logger, string workflowName)
    {
        logger.LogInformation($"Starting {RunRootSpanWithChildSpanTest}...");

        var workstate = new CustomWorkState();

        workstate.SetWorkflowNameAsOpenTelemetrySourceName(workflowName, "v1.0.0");
        workstate.StartRootSpan("RunBasicSpanTest.Root", SpanKind.Internal);

        using (var span = workstate.CreateActiveSpan("RunBasicSpanTest.Child", SpanKind.Internal))
        {
            workstate.SetCurrentSpanAsError("Span had an error!");
        }

        workstate.EndRootSpan();

        logger.LogInformation($"Finished {nameof(RunRootSpanWithChildSpanTest)}.");
    }

    public static void RunRootSpanWithManyChildSpanFlatLevelTest(ILogger logger, string workflowName)
    {
        logger.LogInformation($"Starting {RunRootSpanWithChildSpanTest}...");

        var workstate = new CustomWorkState();

        workstate.SetWorkflowNameAsOpenTelemetrySourceName(workflowName, "v1.0.0");
        workstate.StartRootSpan("RunBasicSpanTest.Root", SpanKind.Internal);

        for (var i = 0; i < 10; i++)
        {
            using var span = workstate.CreateActiveSpan($"RunBasicSpanTest.Child.{i}", SpanKind.Internal);
            
            if (i == 9)
            {
                workstate.SetCurrentSpanAsError("Span 9 had an error!");
            }
            else
            {
                span.SetStatus(Status.Ok);
            }
        }

        workstate.EndRootSpan();

        logger.LogInformation($"Finished {nameof(RunRootSpanWithChildSpanTest)}.");
    }
}
