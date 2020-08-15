using HouseofCat.Utilities;
using System;

namespace HouseofCat.Metrics
{
    public interface IMetricsProvider
    {
        void DecrementCounter(string name, bool create);
        void DecrementGauge(string name, bool create);
        void IncrementCounter(string name, bool create);
        void IncrementGauge(string name, bool create);
        void ObserveValue(string name, double value, bool create);
        void ObserveValueFluctuation(string name, double value, bool create);
        IDisposable MeasureDuration(string name, bool create);
        IDisposable TrackConcurrency(string name, bool create);
        MultiDispose MeasureAndTrack(string name, bool create);
    }
}
