using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HouseofCat.Framing;

public class TerminatedByteFrameStrategy : IFramingStrategy
{
    private const byte TerminatingByte = (byte)'\n';
    private ArrayPool<byte> SharedBytePool { get; } = ArrayPool<byte>.Shared;

    public bool TryReadSequence(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> sequence)
    {
        SequencePosition? position = buffer.PositionOf(TerminatingByte);

        // If terminating character is not found, exit false.
        if (position == null)
        {
            sequence = default;
            return false;
        }

        // Get a readonly sequence upto the next line terminator but not including the last one.
        sequence = buffer.Slice(0, position.Value);
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

        return true;
    }

    // TODO: ReadOnlySequence instead of bytes??
    public async Task CreateFrameAndSendAsync(byte[] bytes, NetworkStream netStream)
    {
        var payload = SharedBytePool.Rent(bytes.Length + 1);
        bytes.CopyTo(payload, 0);
        payload[bytes.Length] = TerminatingByte;

        try
        {
#if NET6_0_OR_GREATER
            await netStream
                .WriteAsync(payload.AsMemory(0, length: bytes.Length + 1), default)
                .ConfigureAwait(false);
#else
            await netStream
                .WriteAsync(payload, 0, size: bytes.Length + 1, default)
                .ConfigureAwait(false);
#endif
        }
        finally
        {
            if (payload != null)
            {
                SharedBytePool.Return(payload);
            }
        }
    }
}
