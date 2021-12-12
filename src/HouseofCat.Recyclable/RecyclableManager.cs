using Microsoft.IO;
using System;
using System.IO;

namespace HouseofCat.Recyclable
{
    public static class RecyclableManager
    {
        private static RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

        /// <summary>
        /// ConfigureStaticManager completely rebuilds the <c>RecyclableMemoryStreamManager</c> so try to call it only once, and on startup.
        /// </summary>
        /// <param name="blockSize"></param>
        /// <param name="largeBufferMultiple"></param>
        /// <param name="maximumBufferSize"></param>
        /// <param name="useExponentialLargeBuffer"></param>
        /// <param name="maximumSmallPoolFreeBytes"></param>
        /// <param name="maximumLargePoolFreeBytes"></param>
        public static void ConfigureNewStaticManager(
            int blockSize,
            int largeBufferMultiple,
            int maximumBufferSize,
            bool useExponentialLargeBuffer,
            long maximumSmallPoolFreeBytes,
            long maximumLargePoolFreeBytes)
        {
            _manager = new RecyclableMemoryStreamManager(blockSize, largeBufferMultiple, maximumBufferSize, useExponentialLargeBuffer, maximumSmallPoolFreeBytes, maximumLargePoolFreeBytes);
        }

        /// <summary>
        /// ConfigureStaticManagerWithDefaults completely rebuilds the <c>RecyclableMemoryStreamManager</c> so try to call it only once, and on startup.
        /// </summary>
        /// <param name="useExponentialLargeBuffer"></param>
        public static void ConfigureNewStaticManagerWithDefaults(bool useExponentialLargeBuffer = false)
        {
            var blockSize = 1024;
            var largeBufferMultiple = 4 * blockSize * blockSize;
            var maximumBufferSize = 2 * largeBufferMultiple;
            var maximumFreeLargePoolBytes = 32 * maximumBufferSize;
            var maximumFreeSmallPoolBytes = 256 * blockSize;

            _manager = new RecyclableMemoryStreamManager(blockSize, largeBufferMultiple, maximumBufferSize, useExponentialLargeBuffer, maximumFreeSmallPoolBytes, maximumFreeLargePoolBytes);
        }

        public static void SetGenerateCallStacks(bool input = true)
        {
            _manager.GenerateCallStacks = input;
        }

        public static void SetAggressiveBufferReturn(bool input = true)
        {
            _manager.GenerateCallStacks = input;
        }

        public static RecyclableMemoryStream GetStream()
        {
            return _manager.GetStream() as RecyclableMemoryStream;
        }

        public static RecyclableMemoryStream GetStream(string tag)
        {
            return _manager.GetStream(tag) as RecyclableMemoryStream;
        }

        public static RecyclableMemoryStream GetStream(string tag, int desiredSize)
        {
            return _manager.GetStream(tag, desiredSize) as RecyclableMemoryStream;
        }

        public static RecyclableMemoryStream GetStream(ReadOnlySpan<byte> buffer)
        {
            return _manager.GetStream(buffer) as RecyclableMemoryStream;
        }

        public static RecyclableMemoryStream GetStream(string tag, ReadOnlySpan<byte> buffer)
        {
            return _manager.GetStream(tag, buffer) as RecyclableMemoryStream;
        }

        public static RecyclableMemoryStream GetStream(string tag, ReadOnlySpan<byte> buffer, int start, int length)
        {
            return _manager.GetStream(tag, buffer.Slice(start, length)) as RecyclableMemoryStream;
        }

        public static void ReturnStream(MemoryStream stream)
        {
            stream.Dispose();
        }
    }
}
