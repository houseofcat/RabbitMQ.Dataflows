using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HouseofCat.Utilities.Extensions;

public static class MemoryStreamExtensions
{
    private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    /// <summary>
    /// Reset sets the Streams position and attempts to get the underlying Streams buffer. On failure, it rents a byte buffer from shared <c>ArrayPool&lt;byte&gt;</c> and indicates if
    /// the caller needs to also return it.
    /// <remarks>Throws an exception if manually transferring bytes to rented buffer and no bytes were read.</remarks>
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (ArraySegment<byte>, bool) GetSafeBuffer(this MemoryStream stream)
    {
        if (stream.Position == stream.Length) { stream.Seek(0, SeekOrigin.Begin); }

        if (stream.TryGetBuffer(out ArraySegment<byte> unencryptedBuffer)) // try and use a Stream's buffer if it exists, first.
        {
            return (unencryptedBuffer, false);
        }

        unencryptedBuffer = _pool.Rent((int)stream.Length); // otherwise rent a temp buffer to read the stream into
        var bytesRead = stream.Read(unencryptedBuffer);

        if (bytesRead == 0) throw new InvalidDataException();

        return (unencryptedBuffer, true);
    }

    /// <summary>
    /// Reset sets the Streams position and attempts to get the underlying Streams buffer. On failure, it rents a byte buffer from shared <c>ArrayPool&lt;byte&gt;</c> and indicates if
    /// the caller needs to also return it</c>.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (ArraySegment<byte>, bool) GetSafeBuffer(this MemoryStream stream, int length)
    {
        if (stream.Position == stream.Length) { stream.Seek(0, SeekOrigin.Begin); }

        if (stream.TryGetBuffer(out ArraySegment<byte> unencryptedBuffer)) // try and use a Stream's buffer if it exists, first.
        {
            return (unencryptedBuffer, false);
        }

        unencryptedBuffer = _pool.Rent(length); // otherwise rent a temp buffer to read the stream into
        var bytesRead = stream.Read(unencryptedBuffer);

        if (bytesRead == 0) throw new InvalidDataException();

        return (unencryptedBuffer, true);
    }

    /// <summary>
    /// Reset sets the Streams position and attempts to get the underlying Streams buffer. On failure, it rents a byte buffer from shared <c>ArrayPool&lt;byte&gt;</c> and indicates if
    /// the caller needs to also return it.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<(ArraySegment<byte>, bool)> GetSafeBufferAsync(this MemoryStream stream)
    {
        if (stream.Position == stream.Length) { stream.Seek(0, SeekOrigin.Begin); }

        if (stream.TryGetBuffer(out ArraySegment<byte> unencryptedBuffer)) // try and use a Stream's buffer if it exists, first.
        {
            return (unencryptedBuffer, false);
        }

        unencryptedBuffer = _pool.Rent((int)stream.Length); // otherwise rent a temp buffer to read the stream into
        var bytesRead = await stream.ReadAsync(unencryptedBuffer).ConfigureAwait(false);

        if (bytesRead == 0) throw new InvalidDataException();

        return (unencryptedBuffer, true);
    }

    /// <summary>
    /// Reset sets the Streams position and attempts to get the underlying Streams buffer. On failure, it rents a byte buffer from shared <c>ArrayPool&lt;byte&gt;</c> and indicates if
    /// the caller needs to also return it.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<(ArraySegment<byte>, bool)> GetSafeBufferAsync(this MemoryStream stream, int length)
    {
        if (stream.Position == stream.Length) { stream.Seek(0, SeekOrigin.Begin); }

        if (stream.TryGetBuffer(out ArraySegment<byte> unencryptedBuffer)) // try and use a Stream's buffer if it exists, first.
        {
            return (unencryptedBuffer, false);
        }

        unencryptedBuffer = _pool.Rent(length); // otherwise rent a temp buffer to read the stream into
        var bytesRead = await stream.ReadAsync(unencryptedBuffer).ConfigureAwait(false);

        if (bytesRead == 0) throw new InvalidDataException();

        return (unencryptedBuffer, true);
    }

    public static void ReturnBuffer(this MemoryStream _, ArraySegment<byte> buffer)
    {
        _pool.Return(buffer.Array);
    }
}
