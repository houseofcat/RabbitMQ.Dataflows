using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public interface ICompressionProvider
    {
        string Type { get; }

        ArraySegment<byte> Compress(ReadOnlyMemory<byte> data);
        MemoryStream Compress(Stream data, bool leaveStreamOpen = false);

        ValueTask<ArraySegment<byte>> CompressAsync(ReadOnlyMemory<byte> data);
        ValueTask<MemoryStream> CompressAsync(Stream data, bool leaveStreamOpen = false);

        MemoryStream CompressToStream(ReadOnlyMemory<byte> data);
        ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> data);

        ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData);
        MemoryStream Decompress(Stream compressedStream, bool leaveStreamOpen = false);

        ValueTask<ArraySegment<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData);
        ValueTask<MemoryStream> DecompressAsync(Stream compressedStream, bool leaveStreamOpen = false);

        MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedData);
    }
}
