using HouseofCat.Utilities.Errors;
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

        public ArraySegment<byte> Compress(ReadOnlyMemory<byte> inputData)
        {
            Guard.AgainstEmpty(inputData, nameof(inputData));

            using var compressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, _encoderSettings, false))
            {
                lz4Stream.Write(inputData.Span);
            }

            if (compressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return compressedStream.ToArray(); }
        }

        public async ValueTask<ArraySegment<byte>> CompressAsync(ReadOnlyMemory<byte> inputData)
        {
            Guard.AgainstEmpty(inputData, nameof(inputData));

            using var compressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, _encoderSettings, false))
            {
                await lz4Stream
                    .WriteAsync(inputData)
                    .ConfigureAwait(false);
            }

            if (compressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return compressedStream.ToArray(); }
        }

        /// <summary>
        /// Retrieve a new <c>MemoryStream</c> object with the contents unzipped and copied from the provided
        /// stream. The provided stream is optionally closed.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="data"></param>
        /// <returns></returns>
        public MemoryStream Compress(Stream inputStream, bool leaveStreamOpen = false)
        {
            Guard.AgainstNullOrEmpty(inputStream, nameof(inputStream));

            if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

            var compressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, _encoderSettings, true))
            {
                inputStream.CopyTo(lz4Stream);
            }
            if (!leaveStreamOpen) { inputStream.Close(); }

            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        /// <summary>
        /// Retrieve a new <c>MemoryStream</c> object with the contents unzipped and copied from the provided
        /// stream. The provided stream is optionally closed.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="data"></param>
        /// <returns></returns>
        public async ValueTask<MemoryStream> CompressAsync(Stream inputStream, bool leaveStreamOpen = false)
        {
            Guard.AgainstNullOrEmpty(inputStream, nameof(inputStream));

            if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

            var compressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, _encoderSettings, true))
            {
                await inputStream
                    .CopyToAsync(lz4Stream)
                    .ConfigureAwait(false);
            }
            if (!leaveStreamOpen) { inputStream.Close(); }

            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        /// <summary>
        /// Retrieve a new <c>MemoryStream</c> object with the contents contained zipped data writen from the unzipped
        /// bytes in <c>ReadOnlyMemory&lt;byte&gt;</c>.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="data"></param>
        /// <returns></returns>
        public MemoryStream CompressToStream(ReadOnlyMemory<byte> inputData)
        {
            Guard.AgainstEmpty(inputData, nameof(inputData));

            var compressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, _encoderSettings, true))
            {
                lz4Stream.Write(inputData.Span);
            }

            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        /// <summary>
        /// Retrieve a new <c>MemoryStream</c> object with the contents contained zipped data writen from the unzipped
        /// bytes in <c>ReadOnlyMemory&lt;byte&gt;</c>.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="data"></param>
        /// <returns></returns>
        public async ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> inputData)
        {
            Guard.AgainstEmpty(inputData, nameof(inputData));

            var compressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Encode(compressedStream, _encoderSettings, true))
            {
                await lz4Stream
                    .WriteAsync(inputData)
                    .ConfigureAwait(false);
            }

            compressedStream.Seek(0, SeekOrigin.Begin);
            return compressedStream;
        }

        public ArraySegment<byte> Decompress(ReadOnlyMemory<byte> compressedData)
        {
            Guard.AgainstEmpty(compressedData, nameof(compressedData));

            using var uncompressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Decode(compressedData.AsStream(), _decoderSettings, false))
            {
                lz4Stream.CopyTo(uncompressedStream);
            }

            if (uncompressedStream.TryGetBuffer(out var buffer))
            { return buffer; }
            else
            { return uncompressedStream.ToArray(); }
        }

        public async ValueTask<ArraySegment<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData)
        {
            Guard.AgainstEmpty(compressedData, nameof(compressedData));

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
        /// Returns a new <c>MemoryStream</c> that has decompressed data inside. The provided stream is optionally closed.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="compressedStream"></param>
        /// <returns></returns>
        public MemoryStream Decompress(Stream compressedStream, bool leaveStreamOpen = false)
        {
            Guard.AgainstNullOrEmpty(compressedStream, nameof(compressedStream));

            if (compressedStream.Position == compressedStream.Length) { compressedStream.Seek(0, SeekOrigin.Begin); }

            var uncompressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Decode(compressedStream, _decoderSettings, leaveStreamOpen))
            {
                lz4Stream.CopyTo(uncompressedStream);
            }

            return uncompressedStream;
        }

        /// <summary>
        /// Returns a new <c>MemoryStream</c> that has decompressed data inside. The provided stream is optionally closed.
        /// </summary>
        /// <param name="compressedStream"></param>
        /// <returns></returns>
        public async ValueTask<MemoryStream> DecompressAsync(Stream compressedStream, bool leaveStreamOpen = false)
        {
            Guard.AgainstNullOrEmpty(compressedStream, nameof(compressedStream));

            if (compressedStream.Position == compressedStream.Length) { compressedStream.Seek(0, SeekOrigin.Begin); }

            var uncompressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Decode(compressedStream, _decoderSettings, leaveStreamOpen))
            {
                await lz4Stream
                    .CopyToAsync(uncompressedStream)
                    .ConfigureAwait(false);
            }

            return uncompressedStream;
        }

        /// <summary>
        /// Returns a new <c>MemoryStream</c> that has decompressed data inside.
        /// </summary>
        /// <param name="compressedData"></param>
        /// <returns>A <c>new MemoryStream</c>.</returns>
        public MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedData)
        {
            var uncompressedStream = new MemoryStream();
            using (var lz4Stream = LZ4Stream.Decode(compressedData.AsStream(), _decoderSettings, false))
            {
                lz4Stream.CopyTo(uncompressedStream);
            }

            return uncompressedStream;
        }
    }
}
