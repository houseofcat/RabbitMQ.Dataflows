using HouseofCat.Utilities;
using System;
using System.Runtime.CompilerServices;

namespace HouseofCat.Metrics
{
    public class NullMetricsProvider : IMetricsProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecrementCounter(string name, bool create)
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecrementGauge(string name, bool create)
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementCounter(string name, bool create)
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementGauge(string name, bool create)
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable MeasureDuration(string name, bool create)
        {
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ObserveValue(string name, double value, bool create)
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ObserveValueFluctuation(string name, double value, bool create)
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable TrackConcurrency(string name, bool create)
        {
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultiDispose MeasureAndTrack(string name, bool create)
        {
            return null;
        }
    }
}
