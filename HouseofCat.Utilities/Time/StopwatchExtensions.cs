using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HouseofCat.Library.Utilities.Time
{
    public static class StopwatchExtensions
    {
        private const long Billion = 1_000_000_000L;
        private const long Million = 1_000_000L;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ElapsedNanoseconds(this Stopwatch watch)
        {
            return (long)((double)watch.ElapsedTicks / Stopwatch.Frequency * Billion);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ElapsedMicroseconds(this Stopwatch watch)
        {
            return (long)((double)watch.ElapsedTicks / Stopwatch.Frequency * Million);
        }
    }
}
