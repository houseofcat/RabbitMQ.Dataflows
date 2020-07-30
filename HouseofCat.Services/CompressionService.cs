using HouseofCat.Compression;
using System.Threading.Tasks;
using static HouseofCat.Services.Enums;

namespace HouseofCat.Services
{
    public interface ICompressionService
    {
        Task<byte[]> CompressAsync(byte[] data, CompressionMethod method);
        Task<byte[]> DecompressAsync(byte[] data, CompressionMethod method);
    }

    public class CompressionService : ICompressionService
    {
        public async Task<byte[]> DecompressAsync(byte[] data, CompressionMethod method)
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

        public async Task<byte[]> CompressAsync(byte[] data, CompressionMethod method)
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
