using CommunityToolkit.HighPerformance;
using HouseofCat.Utilities.Errors;
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static HouseofCat.Compression.Enums;

namespace HouseofCat.Compression;

public static class CompressionHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetGzipUncompressedLength(ReadOnlyMemory<byte> compressedInput)
    {
        // Anticipate the uncompressed length of GZip to get adequate sized initial buffers.
        return BitConverter.ToInt32(compressedInput.Slice(compressedInput.Length - 4, 4).Span);
    }

    // RFC GZIP Last 8 Bytes
    // https://datatracker.ietf.org/doc/html/rfc1952
    // [ 0 , 1 , 2 , 3 , 4 , 5 , 6 , 7 ]
    // +---+---+---+---+---+---+---+---+
    // |     CRC32     |     ISIZE     |
    // +---+---+---+---+---+---+---+---+
    //
    // ISIZE - This contains the size of the original (uncompressed) input data modulo 2^32.
    // Due to Little Endian format of ISIZE, its better to mentally re-arrange the bytes.
    // ex.) [ 3, 2, 1, 0 ]
    // Viable strategies for reading this value with respect to little endian byte ordering:
    // var length = ([3] << 24) | ([2] << 24) + ([1] << 8) + [0];
    // var length ((((([3] << 8) | [2]) << 8) | [1]) << 8) | [0];
    // var length = BitConverter.ToInt32(lastFour); // not sure why this works, must internally check
    // UnitTests added.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetGzipUncompressedLength(Stream stream)
    {
        // Anticipate the uncompressed length of GZip to get adequate sized initial buffers.
        Span<byte> uncompressedLength = stackalloc byte[4];
        stream.Position = stream.Length - 4;
        stream.Read(uncompressedLength);
        stream.Seek(0, SeekOrigin.Begin);
        return BitConverter.ToInt32(uncompressedLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Stream GetCompressionStream(
        CompressionType type,
        Stream stream,
        CompressionLevel compressionLevel = CompressionLevel.Optimal,
        bool leaveOpen = false)
    {
        return type switch
        {
            CompressionType.Gzip => new GZipStream(stream, compressionLevel, leaveOpen),
            CompressionType.Deflate => new DeflateStream(stream, compressionLevel, leaveOpen),
            CompressionType.Brotli => new BrotliStream(stream, compressionLevel, leaveOpen),
            _ => throw new NotImplementedException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Stream GetDecompressionStream(
        CompressionType type,
        Stream stream,
        bool leaveOpen = false)
    {
        return type switch
        {
            CompressionType.Gzip => new GZipStream(stream, CompressionMode.Decompress, leaveOpen),
            CompressionType.Deflate => new DeflateStream(stream, CompressionMode.Decompress, leaveOpen),
            CompressionType.Brotli => new BrotliStream(stream, CompressionMode.Decompress, leaveOpen),
            _ => throw new NotImplementedException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyMemory<byte> Compress(
        ReadOnlyMemory<byte> input,
        CompressionType compressionType,
        CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        Guard.AgainstEmpty(input, nameof(input));

        using var compressedStream = new MemoryStream();
        using (var compressionStream = GetCompressionStream(compressionType, compressedStream, compressionLevel, false))
        {
            compressionStream.Write(input.Span);
        }

        return compressedStream.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<MemoryStream> CompressAsync(
        Stream inputStream,
        CompressionType compressionType,
        CompressionLevel compressionLevel = CompressionLevel.Optimal,
        bool leaveOpen = false)
    {
        Guard.AgainstNullOrEmpty(inputStream, nameof(inputStream));

        if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

        var compressedStream = new MemoryStream();
        using (var compressionStream = GetCompressionStream(compressionType, compressedStream, compressionLevel, true))
        {
            await inputStream
                .CopyToAsync(compressionStream)
                .ConfigureAwait(false);
        }

        if (!leaveOpen) { inputStream.Close(); }

        compressedStream.Seek(0, SeekOrigin.Begin);

        return compressedStream;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyMemory<byte> Decompress(
        ReadOnlyMemory<byte> compressedInput,
        CompressionType compressionType)
    {
        Guard.AgainstEmpty(compressedInput, nameof(compressedInput));

        using var decompressedStream = GetRightSizedMemoryStream(compressedInput, compressionType);
        using (var decompressionStream = GetDecompressionStream(compressionType, compressedInput.AsStream(), false))
        {
            decompressionStream.CopyTo(decompressedStream);
        }

        return decompressedStream.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<MemoryStream> DecompressAsync(
        Stream compressedStream,
        CompressionType compressionType)
    {
        Guard.AgainstNullOrEmpty(compressedStream, nameof(compressedStream));

        using var decompressedStream = GetRightSizedMemoryStream(compressedStream, compressionType);
        using (var decompressionStream = GetDecompressionStream(compressionType, compressedStream, false))
        {
            await decompressionStream.CopyToAsync(decompressedStream);
        }

        return decompressedStream;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryStream GetRightSizedMemoryStream(ReadOnlyMemory<byte> compressedInput, CompressionType compressionType)
    {
        return compressionType switch
        {
            CompressionType.Gzip => new MemoryStream(GetGzipUncompressedLength(compressedInput)),
            CompressionType.Deflate => new MemoryStream(),
            CompressionType.Brotli => new MemoryStream(),
            _ => throw new NotImplementedException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryStream GetRightSizedMemoryStream(Stream compressedStream, CompressionType compressionType)
    {
        return compressionType switch
        {
            CompressionType.Gzip => new MemoryStream(GetGzipUncompressedLength(compressedStream)),
            CompressionType.Deflate => new MemoryStream(),
            CompressionType.Brotli => new MemoryStream(),
            _ => throw new NotImplementedException()
        };
    }
}
