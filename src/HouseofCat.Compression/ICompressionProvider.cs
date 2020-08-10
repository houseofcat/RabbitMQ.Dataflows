using System;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public interface ICompressionProvider
    {
        string Type { get; }
        byte[] Compress(ReadOnlyMemory<byte> data);
        byte[] Decompress(ReadOnlyMemory<byte> data);
        Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data);
        Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> data);
    }
}
