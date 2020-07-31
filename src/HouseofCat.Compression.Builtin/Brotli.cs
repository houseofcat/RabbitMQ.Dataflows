using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HouseofCat.Compression.Builtin
{
    public static class Brotli
    {
        public static async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> input)
        {
            using var compressedStream = new MemoryStream();

            using (var bstream = new BrotliStream(compressedStream, CompressionMode.Compress))
            {
                await bstream
                    .WriteAsync(input)
                    .ConfigureAwait(false);
            }

            return compressedStream.ToArray();
        }

        public static async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> input)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(input.ToArray()))
            using (var bstream = new BrotliStream(compressedStream, CompressionMode.Decompress, false))
            {
                await bstream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream.ToArray();
        }
    }
}
