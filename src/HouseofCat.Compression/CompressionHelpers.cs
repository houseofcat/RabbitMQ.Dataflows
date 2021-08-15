using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace HouseofCat.Compression
{
    public static class CompressionHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetGzipUncompressedLength(ReadOnlyMemory<byte> compressedData)
        {
            // Anticipate the uncompressed length of GZip to get adequate sized initial buffers.
            return BitConverter.ToInt32(compressedData.Slice(compressedData.Length - 4, 4).Span);
        }

        // RFC GZIP Last 8 Bytes
        // https://datatracker.ietf.org/doc/html/rfc1952#section-2.2
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
    }
}
