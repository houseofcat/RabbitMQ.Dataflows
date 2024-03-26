using HouseofCat.Utilities;
using System;

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
    MultiDispose TrackAndDuration(string name, bool microScale = false, string unit = null, string description = null);
    MultiDispose Trace(string name, string unit = null, string description = null);
}
