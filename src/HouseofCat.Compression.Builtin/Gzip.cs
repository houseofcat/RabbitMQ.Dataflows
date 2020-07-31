using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HouseofCat.Compression.Builtin
{
    public static class Gzip
    {
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
