using Microsoft.Toolkit.HighPerformance;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public class GzipProvider : ICompressionProvider
    {
        public string Type { get; } = "GZIP";
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

        public ArraySegment<byte> Compress(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, false))
            {
                gzipStream.Write(data.Span);
            }

            if (compressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return compressedStream.ToArray(); }
        }

        public async Task<ArraySegment<byte>> CompressAsync(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, false))
            {
                await gzipStream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            if (compressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return compressedStream.ToArray(); }
        }

        public MemoryStream CompressToStream(ReadOnlyMemory<byte> data)
        {
            var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, true))
            {
                gzipStream.Write(data.Span);
            }

            return compressedStream;
        }

        public async Task<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> data)
        {
            var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel, true))
            {
                await gzipStream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            return compressedStream;
        }

        public unsafe ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
        {
            fixed (byte* pBuffer = &compressedData.Span[0])
            {
                using var uncompressedStream = new MemoryStream();
                using (var compressedStream = new UnmanagedMemoryStream(pBuffer, compressedData.Length))
                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
                {
                    gzipStream.CopyTo(uncompressedStream);
                }

                if (uncompressedStream.TryGetBuffer(out var buffer))
                { return buffer; }
                else
                { return uncompressedStream.ToArray(); }
            }
        }

        public async Task<ArraySegment<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData)
        {
            using var uncompressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedData.AsStream(), CompressionMode.Decompress, false))
            {
                await gzipStream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            if (uncompressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return uncompressedStream.ToArray(); }
        }

        /// <summary>
        /// Returns a new MemoryStream() that has decompressed data inside. Original stream is closed/disposed.
        /// </summary>
        /// <param name="compressedStream"></param>
        /// <returns></returns>
        public MemoryStream DecompressStream(Stream compressedStream)
        {
            compressedStream.Position = 0;

            var uncompressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
            {
                gzipStream.CopyTo(uncompressedStream);
            }

            return uncompressedStream;
        }

        /// <summary>
        /// Returns a new MemoryStream() that has decompressed data inside. Original stream is closed/disposed.
        /// </summary>
        /// <param name="compressedStream"></param>
        /// <returns></returns>
        public async Task<MemoryStream> DecompressStreamAsync(Stream compressedStream)
        {
            compressedStream.Position = 0;

            var uncompressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
            {
                await gzipStream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream;
        }
    }
}
