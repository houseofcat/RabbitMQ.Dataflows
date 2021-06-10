using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using Microsoft.Toolkit.HighPerformance;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Compression
{
    public class LZ4StreamProvider : ICompressionProvider
    {
        public string Type { get; } = "LZ4STREAM";

        private readonly LZ4EncoderSettings _encoderSettings;
        private readonly LZ4DecoderSettings _decoderSettings;

        public LZ4StreamProvider(LZ4EncoderSettings encoderSettings = null, LZ4DecoderSettings decoderSettings = null)
        {
            _encoderSettings = encoderSettings ?? new LZ4EncoderSettings
            {
                CompressionLevel = LZ4Level.L00_FAST,
            };

            _decoderSettings = decoderSettings ?? new LZ4DecoderSettings();
        }

        public ArraySegment<byte> Compress(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, _encoderSettings, false))
            {
                lz4Stream.Write(data.Span);
            }

            if (compressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return compressedStream.ToArray(); }
        }

        public async Task<ArraySegment<byte>> CompressAsync(ReadOnlyMemory<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, _encoderSettings, false))
            {
                await lz4Stream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            if (compressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return compressedStream.ToArray(); }
        }

        public MemoryStream CompressToStream(ReadOnlyMemory<byte> data)
        {
            var compressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, _encoderSettings, true))
            {
                lz4Stream.Write(data.Span);
            }

            return compressedStream;
        }

        public async Task<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> data)
        {
            var compressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, _encoderSettings, true))
            {
                await lz4Stream
                    .WriteAsync(data)
                    .ConfigureAwait(false);
            }

            return compressedStream;
        }

        public unsafe ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
        {
            fixed (byte* pBuffer = &compressedData.Span[0])
            {
                using var uncompressedStream = new MemoryStream();
                using (var compressedStream = new UnmanagedMemoryStream(pBuffer, compressedData.Length))
                using (var lz4Stream = LZ4Stream.Decode(compressedStream, _decoderSettings, false))
                {
                    lz4Stream.CopyTo(uncompressedStream);
                }

                if (uncompressedStream.TryGetBuffer(out var buffer))
                { return buffer; }
                else
                { return uncompressedStream.ToArray(); }
            }
        }

        public async Task<ArraySegment<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData)
        {
            using var uncompressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Decode(compressedData.AsStream(), _decoderSettings, false))
            {
                await lz4Stream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            if (uncompressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return uncompressedStream.ToArray(); }
        }

        /// <summary>
        /// Returns a new MemoryStream() that has decompressed data inside. Original stream is closed/disposed.
        /// </summary>
        /// <param name="compressedStream"></param>
        /// <returns></returns>
        public MemoryStream DecompressStream(Stream compressedStream)
        {
            compressedStream.Position = 0;

            var uncompressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Decode(compressedStream, _decoderSettings, true))
            {
                lz4Stream.CopyTo(uncompressedStream);
            }

            return uncompressedStream;
        }

        /// <summary>
        /// Returns a new MemoryStream() that has decompressed data inside. Original stream is closed/disposed.
        /// </summary>
        /// <param name="compressedStream"></param>
        /// <returns></returns>
        public async Task<MemoryStream> DecompressStreamAsync(Stream compressedStream)
        {
            compressedStream.Position = 0;

            var uncompressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Decode(compressedStream, _decoderSettings, true))
            {
                await lz4Stream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream;
        }
    }
}
