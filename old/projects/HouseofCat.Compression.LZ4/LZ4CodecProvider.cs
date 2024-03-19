using K4os.Compression.LZ4;
using System;

namespace HouseofCat.Compression;

public class LZ4CodecProvider : ICodecProvider
{
    public string Type { get; } = "LZ4CODEC";

    private readonly LZ4Level _level;

    public LZ4CodecProvider(LZ4Level? level = null)
    {
        _level = level ?? LZ4Level.L00_FAST;
    }

    public int Encode(ReadOnlySpan<byte> source, Span<byte> target)
    {
        return LZ4Codec.Encode(source, target, _level);
    }

    public int Decode(ReadOnlySpan<byte> source, Span<byte> target)
    {
        return LZ4Codec.Decode(source, target);
    }
}
