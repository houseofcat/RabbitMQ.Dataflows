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
        private readonly ICompressionProvider _lz4StreamProvider;
        private readonly ICompressionProvider _lz4PickleProvider;

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
            _lz4StreamProvider = new LZ4StreamProvider();
            _lz4PickleProvider = new LZ4PickleProvider();
        }

        [Fact]
        public void Gzip_Compress()
        {
            var compressed = _gzipProvider.Compress(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public void Gzip_CompressDecompress()
        {
            var compressed = _gzipProvider.Compress(_compressableData);
            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);

            var decompressed = _gzipProvider.Decompress(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(decompressed.Length, _compressableData.Length);
            Assert.Equal(decompressed, _compressableData);
        }

        [Fact]
        public async Task Gzip_Compress_Async()
        {
            var compressed = await _gzipProvider.CompressAsync(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public async Task Gzip_CompressDecompress_Async()
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
        public void Deflate_Compress()
        {
            var compressed = _deflateProvider.Compress(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public void Deflate_CompressDecompress()
        {
            var compressed = _deflateProvider.Compress(_compressableData);
            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);

            var decompressed = _deflateProvider.Decompress(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(decompressed.Length, _compressableData.Length);
            Assert.Equal(decompressed, _compressableData);
        }

        [Fact]
        public async Task Deflate_Compress_Async()
        {
            var compressed = await _deflateProvider.CompressAsync(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public async Task Deflate_CompressDecompress_Async()
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
        public void Brotli_Compress()
        {
            var compressed = _brotliProvider.Compress(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public void Brotli_CompressDecompress()
        {
            var compressed = _brotliProvider.Compress(_compressableData);
            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);

            var decompressed = _brotliProvider.Decompress(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(decompressed.Length, _compressableData.Length);
            Assert.Equal(decompressed, _compressableData);
        }

        [Fact]
        public async Task Brotli_Compress_Async()
        {
            var compressed = await _brotliProvider.CompressAsync(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public async Task Brotli_CompressDecompress_Async()
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
        public void LZ4Stream_Compress()
        {
            var compressed = _lz4StreamProvider.Compress(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public void LZ4Stream_CompressDecompress()
        {
            var compressed = _lz4StreamProvider.Compress(_compressableData);
            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);

            var decompressed = _lz4StreamProvider.Decompress(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(decompressed.Length, _compressableData.Length);
            Assert.Equal(decompressed, _compressableData);
        }

        [Fact]
        public async Task LZ4Stream_Compress_Async()
        {
            var compressed = await _lz4StreamProvider.CompressAsync(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public async Task LZ4Stream_CompressDecompress_Async()
        {
            var compressed = await _lz4StreamProvider.CompressAsync(_compressableData);
            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);

            var decompressed = await _lz4StreamProvider.DecompressAsync(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(decompressed.Length, _compressableData.Length);
            Assert.Equal(decompressed, _compressableData);
        }

        [Fact]
        public void LZ4Pickle_Compress()
        {
            var compressed = _lz4PickleProvider.Compress(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public void LZ4Pickle_CompressDecompress()
        {
            var compressed = _lz4PickleProvider.Compress(_compressableData);
            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);

            var decompressed = _lz4PickleProvider.Decompress(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(decompressed.Length, _compressableData.Length);
            Assert.Equal(decompressed, _compressableData);
        }

        [Fact]
        public async Task LZ4Pickle_Compress_Async()
        {
            var compressed = await _lz4PickleProvider.CompressAsync(_compressableData);

            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);
        }

        [Fact]
        public async Task LZ4Pickle_CompressDecompress_Async()
        {
            var compressed = await _lz4PickleProvider.CompressAsync(_compressableData);
            Assert.NotNull(compressed);
            Assert.NotEqual(compressed.Length, _compressableData.Length);
            Assert.True(compressed.Length < _compressableData.Length);

            var decompressed = await _lz4PickleProvider.DecompressAsync(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(decompressed.Length, _compressableData.Length);
            Assert.Equal(decompressed, _compressableData);
        }
    }
}
