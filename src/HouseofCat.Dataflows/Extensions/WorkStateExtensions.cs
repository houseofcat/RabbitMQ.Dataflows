using HouseofCat.Utilities.Extensions;
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

        if (state.RootSpan is null) return;
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

        span.SetStatus(Status.Error);
        span.SetAttribute("Error", message);
        if (state?.EDI is not null)
        {
            if (state.EDI.SourceException is not null)
            {
                span.RecordException(state.EDI.SourceException);
            }
        }
    }

    public static string DefaultRootSpanNameFormat { get; set; } = "{0}.root";
    public static string DefaultChildSpanNameFormat { get; set; } = "{0}.{1}.child";
    public static string DefaultWorkflowNameKey { get; set; } = "hoc.workflow.name";
    public static string DefaultWorkflowVersionKey { get; set; } = "hoc.workflow.version";

    /// <summary>
    /// Start a new RootSpan with respect to State.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="workflowName"></param>
    /// <param name="workflowVersion"></param>
    /// <param name="uniqueIdentifier"></param>
    /// <param name="spanKind"></param>
    /// <param name="spanAttributes"></param>
    public static void StartRootSpan(
        this IWorkState state,
        string workflowName,
        string workflowVersion = null,
        SpanKind spanKind = SpanKind.Internal,
        IEnumerable<KeyValuePair<string, string>> suppliedAttributes = null)
    {
        if (state is null) return;

        if (string.IsNullOrWhiteSpace(workflowVersion))
        {
            var assembly = Assembly.GetExecutingAssembly();
            workflowVersion = assembly.GetExecutingSemanticVersion();
        }

        var traceProvider = TracerProvider
            .Default
            .GetTracer(
                workflowName,
                workflowVersion);

        if (traceProvider is null) return;

        state.Data[DefaultWorkflowNameKey] = workflowName;
        state.Data[DefaultWorkflowVersionKey] = workflowVersion;

        var rootSpanName = string.Format(DefaultRootSpanNameFormat, workflowName);

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

        state.RootSpan = traceProvider
            .StartRootSpan(
                rootSpanName,
                spanKind,
                initialAttributes: attributes);
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
        SpanAttributes spanAttributes = null)
    {
        if (state?.Data is null) return null;

        state.Data.TryGetValue(DefaultWorkflowNameKey, out var workflowName);
        state.Data.TryGetValue(DefaultWorkflowVersionKey, out var workflowVersion);

        if (workflowName is null) return null;

        var traceProvider = TracerProvider
            .Default
            .GetTracer(
                workflowName.ToString(),
                workflowVersion?.ToString());

        if (traceProvider is null) return null;

        var childSpanName = string.Format(DefaultChildSpanNameFormat, workflowName, spanName);

        return traceProvider
            .StartActiveSpan(
                childSpanName,
                spanKind,
                parentContext: state.RootSpan?.Context ?? default,
                initialAttributes: spanAttributes);
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
        SpanAttributes spanAttributes = null)
    {
        if (state?.Data is null) return null;

        state.Data.TryGetValue(DefaultWorkflowNameKey, out var workflowName);
        state.Data.TryGetValue(DefaultWorkflowVersionKey, out var workflowVersion);

        var traceProvider = TracerProvider
            .Default
            .GetTracer(
                workflowName.ToString(),
                workflowVersion?.ToString());

        if (traceProvider is null) return null;

        var childSpanName = string.Format(DefaultChildSpanNameFormat, workflowName, spanName);

        return traceProvider
            .StartActiveSpan(
                childSpanName,
                spanKind,
                parentContext: spanContext,
                initialAttributes: spanAttributes);
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
        state.RootSpan?.Dispose();
    }
}
