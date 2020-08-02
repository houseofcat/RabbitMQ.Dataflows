using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HouseofCat.Extensions
{
    public static class ConcurrentQueueExtensions
    {
        public static IEnumerable<T> DequeueExisting<T>(this ConcurrentQueue<T> queue)
        {
            while (queue.TryDequeue(out T item))
            { yield return item; }
        }
    }
}
