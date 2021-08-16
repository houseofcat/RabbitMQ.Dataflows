using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ArrayPoolChannel
{
    // RentMemory
    // PassMemory
    // Async Process memory
    // Async Reclaim Memory
    public static class Program
    {
        public static int GlobalCount = 1_000_000;
        public static bool ForceGcWithMemoryPressure = false;

        public static async Task Main(string[] args)
        {
            await Console.Out.WriteLineAsync("ArrayPool examples (some with Channels) says Hello!").ConfigureAwait(false);

            //RapidByteCreation();
            //RapidRentAndReturn();

            var writer = Task.Run(ChannelExamples.UnboundedMemoryWriteDataAsync);
            var reader = Task.Run(ChannelExamples.UnboundedMemoryReadDataAsync);

            //var writer = Task.Run(ChannelExamples.BoundedMemoryWriteDataAsync);
            //var reader = Task.Run(ChannelExamples.BoundedMemoryReadDataAsync);

            await writer.ConfigureAwait(false);
            await reader.ConfigureAwait(false);

            await Console.Out.WriteLineAsync("We are finished!").ConfigureAwait(false);

            if (ForceGcWithMemoryPressure)
            {
                GC.AddMemoryPressure(GlobalCount * 1024);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.ReadKey();
        }
    }

    public static class ChannelExamples
    {
        public static Channel<byte[]> UnboundedChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        public static Channel<Memory<byte>> UnboundedMemoryChannel = Channel.CreateUnbounded<Memory<byte>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        public static Channel<byte[]> BoundedChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(100) { SingleReader = true, SingleWriter = true });
        public static Channel<Memory<byte>> BoundedMemoryChannel = Channel.CreateBounded<Memory<byte>>(new BoundedChannelOptions(100) { SingleReader = true, SingleWriter = true });

        // Uses about ~15MB
        public static void RapidByteCreation()
        {
            for (int i = 0; i < Program.GlobalCount; i++)
            {
                var bytes = new byte[1024];
                bytes = null;
            }
        }

        // Uses about ~8MB
        public static void RapidRentAndReturn()
        {
            for (int i = 0; i < Program.GlobalCount; i++)
            {
                var bytes = ArrayPool<byte>.Shared.Rent(1024);
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        #region Unbounded Usage

        // Uses about ~1GB - doesn't come down.
        public static async Task UnboundedWriteDataAsync()
        {
            //await Task.Yield(); // forces the calling thread to immediately 
            var counter = 0;
            while (await UnboundedChannel.Writer.WaitToWriteAsync().ConfigureAwait(false))
            {
                var bytes = ArrayPool<byte>.Shared.Rent(1024);
                await UnboundedChannel.Writer.WriteAsync(bytes);
                counter++;

                if (counter == Program.GlobalCount)
                { UnboundedChannel.Writer.Complete(); }
            }
        }

        public static async Task UnboundedReadDataAsync()
        {
            // process each and every element in the stream until stream is closed.
            await foreach (var memory in UnboundedChannel.Reader.ReadAllAsync())
            {
                // Do Something with memory
                Console.WriteLine($"{memory}");

                ArrayPool<byte>.Shared.Return(memory);
            }
        }

        public static async Task UnboundedReadDataWithoutIAsyncEnumerableAsync()
        {
            // process each and every element in the stream until stream is closed.
            while (await UnboundedChannel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (UnboundedChannel.Reader.TryRead(out var memory))
                {
                    // Do Something with memory
                    Console.WriteLine($"{memory}");

                    ArrayPool<byte>.Shared.Return(memory);
                }
            }
        }

        // Uses about ~1GB - doesn't come down.
        public static async Task UnboundedMemoryWriteDataAsync()
        {
            //await Task.Yield(); // forces the calling thread to immediately 
            var counter = 0;
            while (await UnboundedMemoryChannel.Writer.WaitToWriteAsync().ConfigureAwait(false))
            {
                await UnboundedMemoryChannel.Writer.WriteAsync(new Memory<byte>(ArrayPool<byte>.Shared.Rent(1024), 0, 1024));
                counter++;

                if (counter == Program.GlobalCount)
                { UnboundedMemoryChannel.Writer.Complete(); }
            }
        }

        public static async Task UnboundedMemoryReadDataAsync()
        {
            // process each and every element in the stream until stream is closed.
            await foreach (var memory in UnboundedMemoryChannel.Reader.ReadAllAsync())
            {
                MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment);

                // Do Something with memory
                Console.WriteLine($"{memory}");

                ArrayPool<byte>.Shared.Return(segment.Array);
            }
        }

        public static async Task UnboundedMemoryReadDataWithoutIAsyncEnumerableAsync()
        {
            // process each and every element in the stream until stream is closed.
            while (await UnboundedMemoryChannel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (UnboundedMemoryChannel.Reader.TryRead(out var memory))
                {
                    MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment);

                    // Do Something with memory
                    Console.WriteLine($"{memory}");

                    ArrayPool<byte>.Shared.Return(segment.Array);
                }
            }
        }

        #endregion

        #region Bounded Usage

        // Uses about ~18MB
        public static async Task BoundedWriteDataAsync()
        {
            //await Task.Yield(); // forces the calling thread to immediately 
            var counter = 0;
            while (await BoundedChannel.Writer.WaitToWriteAsync().ConfigureAwait(false))
            {
                var bytes = ArrayPool<byte>.Shared.Rent(1024);
                await BoundedChannel.Writer.WriteAsync(bytes);
                counter++;

                if (counter == Program.GlobalCount)
                { UnboundedChannel.Writer.Complete(); }
            }
        }

        public static async Task BoundedReadDataAsync()
        {
            // process each and every element in the stream until stream is closed.
            await foreach (var memory in BoundedChannel.Reader.ReadAllAsync())
            {
                // Do Something with memory
                Console.WriteLine($"{memory}");

                ArrayPool<byte>.Shared.Return(memory);
            }
        }

        public static async Task BoundedReadDataWithoutIAsyncEnumerableAsync()
        {
            // process each and every element in the stream until stream is closed.
            while (await BoundedChannel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (BoundedChannel.Reader.TryRead(out var memory))
                {
                    // Do Something with memory
                    Console.WriteLine($"{memory}");

                    ArrayPool<byte>.Shared.Return(memory);
                }
            }
        }

        // Uses about ~1GB - doesn't come down.
        public static async Task BoundedMemoryWriteDataAsync()
        {
            //await Task.Yield(); // forces the calling thread to immediately 
            var counter = 0;
            while (await BoundedMemoryChannel.Writer.WaitToWriteAsync().ConfigureAwait(false))
            {
                await BoundedMemoryChannel.Writer.WriteAsync(new Memory<byte>(ArrayPool<byte>.Shared.Rent(1024), 0, 1024));
                counter++;

                if (counter == Program.GlobalCount)
                { UnboundedMemoryChannel.Writer.Complete(); }
            }
        }

        public static async Task BoundedMemoryReadDataAsync()
        {
            // process each and every element in the stream until stream is closed.
            await foreach (var memory in BoundedMemoryChannel.Reader.ReadAllAsync())
            {
                MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment);

                // Do Something with memory
                Console.WriteLine($"{memory}");

                ArrayPool<byte>.Shared.Return(segment.Array);
            }
        }

        public static async Task BoundedMemoryReadDataWithoutIAsyncEnumerableAsync()
        {
            // process each and every element in the stream until stream is closed.
            while (await BoundedMemoryChannel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (BoundedMemoryChannel.Reader.TryRead(out var memory))
                {
                    MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment);

                    // Do Something with memory
                    Console.WriteLine($"{memory}");

                    ArrayPool<byte>.Shared.Return(segment.Array);
                }
            }
        }
        #endregion
    }
}
