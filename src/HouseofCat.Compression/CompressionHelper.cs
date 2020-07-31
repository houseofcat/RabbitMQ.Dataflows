using HouseofCat.Compression;
using HouseofCat.Compression.Builtin;
using System;
using System.Threading.Tasks;
using static HouseofCat.Services.Enums;

namespace HouseofCat.Services
{
    public static class CompressionHelper
    {
        public static async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> data, CompressionMethod method)
        {
            return method switch
            {
                CompressionMethod.LZ4 => await LZ4.DecompressAsync(data).ConfigureAwait(false),
                CompressionMethod.Deflate => await Deflate.DecompressAsync(data).ConfigureAwait(false),
                CompressionMethod.Brotli => await Brotli.DecompressAsync(data).ConfigureAwait(false),
                CompressionMethod.Gzip => await Gzip.DecompressAsync(data).ConfigureAwait(false),
                _ => await Gzip.DecompressAsync(data).ConfigureAwait(false)
            };
        }

        public static async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data, CompressionMethod method)
        {
            return method switch
            {
                CompressionMethod.LZ4 => await LZ4.CompressAsync(data).ConfigureAwait(false),
                CompressionMethod.Deflate => await Deflate.CompressAsync(data).ConfigureAwait(false),
                CompressionMethod.Brotli => await Brotli.CompressAsync(data).ConfigureAwait(false),
                CompressionMethod.Gzip => await Gzip.CompressAsync(data).ConfigureAwait(false),
                _ => await Gzip.CompressAsync(data).ConfigureAwait(false)
            };
        }
    }
}
