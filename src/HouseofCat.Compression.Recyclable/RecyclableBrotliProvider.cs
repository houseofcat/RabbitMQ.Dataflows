using HouseofCat.Recyclable;
using Microsoft.Toolkit.HighPerformance;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public class RecyclableBrotliProvider : ICompressionProvider
    {
        public string Type { get; } = "BROTLI";
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

        public ArraySegment<byte> Compress(ReadOnlyMemory<byte> data)
        {
            var compressedStream = RecyclableManager.GetStream(nameof(RecyclableBrotliProvider));
            using (var brotliStream = new BrotliStream(compressedStream, CompressionLevel, true))
            {
                brotliStream.Write(data.Span);
            }

            if (compressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            {
                using (compressedStream) // dispose stream after allocation.
                {
                    return compressedStream.ToArray();
                }
            }
        }

        public async ValueTask<ArraySegment<byte>> CompressAsync(ReadOnlyMemory<byte> data)
        {
            var compressedStream = RecyclableManager.GetStream(nameof(RecyclableBrotliProvider));
            using (var brotliStream = new BrotliStream(compressedStream, CompressionLevel, true))
            {
                await brotliStream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            if (compressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            {
                using (compressedStream) // dispose stream after allocation.
                {
                    return compressedStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Retrieve a new <c>MemoryStream</c> object with the contents unzipped and copied from the provided
        /// stream. The provided stream is optionally closed.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="data"></param>
        /// <param name="leaveStreamOpen"></param>
        /// <returns></returns>
        public async ValueTask<MemoryStream> CompressStreamAsync(Stream data, bool leaveStreamOpen = false)
        {
            var compressedStream = RecyclableManager.GetStream(nameof(RecyclableBrotliProvider));
            using (var brotliStream = new BrotliStream(compressedStream, CompressionLevel, true))
            {
                await data
                    .CopyToAsync(brotliStream)
                    .ConfigureAwait(false);
            }
            if (!leaveStreamOpen) { data.Close(); }

            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        /// <summary>
        /// Retrieve a new <c>MemoryStream</c> object with the contents contained zipped data writen from the unzipped
        /// bytes in <c>ReadOnlyMemory&lt;byte&gt;</c>.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="data"></param>
        /// <returns></returns>
        public MemoryStream CompressToStream(ReadOnlyMemory<byte> data)
        {
            var compressedStream = RecyclableManager.GetStream(nameof(RecyclableBrotliProvider));
            using (var brotliStream = new BrotliStream(compressedStream, CompressionLevel, true))
            {
                brotliStream.Write(data.Span);
            }

            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        /// <summary>
        /// Retrieve a new <c>MemoryStream</c> object with the contents contained zipped data writen from the unzipped
        /// bytes in <c>ReadOnlyMemory&lt;byte&gt;</c>.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="data"></param>
        /// <returns></returns>
        public async ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> data)
        {
            var compressedStream = RecyclableManager.GetStream(nameof(RecyclableBrotliProvider));
            using (var brotliStream = new BrotliStream(compressedStream, CompressionLevel, true))
            {
                await brotliStream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        public ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
        {
            var uncompressedStream = RecyclableManager.GetStream(nameof(RecyclableBrotliProvider));
            using (var brotliStream = new BrotliStream(compressedData.AsStream(), CompressionMode.Decompress, false))
            {
                brotliStream.CopyTo(uncompressedStream);
            }

            if (uncompressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            {
                // dispose stream after allocation.
                using (uncompressedStream) 
                {
                    return uncompressedStream.ToArray();
                }
            }
        }

        public async ValueTask<ArraySegment<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData)
        {
            using var uncompressedStream = RecyclableManager.GetStream(nameof(RecyclableBrotliProvider));
            using (var brotliStream = new BrotliStream(compressedData.AsStream(), CompressionMode.Decompress, false))
            {
                await brotliStream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            if (uncompressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            {
                // dispose stream after allocation.
                using (uncompressedStream)
                {
                    return uncompressedStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Returns a new MemoryStream() that has decompressed data inside. Original stream is closed/disposed.
        /// <para>Use <c>RecyclableManager.ReturnStream()</c> to return the <c>MemoryStream</c> when you are ready to dispose of it.</para>
        /// </summary>
        /// <param name="compressedStream"></param>
        /// <param name="leaveStreamOpen"></param>
        /// <returns></returns>
        public MemoryStream DecompressStream(Stream compressedStream, bool leaveStreamOpen = false)
        {
            var uncompressedStream = RecyclableManager.GetStream(nameof(RecyclableBrotliProvider));
            using (var brotliStream = new BrotliStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen))
            {
                brotliStream.CopyTo(uncompressedStream);
            }

            return uncompressedStream;
        }

        /// <summary>
        /// Returns a new <c>MemoryStream</c> that has decompressed data inside. The provided stream is optionally closed.
        /// </summary>
        /// <param name="compressedStream"></param>
        /// <param name="leaveStreamOpen"></param>
        /// <returns></returns>
        public async ValueTask<MemoryStream> DecompressStreamAsync(Stream compressedStream, bool leaveStreamOpen = false)
        {
            var uncompressedStream = RecyclableManager.GetStream(nameof(RecyclableBrotliProvider));
            using (var brotliStream = new BrotliStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen))
            {
                await brotliStream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream;
        }

        /// <summary>
        /// Returns a new <c>MemoryStream</c> that has decompressed data inside.
        /// </summary>
        /// <param name="compressedStream"></param>
        /// <param name="leaveStreamOpen"></param>
        /// <returns>A <c>new MemoryStream</c>.</returns>
        public MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedData)
        {
            var uncompressedStream = RecyclableManager.GetStream(nameof(RecyclableBrotliProvider));
            using (var brotliStream = new BrotliStream(compressedData.AsStream(), CompressionMode.Decompress, false))
            {
                brotliStream.CopyTo(uncompressedStream);
            }

            return uncompressedStream;
        }
    }
}
