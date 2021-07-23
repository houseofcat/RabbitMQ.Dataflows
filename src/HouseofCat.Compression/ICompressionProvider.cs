using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public interface ICompressionProvider
    {
        string Type { get; }
        ArraySegment<byte> Compress(ReadOnlyMemory<byte> data);
        ValueTask<ArraySegment<byte>> CompressAsync(ReadOnlyMemory<byte> data);

        ValueTask<MemoryStream> CompressStreamAsync(Stream data, bool leaveStreamOpen = false);

        MemoryStream CompressToStream(ReadOnlyMemory<byte> data);
        ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> data);


        ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData);
        ValueTask<ArraySegment<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData);
        MemoryStream DecompressStream(Stream compressedStream, bool leaveStreamOpen = false);
        ValueTask<MemoryStream> DecompressStreamAsync(Stream compressedStream, bool leaveStreamOpen = false);
        MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedData);
    }
}
