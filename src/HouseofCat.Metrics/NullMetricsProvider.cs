using HouseofCat.Utilities;
using System;
using System.Runtime.CompilerServices;

namespace HouseofCat.Metrics
{
    public class NullMetricsProvider : IMetricsProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecrementCounter(string name, string description = null)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecrementGauge(string name, string description = null)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementCounter(string name, string description = null)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementGauge(string name, string description = null)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ObserveValue(string name, double value, string description = null)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ObserveValueFluctuation(string name, double value, string description = null)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable Duration(string name, bool microScale = false, string description = null)
        {
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable Track(string name, string description = null)
        {
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultiDispose TrackAndDuration(string name, bool microScale = false, string description = null)
        {
            return null;
        }
    }
}
