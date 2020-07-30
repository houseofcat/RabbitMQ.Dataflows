using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Compression
{
    public static class Gzip
    {
        public static async Task<byte[]> CompressAsync(byte[] input)
        {
            using var compressedStream = new MemoryStream();

            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                await gzipStream
                    .WriteAsync(input, 0, input.Length)
                    .ConfigureAwait(false);
            }

            return compressedStream.ToArray();
        }

        public static async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> input)
        {
            using var compressedStream = new MemoryStream();

            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                await gzipStream
                    .WriteAsync(input)
                    .ConfigureAwait(false);
            }

            return compressedStream.ToArray();
        }

        public static async Task<byte[]> DecompressAsync(byte[] input)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(input))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
            {
                await gzipStream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream.ToArray();
        }

        public static async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> input)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(input.ToArray()))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
            {
                await gzipStream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream.ToArray();
        }
    }
}
