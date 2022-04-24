using System;

namespace HouseofCat.Compression
{
    public interface ICodecProvider
    {
        string Type { get; }
        int Encode(ReadOnlySpan<byte> source, Span<byte> target);
        int Decode(ReadOnlySpan<byte> source, Span<byte> target);
    }
}
