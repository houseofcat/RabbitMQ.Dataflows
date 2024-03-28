using System;
using System.Collections.Generic;

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
    IDisposable TrackAndDuration(string name, bool microScale = false, string unit = null, string description = null, IDictionary<string, string> metricTags = null);
    IDisposable Trace(string name, IDictionary<string, string> metricTags = null);
}
