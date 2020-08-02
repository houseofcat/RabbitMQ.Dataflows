using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HouseofCat.Gremlins
{
    /// <summary>
    /// This Gremlin is used to create heavy RAM usage. Useful for hardening applications that might suffer errors
    /// when the RAM is taxed and at risk for OutofMemoryExceptions.
    /// </summary>
    public class MemoryUsageGremlin
    {
        #region Private Variables

        private readonly object _syncRoot = new object();

        #endregion


        #region Public Variables 

        /// <summary>
        /// Stores byte[] arrays of varying sizes to simulate .Net CLR memory usage.
        /// </summary>
        public static ConcurrentQueue<byte[]> ByteQueue = new ConcurrentQueue<byte[]>();

        /// <summary>
        /// Stores IntPtr of memory allocations from Unmanaged code. Simulates low RAM usage availability.
        /// </summary>
        public static ConcurrentQueue<IntPtr> MemoryAllocationQueue = new ConcurrentQueue<IntPtr>();

        #endregion

        /// <summary>
        /// Create and store a byte[] in memory.
        /// </summary>
        /// <param name="sizeInBytes"></param>
        /// <returns></returns>
        public Task AddNetMemoryPressureAsync(int sizeInBytes)
        {
            ByteQueue.Enqueue(new byte[sizeInBytes]);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes the first byte[] in memory (if any exist).
        /// </summary>
        /// <returns>Success or failure.</returns>
        public Task<bool> ReduceNetMemoryPressureAsync()
        {
            return Task.FromResult((ByteQueue.TryDequeue(out byte[] array)));
        }

        /// <summary>
        /// Allocate a memory size to a IntPtr and store the pointer.
        /// </summary>
        /// <returns></returns>
        public Task AllocateUnmanagedMemoryAsync(int sizeInBytes)
        {
            MemoryAllocationQueue.Enqueue(Marshal.AllocHGlobal(sizeInBytes));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Deallocates the first memory location stored by a IntPtr in memory (if it exists).
        /// </summary>
        /// <returns>Success or failure.</returns>
        public Task<bool> DellocateUnmanagedMemoryAsync()
        {
            var success = false;
            if (MemoryAllocationQueue.TryDequeue(out IntPtr allocation))
            {
                Marshal.FreeHGlobal(allocation);
                success = true;
            }

            return Task.FromResult(success);
        }

        #region Helpers

        /// <summary>
        /// Clears out all stored managed and unmanaged memory allocations.
        /// </summary>
        public Task ResetGremlinAsync()
        {
            lock (_syncRoot)
            {
                try
                {
                    while (ByteQueue.TryDequeue(out byte[] array)) ;
                    while (MemoryAllocationQueue.TryDequeue(out IntPtr allocation))
                    {
                        Marshal.FreeHGlobal(allocation);
                    }
                }
                catch
                {
                    ByteQueue = new ConcurrentQueue<byte[]>();
                    MemoryAllocationQueue = new ConcurrentQueue<IntPtr>();
                }
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
