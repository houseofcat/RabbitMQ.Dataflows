using Microsoft.Toolkit.HighPerformance;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public class DeflateProvider : ICompressionProvider
    {
        public string Type { get; } = "DEFLATE";
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

        public ArraySegment<byte> Compress(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel, false))
            {
                deflateStream.Write(data.Span);
            }

            if (compressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return compressedStream.ToArray(); }
        }

        public async ValueTask<ArraySegment<byte>> CompressAsync(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel, false))
            {
                await deflateStream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            if (compressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return compressedStream.ToArray(); }
        }

        /// <summary>
        /// Retrieve a new <c>MemoryStream</c> object with the contents unzipped and copied from the provided
        /// stream. The provided stream is optionally closed.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="data"></param>
        /// <returns></returns>
        public MemoryStream Compress(Stream data, bool leaveStreamOpen = false)
        {
            var compressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel, true))
            {
                data.CopyTo(deflateStream);
            }
            if (!leaveStreamOpen) { data.Close(); }

            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        /// <summary>
        /// Retrieve a new <c>MemoryStream</c> object with the contents unzipped and copied from the provided
        /// stream. The provided stream is optionally closed.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="data"></param>
        /// <returns></returns>
        public async ValueTask<MemoryStream> CompressAsync(Stream data, bool leaveStreamOpen = false)
        {
            var compressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel, true))
            {
                await data
                    .CopyToAsync(deflateStream)
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
            var compressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel, true))
            {
                deflateStream.Write(data.Span);
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
            var compressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel, true))
            {
                await deflateStream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        public unsafe ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
        {
            fixed (byte* pBuffer = &compressedData.Span[0])
            {
                using var uncompressedStream = new MemoryStream();
                using (var compressedStream = new UnmanagedMemoryStream(pBuffer, compressedData.Length))
                using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, false))
                {
                    deflateStream.CopyTo(uncompressedStream);
                }

                if (uncompressedStream.TryGetBuffer(out var buffer))
                { return buffer; }
                else
                { return uncompressedStream.ToArray(); }
            }
        }

        public async ValueTask<ArraySegment<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData)
        {
            using var uncompressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedData.AsStream(), CompressionMode.Decompress, false))
            {
                await deflateStream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            if (uncompressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return uncompressedStream.ToArray(); }
        }

        /// <summary>
        /// Returns a new <c>MemoryStream</c> that has decompressed data inside. The provided stream is optionally closed.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="compressedStream"></param>
        /// <returns></returns>
        public MemoryStream Decompress(Stream compressedStream, bool leaveStreamOpen = false)
        {
            var uncompressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen))
            {
                deflateStream.CopyTo(uncompressedStream);
            }

            return uncompressedStream;
        }

        /// <summary>
        /// Returns a new <c>MemoryStream</c> that has decompressed data inside. The provided stream is optionally closed.
        /// </summary>
        /// <param name="compressedStream"></param>
        /// <returns></returns>
        public async ValueTask<MemoryStream> DecompressAsync(Stream compressedStream, bool leaveStreamOpen = false)
        {
            var uncompressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen))
            {
                await deflateStream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream;
        }

        /// <summary>
        /// Returns a new <c>MemoryStream</c> that has decompressed data inside.
        /// </summary>
        /// <param name="compressedData"></param>
        /// <returns>A <c>new MemoryStream</c>.</returns>
        public MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedData)
        {
            var uncompressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedData.AsStream(), CompressionMode.Decompress, false))
            {
                deflateStream.CopyTo(uncompressedStream);
            }

            return uncompressedStream;
        }
    }
}
