using HouseofCat.Recyclable;
using HouseofCat.Utilities.Errors;
using Microsoft.Toolkit.HighPerformance;
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public class RecyclableGzipProvider : ICompressionProvider
    {
        public string Type { get; } = "GZIP";
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

        public ArraySegment<byte> Compress(ReadOnlyMemory<byte> inputData)
        {
            Guard.AgainstEmpty(inputData, nameof(inputData));

            var compressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider));
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, true))
            {
                gzipStream.Write(inputData.Span);
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

        public async ValueTask<ArraySegment<byte>> CompressAsync(ReadOnlyMemory<byte> inputData)
        {
            Guard.AgainstEmpty(inputData, nameof(inputData));

            var compressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider));
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, true))
            {
                await gzipStream
                    .WriteAsync(inputData)
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
        public MemoryStream Compress(Stream inputStream, bool leaveStreamOpen = false)
        {
            Guard.AgainstNullOrEmpty(inputStream, nameof(inputStream));

            if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

            var compressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider));
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, true))
            {
                inputStream.CopyTo(gzipStream);
            }
            if (!leaveStreamOpen) { inputStream.Close(); }

            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        /// <summary>
        /// Retrieve a new <c>MemoryStream</c> object with the contents unzipped and copied from the provided
        /// stream. The provided stream is optionally closed.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="data"></param>
        /// <param name="leaveStreamOpen"></param>
        /// <returns></returns>
        public async ValueTask<MemoryStream> CompressAsync(Stream inputStream, bool leaveStreamOpen = false)
        {
            Guard.AgainstNullOrEmpty(inputStream, nameof(inputStream));

            if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

            var compressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider));
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, true))
            {
                await inputStream
                    .CopyToAsync(gzipStream)
                    .ConfigureAwait(false);
            }
            if (!leaveStreamOpen) { inputStream.Close(); }

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
        public MemoryStream CompressToStream(ReadOnlyMemory<byte> inputData)
        {
            Guard.AgainstEmpty(inputData, nameof(inputData));

            var compressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider));
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, true))
            {
                gzipStream.Write(inputData.Span);
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
        public async ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> inputData)
        {
            Guard.AgainstEmpty(inputData, nameof(inputData));

            var compressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider));
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, true))
            {
                await gzipStream
                    .WriteAsync(inputData)
                    .ConfigureAwait(false);
            }

            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        public ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
        {
            Guard.AgainstEmpty(compressedData, nameof(compressedData));

            var uncompressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider), CompressionHelpers.GetGzipUncompressedLength(compressedData));
            using (var gzipStream = new GZipStream(compressedData.AsStream(), CompressionMode.Decompress, false))
            {
                gzipStream.CopyTo(uncompressedStream);
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
            Guard.AgainstEmpty(compressedData, nameof(compressedData));

            using var uncompressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider), CompressionHelpers.GetGzipUncompressedLength(compressedData));
            using (var gzipStream = new GZipStream(compressedData.AsStream(), CompressionMode.Decompress, false))
            {
                await gzipStream
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
        public MemoryStream Decompress(Stream compressedStream, bool leaveStreamOpen = false)
        {
            Guard.AgainstNullOrEmpty(compressedStream, nameof(compressedStream));

            if (compressedStream.Position == compressedStream.Length) { compressedStream.Seek(0, SeekOrigin.Begin); }

            var uncompressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider), CompressionHelpers.GetGzipUncompressedLength(compressedStream));
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen))
            {
                gzipStream.CopyTo(uncompressedStream);
            }

            return uncompressedStream;
        }

        /// <summary>
        /// Returns a new <c>MemoryStream</c> that has decompressed data inside. The provided stream is optionally closed.
        /// </summary>
        /// <param name="compressedStream"></param>
        /// <param name="leaveStreamOpen"></param>
        /// <returns></returns>
        public async ValueTask<MemoryStream> DecompressAsync(Stream compressedStream, bool leaveStreamOpen = false)
        {
            Guard.AgainstNullOrEmpty(compressedStream, nameof(compressedStream));

            if (compressedStream.Position == compressedStream.Length) { compressedStream.Seek(0, SeekOrigin.Begin); }

            var uncompressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider), CompressionHelpers.GetGzipUncompressedLength(compressedStream));
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen))
            {
                await gzipStream
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
            Guard.AgainstEmpty(compressedData, nameof(compressedData));

            var uncompressedStream = RecyclableManager.GetStream(nameof(RecyclableGzipProvider), CompressionHelpers.GetGzipUncompressedLength(compressedData));
            using (var gzipStream = new GZipStream(compressedData.AsStream(), CompressionMode.Decompress, false))
            {
                gzipStream.CopyTo(uncompressedStream);
            }

            return uncompressedStream;
        }
    }
}
