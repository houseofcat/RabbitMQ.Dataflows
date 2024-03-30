using OpenTelemetry.Trace;
using System.Diagnostics;

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

    public static string OpenTelemetryDefaultProviderTraceSourceNameKey { get; set; } = "OpenTelemetryTraceSourceName";
    public static string OpenTelemetryDefaultProviderTracerServiceVersionKey { get; set; } = "OpenTelemetryTraceSourceVersion";

    private static readonly string _defaultProviderTracerSourceName = "HouseofCat.Dataflows";

    public static void SetWorkflowNameAsOpenTelemetrySourceName(
        this IWorkState state,
        string workflowName,
        string version = null)
    {
        state.Data[OpenTelemetryDefaultProviderTraceSourceNameKey] = workflowName ?? _defaultProviderTracerSourceName;
        state.Data[OpenTelemetryDefaultProviderTracerServiceVersionKey] = version;
    }

    public static void StartRootSpan(
        this IWorkState state,
        string spanName,
        SpanKind spanKind = SpanKind.Internal,
        SpanAttributes spanAttributes = null)
    {
        if (state.Data.TryGetValue(OpenTelemetryDefaultProviderTraceSourceNameKey, out var workflowName))
        {
            state.Data.TryGetValue(OpenTelemetryDefaultProviderTracerServiceVersionKey, out var version);

            state.RootSpan = TracerProvider
                .Default
                ?.GetTracer(
                    workflowName?.ToString() ?? _defaultProviderTracerSourceName,
                    version?.ToString())
                ?.StartRootSpan(spanName, spanKind, initialAttributes: spanAttributes);
        }
        else
        {
            state.RootSpan = TracerProvider
                .Default
                ?.GetTracer(_defaultProviderTracerSourceName, null)
                ?.StartRootSpan(spanName, spanKind, initialAttributes: spanAttributes);
        }
    }

    public static TelemetrySpan CreateActiveSpan(
        this IWorkState state,
        string spanName,
        SpanKind spanKind = SpanKind.Internal,
        SpanAttributes spanAttributes = null)
    {
        if (state.Data.TryGetValue(OpenTelemetryDefaultProviderTraceSourceNameKey, out var workflowName))
        {
            state.Data.TryGetValue(OpenTelemetryDefaultProviderTracerServiceVersionKey, out var version);

            return TracerProvider
                .Default
                ?.GetTracer(
                    workflowName?.ToString() ?? _defaultProviderTracerSourceName,
                    version?.ToString())
                ?.StartActiveSpan(
                    spanName,
                    spanKind,
                    parentContext: state.RootSpan?.Context ?? default,
                    initialAttributes: spanAttributes);
        }
        else
        {
            return TracerProvider
                .Default
                ?.GetTracer(_defaultProviderTracerSourceName, null)
                ?.StartActiveSpan(
                    spanName,
                    spanKind,
                    parentContext: state.RootSpan?.Context ?? default,
                    initialAttributes: spanAttributes);
        }
    }

    public static void EndRootSpan(
        this IWorkState state,
        bool includeErrorWhenFaulted = false)
    {
        if (includeErrorWhenFaulted && state.IsFaulted)
        {
            state.SetOpenTelemetryError();
        }
        state?.RootSpan?.End();
        state?.RootSpan?.Dispose();
    }
}
