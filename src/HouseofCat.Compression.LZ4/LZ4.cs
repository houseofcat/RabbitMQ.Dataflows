using K4os.Compression.LZ4.Streams;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public static class LZ4
    {
        private readonly static LZ4EncoderSettings encoderSettings = new LZ4EncoderSettings
        {
            CompressionLevel = K4os.Compression.LZ4.LZ4Level.L00_FAST,
        };

        private readonly static LZ4DecoderSettings decoderSettings = new LZ4DecoderSettings();

        public static async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> input)
        {
            using var compressedStream = new MemoryStream();

            using (var lz4Stream = LZ4Stream.Encode(compressedStream, encoderSettings, false))
            {
                await lz4Stream
                    .WriteAsync(input)
                    .ConfigureAwait(false);
            }

            return compressedStream.ToArray();
        }

        public static async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> input)
        {
            using var uncompressedStream = new MemoryStream();

            using (var compressedStream = new MemoryStream(input.ToArray()))
            using (var lz4Stream = LZ4Stream.Decode(compressedStream, decoderSettings, false))
            {
                await lz4Stream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream.ToArray();
        }
    }
}
