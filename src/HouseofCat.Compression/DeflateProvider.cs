using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public class DeflateProvider : ICompressionProvider
    {
        public string Type { get; } = "DEFLATE";

        public byte[] Compress(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();

            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Compress))
            {
                deflateStream.Write(data.ToArray());
            }

            return compressedStream.ToArray();
        }

        public async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();

            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Compress))
            {
                await deflateStream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            return compressedStream.ToArray();
        }

        public byte[] Decompress(ReadOnlyMemory<byte> data)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(data.ToArray()))
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, false))
            {
                deflateStream.CopyTo(uncompressedStream);
            }

            return uncompressedStream.ToArray();
        }

        public async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> data)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(data.ToArray()))
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
