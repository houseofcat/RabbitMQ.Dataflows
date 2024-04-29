using HouseofCat.Utilities.Helpers;
using OpenTelemetry.Trace;
using System.Collections.Generic;

namespace HouseofCat.Dataflows.Extensions;

public static class WorkStateExtensions
{
    public static void SetOpenTelemetryError(this IWorkState state, string message = null)
    {
        if (state.WorkflowSpan is null || !state.WorkflowSpan.IsRecording) return;
        state.SetSpanAsError(state.WorkflowSpan, message);
    }

    public static void SetCurrentActivityAsError(this IWorkState state, string message = null)
    {
        OpenTelemetryHelpers.SetCurrentActivityAsError(state?.EDI?.SourceException, message);
    }

    public static void SetCurrentSpanAsError(this IWorkState state, string message = null)
    {
        OpenTelemetryHelpers.SetCurrentSpanAsError(state?.EDI?.SourceException, message);
    }

    public static void SetSpanAsError(this IWorkState state, TelemetrySpan span, string message = null)
    {
        if (span is null) return;

        if (state?.EDI is not null)
        {
            span.SetStatus(Status.Error.WithDescription(message ?? state.EDI.SourceException?.Message));
            if (state.EDI.SourceException is not null)
            {
                span.RecordException(state.EDI.SourceException);
            }
        }
        else
        {
            span.SetStatus(Status.Error.WithDescription(message));
        }
    }

    public static string DefaultWorkflowNameKey { get; set; } = "hoc.workflow.name";
    public static string DefaultWorkflowStepIdKey { get; set; } = "hoc.workflow.step.id";

    /// <summary>
    /// Start a new RootSpan/ChildSpan with respect to WorkState.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="workflowName"></param>
    /// <param name="workflowVersion"></param>
    /// <param name="uniqueIdentifier"></param>
    /// <param name="spanKind"></param>
    /// <param name="spanAttributes"></param>
    public static void StartWorkflowSpan(
        this IWorkState state,
        string workflowName,
        SpanKind spanKind = SpanKind.Internal,
        IEnumerable<KeyValuePair<string, string>> suppliedAttributes = null,
        SpanContext? parentSpanContext = null)
    {
        if (state is null) return;

        var attributes = new SpanAttributes();
        attributes.Add(DefaultWorkflowNameKey, workflowName);

        if (suppliedAttributes is not null)
        {
            foreach (var kvp in suppliedAttributes)
            {
                attributes.Add(kvp.Key, kvp.Value);
            }
        }

        if (parentSpanContext.HasValue)
        {
            state.WorkflowSpan = OpenTelemetryHelpers
                .StartActiveSpan(
                    workflowName,
                    spanKind: spanKind,
                    parentSpanContext.Value,
                    attributes: attributes);
        }
        else
        {
            state.WorkflowSpan = OpenTelemetryHelpers
                .StartRootSpan(
                    workflowName,
                    spanKind,
                    attributes: attributes);
        }
    }

    /// <summary>
    /// Create a new active span with respect to State and potentially a parent RootSpan.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="spanName"></param>
    /// <param name="spanKind"></param>
    /// <param name="spanAttributes"></param>
    /// <returns></returns>
    public static TelemetrySpan CreateActiveSpan(
        this IWorkState state,
        string spanName,
        SpanKind spanKind = SpanKind.Internal,
        IEnumerable<KeyValuePair<string, string>> suppliedAttributes = null)
    {
        var attributes = new SpanAttributes();
        if (suppliedAttributes is not null)
        {
            foreach (var kvp in suppliedAttributes)
            {
                attributes.Add(kvp.Key, kvp.Value);
            }

            attributes.Add(DefaultWorkflowStepIdKey, spanName);
        }

        return OpenTelemetryHelpers
            .StartActiveSpan(
                spanName,
                spanKind,
                parentContext: state.WorkflowSpan?.Context ?? default,
                attributes: attributes);
    }

    /// <summary>
    /// Create a new active span with respect to state and span context provided.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="spanName"></param>
    /// <param name="spanKind"></param>
    /// <param name="spanAttributes"></param>
    /// <returns></returns>
    public static TelemetrySpan CreateActiveChildSpan(
        this IWorkState state,
        string spanName,
        SpanKind spanKind = SpanKind.Internal,
        SpanAttributes attributes = null)
    {
        return OpenTelemetryHelpers
            .StartActiveSpan(
                spanName,
                spanKind,
                state.WorkflowSpan.Context,
                attributes: attributes);
    }

    public static void EndStateSpan(
        this IWorkState state,
        bool includeErrorWhenFaulted = false)
    {
        if (state is null) return;

        if (includeErrorWhenFaulted && state.IsFaulted)
        {
            state.SetOpenTelemetryError();
        }
        state.WorkflowSpan.End();
        state.WorkflowSpan?.Dispose();
    }
}
