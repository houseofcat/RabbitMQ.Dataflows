using CommunityToolkit.HighPerformance;
using HouseofCat.Utilities.Errors;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using static HouseofCat.Compression.Enums;

namespace HouseofCat.Compression;

public class BrotliProvider : ICompressionProvider
{
    public string Type { get; } = CompressionType.Brotli.ToString().ToUpper();
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

    public BrotliProvider() { }

    public BrotliProvider(CompressionLevel compressionLevel)
    {
        CompressionLevel = compressionLevel;
    }

    public ReadOnlyMemory<byte> Compress(ReadOnlyMemory<byte> input)
    {
        Guard.AgainstEmpty(input, nameof(input));

        using var compressedStream = new MemoryStream();
        using (var compressionStream = new BrotliStream(compressedStream, CompressionLevel, false))
        {
            compressionStream.Write(input.Span);
        }

        return compressedStream.ToArray();
    }

    public async ValueTask<ReadOnlyMemory<byte>> CompressAsync(ReadOnlyMemory<byte> input)
    {
        Guard.AgainstEmpty(input, nameof(input));

        using var compressedStream = new MemoryStream();
        using (var compressionStream = new BrotliStream(compressedStream, CompressionLevel, false))
        {
            await compressionStream
                .WriteAsync(input)
                .ConfigureAwait(false);
        }

        return compressedStream.ToArray();
    }

    /// <summary>
    /// Retrieve a new <c>MemoryStream</c> object with the contents unzipped and copied from the provided
    /// stream. The provided stream is optionally closed.
    /// </summary>
    /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
    /// <param name="data"></param>
    /// <returns></returns>
    public MemoryStream Compress(Stream inputStream, bool leaveOpen = false)
    {
        Guard.AgainstNullOrEmpty(inputStream, nameof(inputStream));

        if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

        var compressedStream = new MemoryStream();
        using (var compressionStream = new BrotliStream(compressedStream, CompressionLevel, true))
        {
            inputStream.CopyTo(compressionStream);
        }
        if (!leaveOpen) { inputStream.Close(); }

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
    public async ValueTask<MemoryStream> CompressAsync(Stream inputStream, bool leaveOpen = false)
    {
        Guard.AgainstNullOrEmpty(inputStream, nameof(inputStream));

        if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

        var compressedStream = new MemoryStream();
        using (var compressionStream = new BrotliStream(compressedStream, CompressionLevel, true))
        {
            await inputStream
                .CopyToAsync(compressionStream)
                .ConfigureAwait(false);
        }
        if (!leaveOpen) { inputStream.Close(); }

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
    public MemoryStream CompressToStream(ReadOnlyMemory<byte> input)
    {
        Guard.AgainstEmpty(input, nameof(input));

        var compressedStream = new MemoryStream();
        using (var compressionStream = new BrotliStream(compressedStream, CompressionLevel, true))
        {
            compressionStream.Write(input.Span);
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
    public async ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> compressedInput)
    {
        Guard.AgainstEmpty(compressedInput, nameof(compressedInput));

        var compressedStream = new MemoryStream();
        using (var compressionStream = new BrotliStream(compressedStream, CompressionLevel, true))
        {
            await compressionStream
                .WriteAsync(compressedInput)
                .ConfigureAwait(false);
        }

        compressedStream.Seek(0, SeekOrigin.Begin);
        return compressedStream;
    }

    public ReadOnlyMemory<byte> Decompress(ReadOnlyMemory<byte> compressedInput)
    {
        Guard.AgainstEmpty(compressedInput, nameof(compressedInput));

        using var uncompressedStream = new MemoryStream();
        using (var compressionStream = new BrotliStream(compressedInput.AsStream(), CompressionMode.Decompress, false))
        {
            compressionStream.CopyTo(uncompressedStream);
        }

        return uncompressedStream.ToArray();
    }

    public async ValueTask<ReadOnlyMemory<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedInput)
    {
        Guard.AgainstEmpty(compressedInput, nameof(compressedInput));

        using var uncompressedStream = new MemoryStream();
        using (var compressionStream = new BrotliStream(compressedInput.AsStream(), CompressionMode.Decompress, false))
        {
            await compressionStream
                .CopyToAsync(uncompressedStream)
                .ConfigureAwait(false);
        }

        return uncompressedStream.ToArray();
    }

    /// <summary>
    /// Returns a new <c>MemoryStream</c> that has decompressed data inside. The provided stream is optionally closed.
    /// </summary>
    /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
    /// <param name="compressedStream"></param>
    /// <returns></returns>
    public MemoryStream Decompress(Stream compressedStream, bool leaveOpen = false)
    {
        Guard.AgainstNullOrEmpty(compressedStream, nameof(compressedStream));

        if (compressedStream.Position == compressedStream.Length) { compressedStream.Seek(0, SeekOrigin.Begin); }

        var uncompressedStream = new MemoryStream();
        using (var compressionStream = new BrotliStream(compressedStream, CompressionMode.Decompress, leaveOpen))
        {
            compressionStream.CopyTo(uncompressedStream);
        }

        return uncompressedStream;
    }

    /// <summary>
    /// Returns a new <c>MemoryStream</c> that has decompressed data inside. The provided stream is optionally closed.
    /// </summary>
    /// <param name="compressedStream"></param>
    /// <returns></returns>
    public async ValueTask<MemoryStream> DecompressAsync(Stream compressedStream, bool leaveOpen = false)
    {
        Guard.AgainstNullOrEmpty(compressedStream, nameof(compressedStream));

        if (compressedStream.Position == compressedStream.Length) { compressedStream.Seek(0, SeekOrigin.Begin); }

        var uncompressedStream = new MemoryStream();
        using (var compressionStream = new BrotliStream(compressedStream, CompressionMode.Decompress, leaveOpen))
        {
            await compressionStream
                .CopyToAsync(uncompressedStream)
                .ConfigureAwait(false);
        }

        return uncompressedStream;
    }

    /// <summary>
    /// Returns a new <c>MemoryStream</c> that has decompressed data inside.
    /// </summary>
    /// <param name="compressedInput"></param>
    /// <returns>A <c>new MemoryStream</c>.</returns>
    public MemoryStream DecompressToStream(ReadOnlyMemory<byte> compressedInput)
    {
        var uncompressedStream = new MemoryStream();
        using (var compressionStream = new BrotliStream(compressedInput.AsStream(), CompressionMode.Decompress, false))
        {
            compressionStream.CopyTo(uncompressedStream);
        }

        return uncompressedStream;
    }
}
