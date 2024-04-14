using HouseofCat.Utilities.Extensions;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace HouseofCat.Utilities.Helpers;

public static class OpenTelemetryHelpers
{
    private static string _sourceName;
    private static string _sourceVersion;

    static OpenTelemetryHelpers()
    {
        SetEntryAssemblyAsSourceForTelemetry();
    }

    public static void SetEntryAssemblyAsSourceForTelemetry()
    {
        var assembly = Assembly.GetEntryAssembly();
        _sourceName = assembly.GetName().Name;
        _sourceVersion = assembly.GetSemanticVersion();
    }

    public static void SetAssemblyAsSourceForTelemetry(Assembly assembly)
    {
        _sourceName = assembly.GetName().Name;
        _sourceVersion = assembly.GetSemanticVersion();
    }

    public static TracerProvider CreateTraceProvider(
        string appName = null,
        string appVersion = null,
        string sourceName = null,
        bool addConsoleExporter = false)
    {
        var builder = Sdk
            .CreateTracerProviderBuilder()
            .SetResourceBuilder(
                ResourceBuilder
                .CreateDefault()
                .AddService(
                    appName ?? _sourceName,
                    serviceVersion: appVersion ?? _sourceVersion))
            .AddSource(sourceName ?? _sourceName);

        if (addConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        return builder.Build();
    }

    public static TracerProvider CreateTraceProvider(
        ResourceBuilder resourceBuilder,
        bool addConsoleExporter = false,
        params string[] sourceNames)
    {
        var builder = Sdk
            .CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource(sourceNames);

        if (addConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        return builder.Build();
    }

    #region Span Helpers

    public static TelemetrySpan StartRootSpan(
        string spanName,
        SpanKind spanKind = SpanKind.Internal,
        SpanAttributes attributes = null,
        IEnumerable<Link> links = null,
        DateTimeOffset startTime = default)
    {
        var provider = TracerProvider
            .Default
            .GetTracer(
                _sourceName,
                _sourceVersion);

        if (provider is null) return null;

        return provider.StartRootSpan(
            spanName,
            spanKind,
            initialAttributes: attributes,
            links: links,
            startTime: startTime);
    }

    /// <summary>
    /// Starts a new active span, automatically setting the parent ontext if there is a current span
    /// create a new active child span automatically.
    /// </summary>
    /// <param name="spanName"></param>
    /// <param name="spanKind"></param>
    /// <param name="parentContext"></param>
    /// <param name="attributes"></param>
    /// <param name="links"></param>
    /// <param name="startTime"></param>
    /// <returns></returns>
    public static TelemetrySpan StartActiveSpan(
        string spanName,
        SpanKind spanKind = SpanKind.Internal,
        SpanContext parentContext = default,
        SpanAttributes attributes = null,
        IEnumerable<Link> links = null,
        DateTimeOffset startTime = default)
    {
        var provider = TracerProvider
            .Default
            .GetTracer(
                _sourceName,
                _sourceVersion);

        if (provider is null) return null;

        if (parentContext == default)
        {
            var currentSpan = Tracer.CurrentSpan;
            if (currentSpan != null)
            {
                parentContext = currentSpan.Context;
            }
        }

        var span = provider.StartActiveSpan(
            spanName,
            spanKind,
            initialAttributes: attributes,
            parentContext: parentContext,
            links: links,
            startTime: startTime);

        return span;
    }

    #endregion

    #region Current and ParentId Helpers

    public static string GetCurrentActivityId()
    {
        return Activity.Current?.Id;
    }

    public static ActivitySpanId? GetCurrentActivitySpanId()
    {
        return Activity.Current?.SpanId;
    }

    public static void SetCurrentActivityParentId(string parentId)
    {
        Activity.Current?.SetParentId(parentId);
    }

    public static void SetCurrentActivityParentId(
        string traceId,
        string spanId,
        ActivityTraceFlags activityTraceFlags = ActivityTraceFlags.None)
    {
        Activity.Current?.SetParentId(
            ActivityTraceId.CreateFromString(traceId),
            ActivitySpanId.CreateFromString(spanId),
            activityTraceFlags);
    }

    public static void SetCurrentActivityParentId(
        ActivityTraceId traceId,
        ActivitySpanId spanId,
        ActivityTraceFlags activityTraceFlags = ActivityTraceFlags.None)
    {
        Activity.Current?.SetParentId(traceId, spanId, activityTraceFlags);
    }

    #endregion

    #region Header Helpers

    public static string GetOrCreateTraceHeaderFromCurrentActivity(
        ActivityTraceFlags flags = ActivityTraceFlags.None)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return FormatOpenTelemetryHeader(
                ActivityTraceId.CreateRandom().ToHexString(),
                ActivitySpanId.CreateRandom().ToHexString(),
                "00",
                flags);
        }

        return FormatOpenTelemetryHeader(
            activity.TraceId.ToHexString(),
            activity.SpanId.ToHexString(),
            "00",
            activity.ActivityTraceFlags);
    }

    public static string FormatOpenTelemetryHeader(
        string traceId,
        string spanId,
        string version = "00",
        ActivityTraceFlags activityTraceFlags = ActivityTraceFlags.None)
    {
        return $"{version}-{traceId}-{spanId}-{(int)activityTraceFlags:00}";
    }

    public static SpanContext? ExtractSpanContextFromTraceHeader(string traceHeader)
    {
        if (string.IsNullOrEmpty(traceHeader)) return default;

        var split = traceHeader.Split('-');
        if (split.Length < 3) return default;

        try
        {
            // With Version
            var activityTraceFlags = ActivityTraceFlags.None;
            ActivityTraceId activityTraceId;
            ActivitySpanId activitySpanId;
            if (split.Length == 4
                && split[0].Length == 2
                && split[1].Length == 32
                && split[2].Length == 16
                && split[3].Length == 2)
            {
                activityTraceId = ActivityTraceId.CreateFromString(split[1].AsSpan());
                activitySpanId = ActivitySpanId.CreateFromString(split[2].AsSpan());
                if (int.TryParse(split[3], out var flagsAsInt))
                {
                    activityTraceFlags = (ActivityTraceFlags)flagsAsInt;
                }
            }
            // Without Version Pre-Appended
            else if (split.Length == 3
                && split[0].Length == 32
                && split[1].Length == 16
                && split[2].Length == 2)
            {
                activityTraceId = ActivityTraceId.CreateFromString(split[0].AsSpan());
                activitySpanId = ActivitySpanId.CreateFromString(split[1].AsSpan());
                if (int.TryParse(split[2], out var flagsAsInt))
                {
                    activityTraceFlags = (ActivityTraceFlags)flagsAsInt;
                }
            }
            else
            {
                return default;
            }

            return new SpanContext(activityTraceId, activitySpanId, activityTraceFlags, isRemote: true);
        }
        catch
        {
            return default;
        }
    }

    #endregion

    #region Error Handling

    public static void SetCurrentActivityAsError(Exception ex, string message = null)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        SetActivityAsError(activity, ex, message);
    }

    public static void SetActivityAsError(Activity activity, Exception ex, string message = null)
    {
        if (activity is null) return;

        if (ex is not null)
        {
            activity.RecordException(ex);
        }

        activity.SetStatus(ActivityStatusCode.Error, message);
    }

    public static void SetCurrentSpanAsError(Exception ex, string message = null)
    {
        var span = Tracer.CurrentSpan;
        if (span is null) return;

        SetSpanAsError(span, ex, message);
    }

    public static void SetSpanAsError(TelemetrySpan span, Exception ex, string message = null)
    {
        if (span is null) return;

        if (ex is not null)
        {
            span.RecordException(ex);
        }

        span.SetStatus(Status.Error.WithDescription(message));
    }

    #endregion
}
