using HouseofCat.Utilities.Errors;
using CommunityToolkit.HighPerformance;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HouseofCat.Compression;

public class BrotliProvider : ICompressionProvider
{
    public string Type { get; } = "BROTLI";
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

    public BrotliProvider() { }

    public BrotliProvider(CompressionLevel compressionLevel)
    {
        CompressionLevel = compressionLevel;
    }

    public ReadOnlyMemory<byte> Compress(ReadOnlyMemory<byte> inputData)
    {
        Guard.AgainstEmpty(inputData, nameof(inputData));

        using var compressedStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(compressedStream, CompressionLevel, false))
        {
            brotliStream.Write(inputData.Span);
        }

        return compressedStream.ToArray();
    }

    public async ValueTask<ReadOnlyMemory<byte>> CompressAsync(ReadOnlyMemory<byte> inputData)
    {
        Guard.AgainstEmpty(inputData, nameof(inputData));

        using var compressedStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(compressedStream, CompressionLevel, false))
        {
            await brotliStream
                .WriteAsync(inputData)
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
    public MemoryStream Compress(Stream inputStream, bool leaveStreamOpen = false)
    {
        Guard.AgainstNullOrEmpty(inputStream, nameof(inputStream));

        if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

        var compressedStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(compressedStream, CompressionLevel, true))
        {
            inputStream.CopyTo(brotliStream);
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
        using (var brotliStream = new BrotliStream(compressedStream, CompressionLevel, true))
        {
            await inputStream
                .CopyToAsync(brotliStream)
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
        using (var brotliStream = new BrotliStream(compressedStream, CompressionLevel, true))
        {
            brotliStream.Write(inputData.Span);
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
    public async ValueTask<MemoryStream> CompressToStreamAsync(ReadOnlyMemory<byte> compressedData)
    {
        Guard.AgainstEmpty(compressedData, nameof(compressedData));

        var compressedStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(compressedStream, CompressionLevel, true))
        {
            await brotliStream
                .WriteAsync(compressedData)
                .ConfigureAwait(false);
        }

        compressedStream.Seek(0, SeekOrigin.Begin);
        return compressedStream;
    }

    public ReadOnlyMemory<byte> Decompress(ReadOnlyMemory<byte> compressedData)
    {
        Guard.AgainstEmpty(compressedData, nameof(compressedData));

        using var uncompressedStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(compressedData.AsStream(), CompressionMode.Decompress, false))
        {
            brotliStream.CopyTo(uncompressedStream);
        }

        return uncompressedStream.ToArray();
    }

    public async ValueTask<ReadOnlyMemory<byte>> DecompressAsync(ReadOnlyMemory<byte> compressedData)
    {
        Guard.AgainstEmpty(compressedData, nameof(compressedData));

        using var uncompressedStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(compressedData.AsStream(), CompressionMode.Decompress, false))
        {
            await brotliStream
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
    public MemoryStream Decompress(Stream compressedStream, bool leaveStreamOpen = false)
    {
        Guard.AgainstNullOrEmpty(compressedStream, nameof(compressedStream));

        if (compressedStream.Position == compressedStream.Length) { compressedStream.Seek(0, SeekOrigin.Begin); }

        var uncompressedStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen))
        {
            brotliStream.CopyTo(uncompressedStream);
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
        using (var brotliStream = new BrotliStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen))
        {
            await brotliStream
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
        using (var brotliStream = new BrotliStream(compressedData.AsStream(), CompressionMode.Decompress, false))
        {
            brotliStream.CopyTo(uncompressedStream);
        }

        return uncompressedStream;
    }
}
