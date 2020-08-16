using HouseofCat.Utilities;
using System;

namespace HouseofCat.Metrics
{
    public interface IMetricsProvider
    {
        void DecrementCounter(string name, string description = null);
        void DecrementGauge(string name, string description = null);
        void IncrementCounter(string name, string description = null);
        void IncrementGauge(string name, string description = null);
        void ObserveValue(string name, double value, string description = null);
        void ObserveValueFluctuation(string name, double value, string description = null);
        IDisposable Duration(string name, bool microScale = false, string description = null);
        IDisposable Track(string name, string description = null);
        MultiDispose TrackAndDuration(string name, bool microScale = false, string description = null);
    }
}
