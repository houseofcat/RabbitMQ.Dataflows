using System;

namespace HouseofCat.Metrics
{
    public interface IMetricsProvider
    {
        void DecrementCounter(string name);
        void DecrementGauge(string name);
        void IncrementCounter(string name);
        void IncrementGauge(string name);
        void ObserveValue(string name, double value);
        void ObserveValueFluctuation(string name, double value);
        IDisposable MeasureDuration(string name);
    }
}
