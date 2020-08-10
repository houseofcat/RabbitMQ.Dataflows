using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public class BrotliProvider : ICompressionProvider
    {
        public string Type { get; } = "BROTLI";

        public byte[] Compress(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();

            using (var bstream = new BrotliStream(compressedStream, CompressionMode.Compress))
            {
                bstream.Write(data.ToArray());
            }

            return compressedStream.ToArray();
        }

        public async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();

            using (var bstream = new BrotliStream(compressedStream, CompressionMode.Compress))
            {
                await bstream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            return compressedStream.ToArray();
        }

        public byte[] Decompress(ReadOnlyMemory<byte> data)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(data.ToArray()))
            using (var bstream = new BrotliStream(compressedStream, CompressionMode.Decompress, false))
            {
                bstream.CopyTo(uncompressedStream);
            }

            return uncompressedStream.ToArray();
        }

        public async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> data)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(data.ToArray()))
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
