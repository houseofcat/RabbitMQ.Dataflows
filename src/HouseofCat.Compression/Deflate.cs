using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public static class Deflate
    {
        public static async Task<byte[]> CompressAsync(byte[] input)
        {
            using var compressedStream = new MemoryStream();

            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Compress))
            {
                await deflateStream
                    .WriteAsync(input)
                    .ConfigureAwait(false);
            }

            return compressedStream.ToArray();
        }

        public static async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> input)
        {
            using var compressedStream = new MemoryStream();

            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Compress))
            {
                await deflateStream
                    .WriteAsync(input)
                    .ConfigureAwait(false);
            }

            return compressedStream.ToArray();
        }

        public static async Task<byte[]> DecompressAsync(byte[] input)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(input))
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, false))
            {
                await deflateStream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream.ToArray();
        }

        public static async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> input)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(input.ToArray()))
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, false))
            {
                await deflateStream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream.ToArray();
        }
    }
}
