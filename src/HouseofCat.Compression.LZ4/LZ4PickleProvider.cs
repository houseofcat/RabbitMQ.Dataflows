using K4os.Compression.LZ4;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public class LZ4PickleProvider : ICompressionProvider
    {
        public string Type { get; } = "LZ4";

        private readonly LZ4Level _level;

        public LZ4PickleProvider(LZ4Level? level = null)
        {
            _level = level ?? LZ4Level.L00_FAST;
        }

        public ArraySegment<byte> Compress(ReadOnlyMemory<byte> data)
        {
            return LZ4Pickler.Pickle(data.Span, _level);
        }

        public ValueTask<ArraySegment<byte>> CompressAsync(ReadOnlyMemory<byte> data)
        {
            throw new NotSupportedException();
        }

        public ValueTask<MemoryStream> CompressStreamAsync(Stream data, bool leaveStreamOpen = true)
        {
            throw new NotSupportedException();
        }

        public MemoryStream CompressToStream(ReadOnlyMemory<byte> data)
        {
            throw new NotSupportedException();
        }

        public ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> data)
        {
            throw new NotSupportedException();
        }

        public unsafe ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
        {
            return LZ4Pickler.Unpickle(compressedData.Span);
        }

        public ValueTask<ArraySegment<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData)
        {
            throw new NotSupportedException();
        }

        public MemoryStream DecompressStream(Stream compressedStream, bool leaveStreamOpen = false)
        {
            throw new NotSupportedException();
        }

        public ValueTask<MemoryStream> DecompressStreamAsync(Stream compressedStream, bool leaveStreamOpen = false)
        {
            throw new NotSupportedException();
        }

        public MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedData)
        {
            throw new NotSupportedException();
        }
    }
}
