using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public interface ICompressionProvider
    {
        string Type { get; }
        ArraySegment<byte> Compress(ReadOnlyMemory<byte> data);
        Task<ArraySegment<byte>> CompressAsync(ReadOnlyMemory<byte> data);

        Task<MemoryStream> CompressStreamAsync(Stream data);

        MemoryStream CompressToStream(ReadOnlyMemory<byte> data);
        Task<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> data);


        ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData);
        Task<ArraySegment<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData);
        MemoryStream DecompressStream(Stream compressedStream);
        Task<MemoryStream> DecompressStreamAsync(Stream compressedStream);
        MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedData);
    }
}
