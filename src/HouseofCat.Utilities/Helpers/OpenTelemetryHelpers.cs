using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HouseofCat.Utilities.Helpers;

public static class OpenTelemetryHelpers
{
    public static string CreateOpenTelemetryHeader(
        string traceId,
        string spanId,
        string version = "00",
        ActivityTraceFlags activityTraceFlags = ActivityTraceFlags.None)
    {
        return $"{version}-{traceId}-{spanId}-{(int)activityTraceFlags:00}";
    }

    public static string CreateOpenTelemetryHeaderFromCurrentActivityOrDefault(
        ActivityTraceFlags flags = ActivityTraceFlags.None)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return CreateOpenTelemetryHeader(
                ActivityTraceId.CreateRandom().ToHexString(),
                ActivitySpanId.CreateRandom().ToHexString(),
                "00",
                flags);
        }

        return CreateOpenTelemetryHeader(
            activity.TraceId.ToHexString(),
            activity.SpanId.ToHexString(),
            "00",
            activity.ActivityTraceFlags);
    }

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

    public static SpanContext? ExtractSpanContext(string traceHeader)
    {
        if (string.IsNullOrEmpty(traceHeader)) return default;

        var split = traceHeader.Split('-');

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

    public static void SetCurrentActivityParentIdFromTraceHeader(string traceheader)
    {
        if (string.IsNullOrEmpty(traceheader)) return;

        var activityTraceFlags = ActivityTraceFlags.None;
        var split = traceheader.Split('-');

        // With Version
        ActivityTraceId activityTraceId;
        ActivitySpanId activitySpanId;
        if (split.Length == 4 && split[3].Length == 2)
        {
            activityTraceId = ActivityTraceId.CreateFromString(split[1].AsSpan());
            activitySpanId = ActivitySpanId.CreateFromString(split[2].AsSpan());
            if (int.TryParse(split[3], out var flagsAsInt))
            {
                activityTraceFlags = (ActivityTraceFlags)flagsAsInt;
            }
        }
        // Without Version Pre-Appended
        else if (split.Length == 3 && split[0].Length > 2)
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
            return;
        }

        Activity.Current?.SetParentId(
            activityTraceId,
            activitySpanId,
            activityTraceFlags);
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

    public static TelemetrySpan StartRootSpan(
        string sourceName,
        string spanName,
        string sourceVersion = null,
        SpanKind spanKind = SpanKind.Internal,
        SpanAttributes attributes = null,
        IEnumerable<Link> links = null,
        DateTimeOffset startTime = default)
    {
        var provider = TracerProvider
            .Default
            .GetTracer(
                sourceName,
                sourceVersion);

        if (provider is null) return null;

        return provider.StartRootSpan(
            spanName,
            spanKind,
            initialAttributes: attributes,
            links: links,
            startTime: startTime);
    }

    public static TelemetrySpan StartActiveSpan(
        string sourceName,
        string spanName,
        string sourceVersion = null,
        SpanKind spanKind = SpanKind.Internal,
        SpanContext parentContext = default,
        SpanAttributes attributes = null,
        IEnumerable<Link> links = null,
        DateTimeOffset startTime = default)
    {
        var provider = TracerProvider
            .Default
            .GetTracer(
                sourceName,
                sourceVersion);

        if (provider is null) return null;

        return provider.StartActiveSpan(
            spanName,
            spanKind,
            initialAttributes: attributes,
            parentContext: parentContext,
            links: links,
            startTime: startTime);
    }

    public static TelemetrySpan StartActiveChildSpan(
        string sourceName,
        string spanName,
        SpanContext parentContext,
        string sourceVersion = null,
        SpanKind spanKind = SpanKind.Internal,
        SpanAttributes attributes = null)
    {
        var provider = TracerProvider
            .Default
            .GetTracer(
                sourceName,
                sourceVersion);

        if (provider is null) return null;

        return provider.StartActiveSpan(
            spanName,
            spanKind,
            initialAttributes: attributes,
            parentContext: parentContext);
    }
}
