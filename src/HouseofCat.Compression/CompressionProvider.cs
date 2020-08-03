using HouseofCat.Compression;
using HouseofCat.Compression.Builtin;
using System;
using System.Threading.Tasks;
using static HouseofCat.Compression.Enums;

namespace HouseofCat.Compression
{
    public interface ICompressionProvider
    {
        Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data);
        Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> data);
    }

    public class CompressionProvider : ICompressionProvider
    {
        public CompressionMethod CompressionMethod { get; private set; }

        public CompressionProvider(CompressionMethod method)
        {
            CompressionMethod = method;
        }

        public Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> data)
        {
            return CompressionHelper.DecompressAsync(data, CompressionMethod);
        }

        public Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data)
        {
            return CompressionHelper.CompressAsync(data, CompressionMethod);
        }
    }
}
