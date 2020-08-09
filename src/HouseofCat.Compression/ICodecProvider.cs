using System;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public interface ICodecProvider
    {
        int Encode(ReadOnlySpan<byte> source, Span<byte> target);
        int Decode(ReadOnlySpan<byte> source, Span<byte> target);
    }
}
