using Microsoft.Toolkit.HighPerformance;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public class BrotliProvider : ICompressionProvider
    {
        public string Type { get; } = "BROTLI";
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

        public ArraySegment<byte> Compress(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var bstream = new BrotliStream(compressedStream, CompressionLevel, false))
            {
                bstream.Write(data.Span);
            }

            if (compressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return compressedStream.ToArray(); }
        }

        public async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var bstream = new BrotliStream(compressedStream, CompressionLevel, false))
            {
                await bstream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            return compressedStream.ToArray();
        }

        public MemoryStream CompressToStream(ReadOnlyMemory<byte> data)
        {
            var compressedStream = new MemoryStream();
            using (var bstream = new BrotliStream(compressedStream, CompressionLevel, true))
            {
                bstream.Write(data.Span);
            }

            return compressedStream;
        }

        public async Task<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> data)
        {
            var compressedStream = new MemoryStream();
            using (var bStream = new BrotliStream(compressedStream, CompressionLevel, true))
            {
                await bStream
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
                using (var bStream = new BrotliStream(compressedStream, CompressionMode.Decompress, false))
                {
                    bStream.CopyTo(uncompressedStream);
                }

                if (uncompressedStream.TryGetBuffer(out var buffer))
                { return buffer; }
                else
                { return uncompressedStream.ToArray(); }
            }
        }

        public async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> compressedData)
        {
            using var uncompressedStream = new MemoryStream();
            using (var bstream = new BrotliStream(compressedData.AsStream(), CompressionMode.Decompress, false))
            {
                await bstream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream.ToArray();
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
            using (var bstream = new BrotliStream(compressedStream, CompressionMode.Decompress, false))
            {
                bstream.CopyTo(uncompressedStream);
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
            using (var bstream = new BrotliStream(compressedStream, CompressionMode.Decompress, false))
            {
                await bstream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream;
        }
    }
}
