using K4os.Compression.LZ4.Streams;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public class LZ4StreamProvider : ICompressionProvider
    {
        private readonly LZ4EncoderSettings _encoderSettings;
        private readonly LZ4DecoderSettings _decoderSettings;

        public LZ4StreamProvider(LZ4EncoderSettings encoderSettings = null, LZ4DecoderSettings decoderSettings = null)
        {
            _encoderSettings = encoderSettings ?? new LZ4EncoderSettings
            {
                CompressionLevel = K4os.Compression.LZ4.LZ4Level.L00_FAST,
            };

            _decoderSettings = decoderSettings ?? new LZ4DecoderSettings();
        }

        public byte[] Compress(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, _encoderSettings, false))
            {
                 lz4Stream.Write(data.ToArray());
            }

            return compressedStream.ToArray();
        }

        public async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, _encoderSettings, false))
            {
                await lz4Stream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            return compressedStream.ToArray();
        }

        public byte[] Decompress(ReadOnlyMemory<byte> data)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(data.ToArray()))
            using (var lz4Stream = LZ4Stream.Decode(compressedStream, _decoderSettings, false))
            {
                lz4Stream.CopyTo(uncompressedStream);
            }

            return uncompressedStream.ToArray();
        }

        public async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> data)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(data.ToArray()))
            using (var lz4Stream = LZ4Stream.Decode(compressedStream, _decoderSettings, false))
            {
                await lz4Stream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream.ToArray();
        }
    }
}
