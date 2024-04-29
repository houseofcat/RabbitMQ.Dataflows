using HouseofCat.Compression;
using HouseofCat.Compression.LZ4;

namespace Compression.LZ4;

public class LZCodecTests
{
    [Fact]
    public void Encode_Decode_Success()
    {
        // Arrange
        var provider = new LZ4CodecProvider();
        var source = new byte[] { 1, 2, 3, 4, 5 };
        var target = new byte[20];

        // Act
        var encodedLength = provider.Encode(source, target);
        var decodedLength = provider.Decode(target.AsSpan().Slice(0, encodedLength), source.AsSpan());

        // Assert
        Assert.Equal(source.Length, decodedLength);
        Assert.Equal(source, source);
    }
}
