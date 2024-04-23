using HouseofCat.Utilities.Errors;
using K4os.Compression.LZ4;
using System;
using System.IO;
using System.Threading.Tasks;
using static HouseofCat.Compression.Enums;

namespace HouseofCat.Compression.LZ4;

public class LZ4PickleProvider : ICompressionProvider
{
    public string Type { get; } = CompressionType.LZ4Pickle.ToString().ToUpper();

    private readonly LZ4Level _level;

    public LZ4PickleProvider(LZ4Level? level = null)
    {
        _level = level ?? LZ4Level.L00_FAST;
    }

    public ReadOnlyMemory<byte> Compress(ReadOnlyMemory<byte> input)
    {
        Guard.AgainstEmpty(input, nameof(input));

        return LZ4Pickler.Pickle(input.Span, _level);
    }

    public ValueTask<ReadOnlyMemory<byte>> CompressAsync(ReadOnlyMemory<byte> input)
    {
        throw new NotSupportedException();
    }

    public MemoryStream Compress(Stream inputStream, bool leaveOpen = true)
    {
        throw new NotSupportedException();
    }

    public ValueTask<MemoryStream> CompressAsync(Stream inputStream, bool leaveOpen = true)
    {
        throw new NotSupportedException();
    }

    public MemoryStream CompressToStream(ReadOnlyMemory<byte> input)
    {
        throw new NotSupportedException();
    }

    public ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> input)
    {
        throw new NotSupportedException();
    }

    public ReadOnlyMemory<byte> Decompress(ReadOnlyMemory<byte> compressedInput)
    {
        Guard.AgainstEmpty(compressedInput, nameof(compressedInput));

        return LZ4Pickler.Unpickle(compressedInput.Span);
    }

    public ValueTask<ReadOnlyMemory<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedInput)
    {
        throw new NotSupportedException();
    }

    public MemoryStream Decompress(Stream compressedStream, bool leaveOpen = false)
    {
        throw new NotSupportedException();
    }

    public ValueTask<MemoryStream> DecompressAsync(Stream compressedStream, bool leaveOpen = false)
    {
        throw new NotSupportedException();
    }

    public MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedInput)
    {
        throw new NotSupportedException();
    }
}
