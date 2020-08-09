using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public class LZ4PickleProvider : ICompressionProvider
    {
        private readonly LZ4Level _level;

        public LZ4PickleProvider(LZ4Level? level = null)
        {
            _level = level ?? LZ4Level.L00_FAST;
        }

        public byte[] Compress(ReadOnlyMemory<byte> data)
        {
            return LZ4Pickler.Pickle(data.ToArray(), _level);
        }

        public Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data)
        {
            return Task.FromResult(LZ4Pickler.Pickle(data.ToArray(), _level));
        }

        public byte[] Decompress(ReadOnlyMemory<byte> data)
        {
            return LZ4Pickler.Unpickle(data.ToArray());
        }

        public Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> data)
        {
            return Task.FromResult(LZ4Pickler.Unpickle(data.ToArray()));
        }
    }
}
