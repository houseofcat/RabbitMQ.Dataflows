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
            using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel))
            {
                deflateStream.Write(data.Span);
            }

            if (compressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return compressedStream.ToArray(); }
        }

        public async Task<ArraySegment<byte>> CompressAsync(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel))
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

        public MemoryStream CompressToStream(ReadOnlyMemory<byte> data)
        {
            var compressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel, true))
            {
                deflateStream.Write(data.Span);
            }

            return compressedStream;
        }

        public async Task<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> data)
        {
            var compressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel, true))
            {
                await deflateStream
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

        public async Task<ArraySegment<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData)
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
        /// Returns a new MemoryStream() that has decompressed data inside. Original stream is closed/disposed.
        /// </summary>
        /// <param name="compressedStream"></param>
        /// <returns></returns>
        public MemoryStream DecompressStream(Stream compressedStream)
        {
            compressedStream.Position = 0;

            var uncompressedStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, false))
            {
                deflateStream.CopyTo(uncompressedStream);
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
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, false))
            {
                await deflateStream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream;
        }
    }
}
