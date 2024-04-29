using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Compression;

public interface ICompressionProvider
{
    string Type { get; }

    ReadOnlyMemory<byte> Compress(ReadOnlyMemory<byte> input);
    MemoryStream Compress(Stream inputStream, bool leaveOpen = false);

    ValueTask<ReadOnlyMemory<byte>> CompressAsync(ReadOnlyMemory<byte> input);
    ValueTask<MemoryStream> CompressAsync(Stream inputStream, bool leaveOpen = false);

    MemoryStream CompressToStream(ReadOnlyMemory<byte> input);
    ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> input);

    ReadOnlyMemory<byte> Decompress(ReadOnlyMemory<byte> compressedInput);
    MemoryStream Decompress(Stream compressedStream, bool leaveOpen = false);

    ValueTask<ReadOnlyMemory<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedInput);
    ValueTask<MemoryStream> DecompressAsync(Stream compressedStream, bool leaveOpen = false);

    MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedInput);
}
