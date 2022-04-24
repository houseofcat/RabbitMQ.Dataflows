using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HouseofCat.Framing
{
    public class AttachedLengthFrameStrategy : IFramingStrategy
    {
        private const int SequenceLengthSize = 4;
        private ArrayPool<byte> SharedBytePool { get; } = ArrayPool<byte>.Shared;

        public bool TryReadSequence(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> sequence)
        {
            if (buffer.Length < SequenceLengthSize)
            {
                sequence = default;
                return false;
            }

            // Read the first four bytes in order to determine length of object
            var attachedSequenceLength = BitConverter.ToInt32(buffer.Slice(0, SequenceLengthSize).ToArray(), 0);
            if (attachedSequenceLength == 0 || (SequenceLengthSize + attachedSequenceLength) > buffer.Length)
            {
                sequence = default;
                return false;
            }

            // Get a readonly sequence upto the next line terminator but not including the last one.
            sequence = buffer.Slice(SequenceLengthSize, attachedSequenceLength);
            buffer = buffer.Slice(SequenceLengthSize + attachedSequenceLength);
            return true;
        }

        // TODO: ReadOnlySequence instead of bytes??
        public async Task CreateFrameAndSendAsync(byte[] bytes, NetworkStream netStream)
        {
            var payload = SharedBytePool.Rent(bytes.Length + SequenceLengthSize);
            BitConverter.GetBytes(bytes.Length).CopyTo(payload, 0);
            bytes.CopyTo(payload, SequenceLengthSize);

            try
            {
#if NET6_0_OR_GREATER
                await netStream
                    .WriteAsync(payload.AsMemory(0, length: bytes.Length + SequenceLengthSize), default)
                    .ConfigureAwait(false);
#else
                await netStream
                    .WriteAsync(payload, 0, size: bytes.Length + SequenceLengthSize, default)
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
}
