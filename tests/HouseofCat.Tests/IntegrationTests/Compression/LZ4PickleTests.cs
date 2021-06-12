using HouseofCat.Compression;
using Microsoft.Toolkit.HighPerformance;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HouseofCat.Tests.IntegrationTests
{
    public class LZ4PickleTests
    {
        private readonly ITestOutputHelper _output;
        private readonly ICompressionProvider _provider;

        private static byte[] _data = new byte[5000];
        private static byte[] _compressedData;

        public LZ4PickleTests(ITestOutputHelper output)
        {
            _output = output;
            Enumerable.Repeat<byte>(0xFF, 1000).ToArray().CopyTo(_data, 0);
            Enumerable.Repeat<byte>(0xAA, 1000).ToArray().CopyTo(_data, 1000);
            Enumerable.Repeat<byte>(0x1A, 1000).ToArray().CopyTo(_data, 2000);
            Enumerable.Repeat<byte>(0xAF, 1000).ToArray().CopyTo(_data, 3000);
            Enumerable.Repeat<byte>(0x01, 1000).ToArray().CopyTo(_data, 4000);

            _provider = new LZ4PickleProvider();
            _compressedData = _provider.Compress(_data).ToArray();
        }

        [Fact]
        public void Compress()
        {
            var compressedData = _provider.Compress(_data);

            Assert.NotNull(compressedData);
            Assert.NotEqual(compressedData.Array.Length, _data.Length);
            Assert.True(compressedData.Array.Length < _data.Length);
        }

        [Fact]
        public void Decompress()
        {
            var decompressedData = _provider.Decompress(_compressedData);

            Assert.NotNull(decompressedData);
            Assert.Equal(decompressedData.Array.Length, _data.Length);
            Assert.Equal(decompressedData, _data);
        }

        [Fact]
        public void CompressDecompress()
        {
            var compressedData = _provider.Compress(_data);

            Assert.NotNull(compressedData);
            Assert.NotEqual(compressedData.Array.Length, _data.Length);
            Assert.True(compressedData.Array.Length < _data.Length);

            var decompressedData = _provider.Decompress(compressedData);

            Assert.NotNull(decompressedData);
            Assert.Equal(decompressedData.Array.Length, _data.Length);
            Assert.Equal(decompressedData, _data);
        }
    }
}
