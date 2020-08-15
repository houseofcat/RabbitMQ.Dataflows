using HouseofCat.Utilities;
using Prometheus;
using Prometheus.DotNetRuntime;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

        /// <summary>
        /// Use this constructor in AspNetCore setup (since AspNetCore Prometheus handles Server creation via middleware).
        /// </summary>
        public PrometheusMetricsProvider() { }

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

        public PrometheusMetricsProvider AddDotNetRuntimeStats(
            SampleEvery contentionSampleRate = SampleEvery.TenEvents,
            SampleEvery jitSampleRate = SampleEvery.HundredEvents,
            SampleEvery threadScheduleSampleRate = SampleEvery.OneEvent)
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

        #region IMetricsProvider Implementation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ObserveValueFluctuation(string name, double value, bool create)
        {
            name = $"{name}_Histogram";
            if (Histograms.ContainsKey(name))
            {
                Histograms[name].Observe(value);
            }
            else if (create)
            {
                AddHistogram(name, string.Empty);
                Histograms[name].Observe(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ObserveValue(string name, double value, bool create)
        {
            name = $"{name}_Summary";
            if (Summaries.ContainsKey(name))
            {
                Summaries[name].Observe(value);
            }
            else if (create)
            {
                AddSummary(name, string.Empty);
                Summaries[name].Observe(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementGauge(string name, bool create)
        {
            name = $"{name}_Gauge";
            if (Gauges.ContainsKey(name))
            {
                Gauges[name].Inc();
            }
            else if (create)
            {
                AddGauge(name, string.Empty);
                Gauges[name].Inc();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecrementGauge(string name, bool create)
        {
            name = $"{name}_Gauge";
            if (Gauges.ContainsKey(name))
            {
                Gauges[name].Dec();
            }
            else if (create)
            {
                AddGauge(name, string.Empty);
                Gauges[name].Dec();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementCounter(string name, bool create)
        {
            name = $"{name}_Counter";
            if (Counters.ContainsKey(name))
            {
                Counters[name].Inc();
            }
            else if (create)
            {
                AddCounter(name, string.Empty);
                Counters[name].Inc();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecrementCounter(string name, bool create)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable MeasureDuration(string name, bool create)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(Constants.HistogramNotExists);

            name = $"{name}_Histogram";
            if (Histograms.ContainsKey(name))
            {
                return Histograms[name].NewTimer();
            }
            else if (create)
            {
                AddHistogram(name, string.Empty);
                return Histograms[name].NewTimer();
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable TrackConcurrency(string name, bool create)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(Constants.GaugeNotExists);

            name = $"{name}_Gauge";
            if (Gauges.ContainsKey(name))
            {
                return Gauges[name].TrackInProgress();
            }
            else if (create)
            {
                AddGauge(name, string.Empty);
                return Gauges[name].TrackInProgress();
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultiDispose MeasureAndTrack(string name, bool create)
        {
            var measure = MeasureDuration(name, create);
            var track = TrackConcurrency(name, create);

            return new MultiDispose(measure, track);
        }

        #endregion
    }
}
