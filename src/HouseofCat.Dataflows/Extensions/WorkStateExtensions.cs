using HouseofCat.Utilities.Extensions;
using HouseofCat.Utilities.Helpers;
using OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace HouseofCat.Dataflows.Extensions;

public static class WorkStateExtensions
{
    public static void SetOpenTelemetryError(this IWorkState state, string message = null)
    {
        if (state is null) return;
        state.SetCurrentActivityAsError(message);

        if (state.WorkflowSpan is null) return;
        state.SetCurrentSpanAsError(message);
    }

    public static void SetCurrentActivityAsError(this IWorkState state, string message = null)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        activity.SetStatus(ActivityStatusCode.Error, message ?? state.EDI.SourceException?.Message);
        if (state?.EDI is not null)
        {
            if (state.EDI.SourceException is not null)
            {
                activity.RecordException(state.EDI.SourceException);
            }
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Error, message);
        }
    }

    public static void SetCurrentSpanAsError(this IWorkState state, string message = null)
    {
        var span = Tracer.CurrentSpan;
        state.SetSpanAsError(span, message);
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

    public static string DefaultSpanNameFormat { get; set; } = "{0}.workflow";
    public static string DefaultChildSpanNameFormat { get; set; } = "{0}.{1}.workflow.step";
    public static string DefaultWorkflowNameKey { get; set; } = "hoc.workflow.name";
    public static string DefaultWorkflowVersionKey { get; set; } = "hoc.workflow.version";

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
        string workflowVersion = null,
        SpanKind spanKind = SpanKind.Internal,
        IEnumerable<KeyValuePair<string, string>> suppliedAttributes = null,
        string traceHeader = null)
    {
        if (state is null) return;

        if (string.IsNullOrWhiteSpace(workflowVersion))
        {
            var assembly = Assembly.GetExecutingAssembly();
            workflowVersion = assembly.GetExecutingSemanticVersion();
        }

        state.Data[DefaultWorkflowNameKey] = workflowName;
        state.Data[DefaultWorkflowVersionKey] = workflowVersion;

        var spanName = string.Format(DefaultSpanNameFormat, workflowName);

        var attributes = new SpanAttributes();
        attributes.Add(DefaultWorkflowNameKey, workflowName);
        attributes.Add(DefaultWorkflowVersionKey, workflowVersion);

        if (suppliedAttributes is not null)
        {
            foreach (var kvp in suppliedAttributes)
            {
                attributes.Add(kvp.Key, kvp.Value);
            }
        }

        if (traceHeader is not null)
        {
            var spanContext = OpenTelemetryHelpers.ExtractSpanContext(traceHeader);
            if (spanContext.HasValue)
            {
                state.WorkflowSpan = OpenTelemetryHelpers
                    .StartActiveChildSpan(
                        workflowName?.ToString(),
                        spanName,
                        spanContext.Value,
                        workflowVersion?.ToString(),
                        spanKind: spanKind,
                        attributes: attributes);
            }
        }

        state.WorkflowSpan = OpenTelemetryHelpers
            .StartRootSpan(
                workflowName?.ToString(),
                spanName,
                workflowVersion?.ToString(),
                spanKind,
                attributes: attributes);
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
        if (state?.Data is null) return null;

        state.Data.TryGetValue(DefaultWorkflowNameKey, out var workflowName);
        state.Data.TryGetValue(DefaultWorkflowVersionKey, out var workflowVersion);

        if (workflowName is null) return null;

        var attributes = new SpanAttributes();

        if (suppliedAttributes is not null)
        {
            foreach (var kvp in suppliedAttributes)
            {
                attributes.Add(kvp.Key, kvp.Value);
            }
        }

        return OpenTelemetryHelpers
            .StartActiveSpan(
                workflowName?.ToString(),
                spanName,
                workflowVersion?.ToString(),
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
        SpanContext spanContext,
        SpanKind spanKind = SpanKind.Internal,
        SpanAttributes attributes = null)
    {
        if (state?.Data is null) return null;

        state.Data.TryGetValue(DefaultWorkflowNameKey, out var workflowName);
        state.Data.TryGetValue(DefaultWorkflowVersionKey, out var workflowVersion);

        var childSpanName = string.Format(DefaultChildSpanNameFormat, workflowName, spanName);

        return OpenTelemetryHelpers
            .StartActiveChildSpan(
                workflowName?.ToString(),
                childSpanName,
                spanContext,
                workflowVersion?.ToString(),
                spanKind,
                attributes: attributes);
    }

    public static void EndRootSpan(
        this IWorkState state,
        bool includeErrorWhenFaulted = false)
    {
        if (state is null) return;

        if (includeErrorWhenFaulted && state.IsFaulted)
        {
            state.SetOpenTelemetryError();
        }
        state.WorkflowSpan?.Dispose();
    }
}
