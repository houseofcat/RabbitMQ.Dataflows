using HouseofCat.Compression;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Compression
{
    public class GzipTests
    {
        private readonly ITestOutputHelper _output;
        private readonly ICompressionProvider _provider;

        private static byte[] _data = new byte[5000];
        private static byte[] _compressedData;

        public GzipTests(ITestOutputHelper output)
        {
            _output = output;
            Enumerable.Repeat<byte>(0xFF, 1000).ToArray().CopyTo(_data, 0);
            Enumerable.Repeat<byte>(0xAA, 1000).ToArray().CopyTo(_data, 1000);
            Enumerable.Repeat<byte>(0x1A, 1000).ToArray().CopyTo(_data, 2000);
            Enumerable.Repeat<byte>(0xAF, 1000).ToArray().CopyTo(_data, 3000);
            Enumerable.Repeat<byte>(0x01, 1000).ToArray().CopyTo(_data, 4000);

            _provider = new GzipProvider();
            _compressedData = _provider.Compress(_data).ToArray();
        }

        [Fact]
        public void Compress()
        {
            var compressedData = _provider.Compress(_data).ToArray();

            Assert.NotNull(compressedData);
            Assert.NotEqual(compressedData.Length, _data.Length);
            Assert.True(compressedData.Length < _data.Length);
        }

        [Fact]
        public async Task CompressAsync()
        {
            var compressedData = (await _provider.CompressAsync(_data)).ToArray();

            Assert.NotNull(compressedData);
            Assert.NotEqual(compressedData.Length, _data.Length);
            Assert.True(compressedData.Length < _data.Length);
        }

        [Fact]
        public void CompressToStream()
        {
            var compressedStream = _provider.CompressToStream(_data);
            var compressedData = compressedStream.ToArray();

            var uncompressedData = _provider.Decompress(compressedData).ToArray();

            Assert.NotNull(compressedData);
            Assert.NotEqual(compressedData.Length, _data.Length);
            Assert.True(compressedData.Length < _data.Length);

            Assert.Equal(uncompressedData, _data);
        }

        [Fact]
        public async Task CompressToStreamAsync()
        {
            var compressedStream = await _provider.CompressToStreamAsync(_data);
            var compressedData = compressedStream.ToArray();

            var uncompressedData = _provider.Decompress(compressedData).ToArray();

            Assert.NotNull(compressedData);
            Assert.NotEqual(compressedData.Length, _data.Length);
            Assert.True(compressedData.Length < _data.Length);

            Assert.Equal(uncompressedData, _data);
        }

        [Fact]
        public void Decompress()
        {
            var decompressedData = _provider.Decompress(_compressedData).ToArray();

            Assert.NotNull(decompressedData);
            Assert.Equal(decompressedData.Length, _data.Length);
            Assert.Equal(decompressedData, _data);
        }

        [Fact]
        public async Task DecompressAsync()
        {
            var decompressedData = (await _provider.DecompressAsync(_compressedData)).ToArray();

            Assert.NotNull(decompressedData);
            Assert.Equal(decompressedData.Length, _data.Length);
            Assert.Equal(decompressedData, _data);
        }

        [Fact]
        public void DecompressStream()
        {
            var decompressedStream = _provider.Decompress(new MemoryStream(_compressedData));
            var decompressedData = decompressedStream.ToArray();

            Assert.NotNull(decompressedData);
            Assert.Equal(decompressedData.Length, _data.Length);
            Assert.Equal(decompressedData, _data);
        }

        [Fact]
        public async Task DecompressStreamAsync()
        {
            var decompressedStream = await _provider.DecompressAsync(new MemoryStream(_compressedData));
            var decompressedData = decompressedStream.ToArray();

            Assert.NotNull(decompressedData);
            Assert.Equal(decompressedData.Length, _data.Length);
            Assert.Equal(decompressedData, _data);
        }

        [Fact]
        public void CompressDecompress()
        {
            var compressedData = _provider.Compress(_data).ToArray();

            Assert.NotNull(compressedData);
            Assert.NotEqual(compressedData.Length, _data.Length);
            Assert.True(compressedData.Length < _data.Length);

            var decompressedData = _provider.Decompress(compressedData).ToArray();

            Assert.NotNull(decompressedData);
            Assert.Equal(decompressedData.Length, _data.Length);
            Assert.Equal(decompressedData, _data);
        }

        [Fact]
        public async Task CompressDecompressAsync()
        {
            var compressedData = (await _provider.CompressAsync(_data)).ToArray();

            Assert.NotNull(compressedData);
            Assert.NotEqual(compressedData.Length, _data.Length);
            Assert.True(compressedData.Length < _data.Length);

            var decompressedData = (await _provider.DecompressAsync(compressedData)).ToArray();

            Assert.NotNull(decompressedData);
            Assert.Equal(decompressedData.Length, _data.Length);
            Assert.Equal(decompressedData, _data);
        }

        [Fact]
        public void CompressDecompressStream()
        {
            var compressedStream = _provider.CompressToStream(_data);
            var decompressedStream = _provider.Decompress(compressedStream);

            var decompressedData = decompressedStream.ToArray();

            Assert.NotNull(decompressedData);
            Assert.Equal(decompressedData.Length, _data.Length);
            Assert.Equal(decompressedData, _data);
        }

        [Fact]
        public async Task CompressDecompressStreamAsync()
        {
            var compressedStream = await _provider.CompressToStreamAsync(_data);
            var decompressedStream = await _provider.DecompressAsync(compressedStream);

            var decompressedData = decompressedStream.ToArray();

            Assert.NotNull(decompressedData);
            Assert.Equal(decompressedData.Length, _data.Length);
            Assert.Equal(decompressedData.ToArray(), _data);
        }

        [Fact]
        public void VerifyLengthCalculationAsync()
        {
            var lastFour = _compressedData.AsSpan(_compressedData.Length - 4, 4);
            var length = BitConverter.ToInt32(lastFour);
            var littleEndianLengthCalculation = (lastFour[3] << 24) | (lastFour[2] << 24) + (lastFour[1] << 8) + lastFour[0];
            var altLittleEndianLengthCalculation = (((((lastFour[3] << 8) | lastFour[2]) << 8) | lastFour[1]) << 8) | lastFour[0];

            Assert.Equal(_data.Length, length);
            Assert.Equal(_data.Length, littleEndianLengthCalculation);
            Assert.Equal(_data.Length, altLittleEndianLengthCalculation);

            var compressedStream = _provider.CompressToStreamAsync(_data).GetAwaiter().GetResult();
            var lengthFromCompressedStream = GetUncompressedLength(compressedStream);
            Assert.Equal(_data.Length, lengthFromCompressedStream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetUncompressedLength(Stream stream)
        {
            // Anticipate the uncompressed length of GZip to get adequate sized buffers.
            Span<byte> uncompressedLength = stackalloc byte[4];
            stream.Position = stream.Length - 4;
            stream.Read(uncompressedLength);
            stream.Seek(0, SeekOrigin.Begin);
            return BitConverter.ToInt32(uncompressedLength);
        }
    }
}
