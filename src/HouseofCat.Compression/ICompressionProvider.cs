using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Compression;

public interface ICompressionProvider
{
    string Type { get; }

    ArraySegment<byte> Compress(ReadOnlyMemory<byte> inputData);
    MemoryStream Compress(Stream inputStream, bool leaveStreamOpen = false);

    ValueTask<ArraySegment<byte>> CompressAsync(ReadOnlyMemory<byte> inputData);
    ValueTask<MemoryStream> CompressAsync(Stream inputStream, bool leaveStreamOpen = false);

    MemoryStream CompressToStream(ReadOnlyMemory<byte> inputData);
    ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> inputData);

    ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData);
    MemoryStream Decompress(Stream compressedStream, bool leaveStreamOpen = false);

    ValueTask<ArraySegment<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData);
    ValueTask<MemoryStream> DecompressAsync(Stream compressedStream, bool leaveStreamOpen = false);

    MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedData);
}
