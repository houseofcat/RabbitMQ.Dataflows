using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HouseofCat.Metrics;

public interface IMetricsProvider
{
    void DecrementCounter(string name, string unit = null, string description = null);
    void DecrementGauge(string name, string unit = null, string description = null);
    void IncrementCounter(string name, string unit = null, string description = null);
    void IncrementGauge(string name, string unit = null, string description = null);
    void ObserveValue(string name, double value, string unit = null, string description = null);
    void ObserveValueFluctuation(string name, double value, string unit = null, string description = null);

    IDisposable Duration(string name, bool microScale = false, string unit = null, string description = null);

    IDisposable Track(string name, string unit = null, string description = null);

    IDisposable TrackAndDuration(
        string name,
        bool microScale = false,
        string unit = null,
        string description = null,
        ActivityKind activityKind = ActivityKind.Internal,
        IDictionary<string, string> metricTags = null);

    IDisposable Trace(
        string name,
        ActivityKind activityKind = ActivityKind.Internal,
        IDictionary<string, string> metricTags = null);

    /// <summary>
    /// Creates a new active span.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="spanKind"></param>
    /// <param name="metricTags"></param>
    /// <returns></returns>
    IDisposable GetSpan(
        string name,
        SpanKind spanKind = SpanKind.Internal,
        IDictionary<string, string> metricTags = null);

    /// <summary>
    /// Attempts to create a new active child span from CURRENT active span or just a new active span if no current span.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="spanKind"></param>
    /// <param name="metricTags"></param>
    /// <returns></returns>
    IDisposable GetChildSpan(
        string name,
        SpanKind spanKind = SpanKind.Internal,
        IDictionary<string, string> metricTags = null);

    /// <summary>
    /// Creates a new active child span from provided span context.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="spanKind"></param>
    /// <param name="metricTags"></param>
    /// <returns></returns>
    IDisposable GetChildSpan(
        string name,
        SpanKind spanKind = SpanKind.Internal,
        SpanContext parentSpanContext = default);
}
