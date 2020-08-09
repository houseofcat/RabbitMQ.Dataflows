using HouseofCat.Compression;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HouseofCat.IntegrationTests
{
    public class CompressionTests
    {
        private readonly ITestOutputHelper _output;
        private readonly ICompressionProvider _gzipProvider;
        private readonly ICompressionProvider _brotliProvider;
        private readonly ICompressionProvider _deflateProvider;
        private readonly ICompressionProvider _lz4Provider;

        private static byte[] _compressableData = new byte[5000];

        public CompressionTests(ITestOutputHelper output)
        {
            _output = output;
            Enumerable.Repeat<byte>(0xFF, 1000).ToArray().CopyTo(_compressableData, 0);
            Enumerable.Repeat<byte>(0xAA, 1000).ToArray().CopyTo(_compressableData, 1000);
            Enumerable.Repeat<byte>(0x1A, 1000).ToArray().CopyTo(_compressableData, 2000);
            Enumerable.Repeat<byte>(0xAF, 1000).ToArray().CopyTo(_compressableData, 3000);
            Enumerable.Repeat<byte>(0x01, 1000).ToArray().CopyTo(_compressableData, 4000);

            _gzipProvider = new GzipProvider();
            _brotliProvider = new BrotliProvider();
            _deflateProvider = new DeflateProvider();
            _lz4Provider = new LZ4StreamProvider();
        }

        [Fact]
        public async Task Gzip_Compress()
        {
            var compressed = await _gzipProvider.CompressAsync(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public async Task Gzip_CompressDecompress()
        {
            var compressed = await _gzipProvider.CompressAsync(_compressableData);
            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);

            var decompressed = await _gzipProvider.DecompressAsync(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(decompressed.Length, _compressableData.Length);
            Assert.Equal(decompressed, _compressableData);
        }

        [Fact]
        public async Task Deflate_Compress()
        {
            var compressed = await _deflateProvider.CompressAsync(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public async Task Deflate_CompressDecompress()
        {
            var compressed = await _deflateProvider.CompressAsync(_compressableData);
            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);

            var decompressed = await _deflateProvider.DecompressAsync(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(decompressed.Length, _compressableData.Length);
            Assert.Equal(decompressed, _compressableData);
        }

        [Fact]
        public async Task Brotli_Compress()
        {
            var compressed = await _brotliProvider.CompressAsync(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public async Task Brotli_CompressDecompress()
        {
            var compressed = await _brotliProvider.CompressAsync(_compressableData);
            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);

            var decompressed = await _brotliProvider.DecompressAsync(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(decompressed.Length, _compressableData.Length);
            Assert.Equal(decompressed, _compressableData);
        }

        [Fact]
        public async Task LZ4_Compress()
        {
            var compressed = await _lz4Provider.CompressAsync(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public async Task LZ4_CompressDecompress()
        {
            var compressed = await _lz4Provider.CompressAsync(_compressableData);
            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);

            var decompressed = await _lz4Provider.DecompressAsync(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(decompressed.Length, _compressableData.Length);
            Assert.Equal(decompressed, _compressableData);
        }
    }
}
