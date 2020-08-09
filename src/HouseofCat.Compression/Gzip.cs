using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public class GzipProvider : ICompressionProvider
    {
        public byte[] Compress(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                gzipStream.Write(data.ToArray());
            }

            return compressedStream.ToArray();
        }

        public async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                await gzipStream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            return compressedStream.ToArray();
        }

        public byte[] Decompress(ReadOnlyMemory<byte> data)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(data.ToArray()))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
            {
                gzipStream.CopyTo(uncompressedStream);
            }

            return uncompressedStream.ToArray();
        }

        public async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> data)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(data.ToArray()))
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
