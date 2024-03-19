using HouseofCat.Utilities.Errors;
using K4os.Compression.LZ4;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Compression.LZ4;

public class LZ4PickleProvider : ICompressionProvider
{
    public string Type { get; } = "LZ4";

    private readonly LZ4Level _level;

    public LZ4PickleProvider(LZ4Level? level = null)
    {
        _level = level ?? LZ4Level.L00_FAST;
    }

    public ReadOnlyMemory<byte> Compress(ReadOnlyMemory<byte> inputData)
    {
        Guard.AgainstEmpty(inputData, nameof(inputData));

        return LZ4Pickler.Pickle(inputData.Span, _level);
    }

    public ValueTask<ReadOnlyMemory<byte>> CompressAsync(ReadOnlyMemory<byte> inputData)
    {
        throw new NotSupportedException();
    }

    public MemoryStream Compress(Stream inputStream, bool leaveStreamOpen = true)
    {
        throw new NotSupportedException();
    }

    public ValueTask<MemoryStream> CompressAsync(Stream inputStream, bool leaveStreamOpen = true)
    {
        throw new NotSupportedException();
    }

    public MemoryStream CompressToStream(ReadOnlyMemory<byte> dinputDataata)
    {
        throw new NotSupportedException();
    }

    public ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> inputData)
    {
        throw new NotSupportedException();
    }

    public ReadOnlyMemory<byte> Decompress(ReadOnlyMemory<byte> compressedData)
    {
        Guard.AgainstEmpty(compressedData, nameof(compressedData));

        return LZ4Pickler.Unpickle(compressedData.Span);
    }

    public ValueTask<ReadOnlyMemory<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData)
    {
        throw new NotSupportedException();
    }

    public MemoryStream Decompress(Stream compressedStream, bool leaveStreamOpen = false)
    {
        throw new NotSupportedException();
    }

    public ValueTask<MemoryStream> DecompressAsync(Stream compressedStream, bool leaveStreamOpen = false)
    {
        throw new NotSupportedException();
    }

    public MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedData)
    {
        throw new NotSupportedException();
    }
}
