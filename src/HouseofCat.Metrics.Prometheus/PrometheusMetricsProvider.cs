using HouseofCat.Utilities;
using HouseofCat.Utilities.Errors;
using Prometheus;
using Prometheus.DotNetRuntime;
using Prometheus.DotNetRuntime.Metrics.Producers;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace HouseofCat.Metrics
{
    public class PrometheusMetricsProvider : IMetricsProvider, IDisposable
    {
        private readonly IMetricServer _server;

        public ConcurrentDictionary<string, Counter> Counters { get; } = new ConcurrentDictionary<string, Counter>();
        public ConcurrentDictionary<string, Gauge> Gauges { get; } = new ConcurrentDictionary<string, Gauge>();
        public ConcurrentDictionary<string, Histogram> Histograms { get; } = new ConcurrentDictionary<string, Histogram>();
        public ConcurrentDictionary<string, Summary> Summaries { get; } = new ConcurrentDictionary<string, Summary>();

        private IDisposable _collector;
        private bool _disposedValue;

        /// <summary>
        /// Use this constructor in AspNetCore setup (since AspNetCore Prometheus handles Server creation via middleware).
        /// </summary>
        public PrometheusMetricsProvider()
        { }

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
            CaptureLevel contentionCaptureLevel = CaptureLevel.Counters,
            SampleEvery contentionSampleRate = SampleEvery.TenEvents,
            CaptureLevel jitCaptureLevel = CaptureLevel.Counters,
            SampleEvery jitSampleRate = SampleEvery.HundredEvents,
            CaptureLevel threadPoolCaptureLevel = CaptureLevel.Counters,
            ThreadPoolMetricsProducer.Options threadPoolOptions = null)
        {
            _collector ??= DotNetRuntimeStatsBuilder
                    .Customize()
                    .WithContentionStats(contentionCaptureLevel, contentionSampleRate)
                    .WithJitStats(jitCaptureLevel, jitSampleRate)
                    .WithThreadPoolStats(threadPoolCaptureLevel, threadPoolOptions)
                    .WithGcStats()
                    .StartCollecting();

            return this;
        }

        public PrometheusMetricsProvider AddDotNetRuntimeStats(DotNetRuntimeStatsBuilder.Builder builder)
        {
            _collector ??= builder.StartCollecting();

            return this;
        }

        public Counter GetOrAddCounter(string name, string description, CounterConfiguration config = null)
        {
            return Counters.GetOrAdd(name, Prometheus.Metrics.CreateCounter(name, description, config));
        }

        public Gauge GetOrAddGauge(string name, string description, GaugeConfiguration config = null)
        {
            return Gauges.GetOrAdd(name, Prometheus.Metrics.CreateGauge(name, description, config));
        }

        public Histogram GetOrAddHistogram(string name, string description, HistogramConfiguration config = null)
        {
            return Histograms.GetOrAdd(name, Prometheus.Metrics.CreateHistogram(name, description, config));
        }

        public Summary GetOrdAddSummary(string name, string description, SummaryConfiguration config = null)
        {
            return Summaries.GetOrAdd(name, Prometheus.Metrics.CreateSummary(name, description, config));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _collector?.Dispose();
                    _server.Stop();
                    _server.Dispose();
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
        public void ObserveValueFluctuation(string name, double value, string description = null)
        {
            Guard.AgainstNull(name, nameof(name));
            name = $"{name}_Histogram";
            GetOrAddHistogram(name, description).Observe(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ObserveValue(string name, double value, string description = null)
        {
            Guard.AgainstNull(name, nameof(name));
            name = $"{name}_Summary";
            GetOrdAddSummary(name, description).Observe(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementGauge(string name, string description = null)
        {
            Guard.AgainstNull(name, nameof(name));
            name = $"{name}_Gauge";
            GetOrAddGauge(name, description).Inc();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecrementGauge(string name, string description = null)
        {
            Guard.AgainstNull(name, nameof(name));
            name = $"{name}_Gauge";
            GetOrAddGauge(name, description).Dec();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementCounter(string name, string description = null)
        {
            Guard.AgainstNull(name, nameof(name));
            name = $"{name}_Counter";
            GetOrAddCounter(name, description).Inc();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecrementCounter(string name, string description = null)
        {
            throw new NotImplementedException();
        }

        private double[] _durationMicroBuckets = Histogram.ExponentialBuckets(0.000_001, 2, 10);
        private double[] _durationMilliBuckets = Histogram.ExponentialBuckets(0.001, 2, 16);
        public void SetDurationHistogramBucketSize(double[] microBuckets, double[] milliBuckets)
        {
            _durationMicroBuckets = microBuckets;
            _durationMilliBuckets = milliBuckets;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable Duration(string name, bool microScale = false, string description = null)
        {
            Guard.AgainstNull(name, nameof(name));
            name = $"{name}_Timer";
            return GetOrAddHistogram(
                name,
                description,
                new HistogramConfiguration
                {
                    Buckets = microScale ? _durationMicroBuckets : _durationMilliBuckets
                }).NewTimer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable Track(string name, string description = null)
        {
            Guard.AgainstNull(name, nameof(name));
            name = $"{name}_Track";
            return GetOrAddGauge(name, description).TrackInProgress();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultiDispose TrackAndDuration(string name, bool microScale = false, string description = null)
        {
            var duration = Duration(name, microScale, description);
            var track = Track(name, description);
            return new MultiDispose(duration, track);
        }

        #endregion
    }
}
