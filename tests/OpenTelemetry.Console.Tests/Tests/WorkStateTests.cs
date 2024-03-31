using HouseofCat.RabbitMQ.Dataflows;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using static HouseofCat.Dataflows.Extensions.WorkStateExtensions;

namespace OpenTelemetry.Tests;

public static class WorkStateTests
{
    public class CustomWorkState : RabbitWorkState
    {
        public CustomWorkState()
        {
            Data = new Dictionary<string, object>();
        }
    }

    public static void RunRootSpanTest(ILogger logger, string workflowName)
    {
        logger.LogInformation($"Starting {nameof(RunRootSpanTest)}...");

        var workstate = new CustomWorkState();

        workstate.StartWorkflowSpan(workflowName, spanKind: SpanKind.Internal);
        workstate.EndRootSpan();

        logger.LogInformation($"Finished {nameof(RunRootSpanTest)}.");
    }

    public static void RunRootSpanWithChildSpanTest(ILogger logger, string workflowName)
    {
        logger.LogInformation($"Starting {nameof(RunRootSpanWithChildSpanTest)}...");

        var workstate = new CustomWorkState();

        workstate.StartWorkflowSpan(workflowName, spanKind: SpanKind.Internal);
        using (var span = workstate.CreateActiveSpan("ChildStep", spanKind: SpanKind.Internal))
        {
            span.SetStatus(Status.Ok);
        }
        workstate.EndRootSpan();

        logger.LogInformation($"Finished {nameof(RunRootSpanWithChildSpanTest)}.");
    }

    public static void RunRootSpanWithChildSpanErrorTest(ILogger logger, string workflowName)
    {
        logger.LogInformation($"Starting {nameof(RunRootSpanWithChildSpanTest)}...");

        var workstate = new CustomWorkState();

        workstate.StartWorkflowSpan(workflowName, spanKind: SpanKind.Internal);

        using (var span = workstate.CreateActiveSpan("ChildStep", spanKind: SpanKind.Internal))
        {
            workstate.SetCurrentSpanAsError("Span had an error!");
        }

        workstate.EndRootSpan();

        logger.LogInformation($"Finished {nameof(RunRootSpanWithChildSpanTest)}.");
    }

    public static void RunRootSpanWithManyChildSpanFlatLevelTest(ILogger logger, string workflowName)
    {
        logger.LogInformation($"Starting {nameof(RunRootSpanWithChildSpanTest)}...");

        var workstate = new CustomWorkState();

        workstate.StartWorkflowSpan(workflowName, spanKind: SpanKind.Internal);

        for (var i = 0; i < 10; i++)
        {
            using var span = workstate.CreateActiveSpan($"ChildStep.{i}", SpanKind.Internal);
            
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
