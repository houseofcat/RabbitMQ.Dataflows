using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Gauge;
using App.Metrics.Histogram;
using App.Metrics.Meter;
using App.Metrics.Timer;
using System;
using System.Collections.Concurrent;

namespace HouseofCat.Metrics
{
    public class AppMetricsProvider : IMetricsProvider
    {
        private readonly IMetricsRoot _metrics;
        public ConcurrentDictionary<string, CounterOptions> Counters { get; } = new ConcurrentDictionary<string, CounterOptions>();
        public ConcurrentDictionary<string, GaugeOptions> Gauges { get; } = new ConcurrentDictionary<string, GaugeOptions>();
        public ConcurrentDictionary<string, HistogramOptions> Histograms { get; } = new ConcurrentDictionary<string, HistogramOptions>();
        public ConcurrentDictionary<string, MeterOptions> Meters { get; } = new ConcurrentDictionary<string, MeterOptions>();
        public ConcurrentDictionary<string, TimerOptions> Timers { get; } = new ConcurrentDictionary<string, TimerOptions>();

        public AppMetricsProvider(MetricsBuilder builder = null)
        {
            if (builder == null)
            { _metrics = AppMetrics.CreateDefaultBuilder().Build(); }
            else
            { _metrics = builder.Build(); }
        }

        #region IMetricsProvider Implementation

        public void DecrementCounter(string name)
        {

        }

        public void DecrementGauge(string name)
        {
            throw new NotImplementedException();
        }

        public void IncrementCounter(string name)
        {
            throw new NotImplementedException();
        }

        public void IncrementGauge(string name)
        {
            throw new NotImplementedException();
        }

        public void ObserveValue(string name, double value)
        {
            throw new NotImplementedException();
        }

        public void ObserveValueFluctuation(string name, double value)
        {
            throw new NotImplementedException();
        }

        public IDisposable MeasureDuration(string name)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
