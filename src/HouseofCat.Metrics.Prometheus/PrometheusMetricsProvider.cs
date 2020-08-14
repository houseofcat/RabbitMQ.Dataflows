using Prometheus;
using Prometheus.DotNetRuntime;
using System;
using System.Collections.Concurrent;

namespace HouseofCat.Metrics
{
    public class PrometheusMetricsProvider : IMetricsProvider, IDisposable
    {
        private readonly IMetricServer _server;
        private IDisposable _collector;
        private bool _disposedValue;

        public ConcurrentDictionary<string, Counter> Counters { get; } = new ConcurrentDictionary<string, Counter>();
        public ConcurrentDictionary<string, Gauge> Gauges { get; } = new ConcurrentDictionary<string, Gauge>();
        public ConcurrentDictionary<string, Histogram> Histograms { get; } = new ConcurrentDictionary<string, Histogram>();
        public ConcurrentDictionary<string, Summary> Summaries { get; } = new ConcurrentDictionary<string, Summary>();

        public PrometheusMetricsProvider(int port, string url, CollectorRegistry registry = null, bool useHttps = false)
        {
            _server = new MetricServer(port, url, registry, useHttps);
            _server.Start();
        }

        public PrometheusMetricsProvider(string hostname, int port, string url, CollectorRegistry registry = null, bool useHttps = false)
        {
            _server = new MetricServer(hostname, port, url, registry, useHttps);
            _server.Start();
        }

        public PrometheusMetricsProvider AddDotNetRuntimeStats(SampleEvery contentionSampleRate, SampleEvery jitSampleRate, SampleEvery threadScheduleSampleRate)
        {
            if (_collector == null)
            {
                _collector = DotNetRuntimeStatsBuilder
                    .Customize()
                    .WithContentionStats(contentionSampleRate)
                    .WithJitStats(jitSampleRate)
                    .WithThreadPoolSchedulingStats(null, threadScheduleSampleRate)
                    .WithThreadPoolStats()
                    .WithGcStats()
                    .StartCollecting();
            }

            return this;
        }

        public PrometheusMetricsProvider AddDotNetRuntimeStats(DotNetRuntimeStatsBuilder.Builder builder)
        {
            if (_collector == null)
            {
                _collector = builder.StartCollecting();
            }

            return this;
        }

        public PrometheusMetricsProvider AddCounter(string name, string description, CounterConfiguration config = null)
        {
            if (Counters.ContainsKey(name)) throw new ArgumentException(Constants.CounterAlreadyExists);

            Counters[name] = Prometheus.Metrics.CreateCounter(name, description, config);
            return this;
        }

        public PrometheusMetricsProvider AddGauge(string name, string description, GaugeConfiguration config = null)
        {
            if (Gauges.ContainsKey(name)) throw new ArgumentException(Constants.GaugeAlreadyExists);

            Gauges[name] = Prometheus.Metrics.CreateGauge(name, description, config);
            return this;
        }

        public PrometheusMetricsProvider AddHistogram(string name, string description, HistogramConfiguration config = null)
        {
            if (Histograms.ContainsKey(name)) throw new ArgumentException(Constants.HistogramAlreadyExists);

            Histograms[name] = Prometheus.Metrics.CreateHistogram(name, description, config);
            return this;
        }

        public PrometheusMetricsProvider AddSummary(string name, string description, SummaryConfiguration config = null)
        {
            if (Summaries.ContainsKey(name)) throw new ArgumentException(Constants.SummaryAlreadyExists);

            Summaries[name] = Prometheus.Metrics.CreateSummary(name, description, config);
            return this;
        }

        public PrometheusMetricsProvider AddTimer(string name, string description, HistogramConfiguration config = null)
        {
            if (Histograms.ContainsKey(name)) throw new ArgumentException(Constants.HistogramAlreadyExists);

            Histograms[name] = Prometheus.Metrics.CreateHistogram(name, description, config);
            return this;
        }

        #region IMetricsProvider Implementation

        public void ObserveValueFluctuation(string name, double value)
        {
            if (Histograms.ContainsKey(name))
            {
                Histograms[name].Observe(value);
            }
        }

        public void ObserveValue(string name, double value)
        {
            if (Summaries.ContainsKey(name))
            {
                Summaries[name].Observe(value);
            }
        }

        public void IncrementGauge(string name)
        {
            if (Gauges.ContainsKey(name))
            {
                Gauges[name].Inc();
            }
        }

        public void DecrementGauge(string name)
        {
            if (Gauges.ContainsKey(name))
            {
                Gauges[name].Inc();
            }
        }

        public void IncrementCounter(string name)
        {
            if (Counters.ContainsKey(name))
            {
                Counters[name].Inc();
            }
        }

        public void DecrementCounter(string name)
        {
            throw new NotImplementedException();
        }

        public IDisposable MeasureDuration(string name)
        {
            if (!Histograms.ContainsKey(name)) throw new ArgumentException(Constants.CounterAlreadyExists);

            return Histograms[name].NewTimer();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _collector?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
