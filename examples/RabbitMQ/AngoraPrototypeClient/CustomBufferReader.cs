using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RabbitMQ.Core.Prototype
{
    internal ref struct CustomBufferReader
    {
        private ReadOnlySpan<byte> _currentSpan;
        private int _index;

        private ReadOnlySequence<byte> _sequence;
        private SequencePosition _currentSequencePosition;
        private SequencePosition _nextSequencePosition;

        private int _consumedBytes;
        private bool _end;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CustomBufferReader(in ReadOnlySequence<byte> buffer)
        {
            _index = 0;
            _consumedBytes = 0;
            _sequence = buffer;
            _currentSequencePosition = _sequence.Start;
            _nextSequencePosition = _currentSequencePosition;

            if (_sequence.TryGet(ref _nextSequencePosition, out var memory, true))
            {
                _end = false;
                _currentSpan = memory.Span;
                if (_currentSpan.Length == 0)
                {
                    // No space in first span, move to one with space
                    MoveNext();
                }
            }
            else
            {
                // No space in any spans and at end of sequence
                _end = true;
                _currentSpan = default;
            }
        }

        public bool End => _end;

        public int CurrentSegmentIndex => _index;

        public SequencePosition Position => _sequence.GetPosition(_index, _currentSequencePosition);

        public ReadOnlySpan<byte> CurrentSegment => _currentSpan;

        public ReadOnlySpan<byte> UnreadSegment => _currentSpan.Slice(_index);

        public int ConsumedBytes => _consumedBytes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Peek()
        {
            if (_end)
            {
                return -1;
            }
            return _currentSpan[_index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read()
        {
            if (_end)
            {
                return -1;
            }

            var value = _currentSpan[_index];
            _index++;
            _consumedBytes++;

            if (_index >= _currentSpan.Length)
            {
                MoveNext();
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            if (_end)
            {
                return 0; // TODO change this to throw instead?
            }

            var value = _currentSpan[_index];
            _index++;
            _consumedBytes++;

            if (_index >= _currentSpan.Length)
            {
                MoveNext();
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte()
        {
            if (_end)
            {
                return 0; // TODO change this to throw instead?
            }

            var value = _currentSpan[_index];
            _index++;
            _consumedBytes++;

            if (_index >= _currentSpan.Length)
            {
                MoveNext();
            }

            return (sbyte)value; //TODO check this cast
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ReadBytes(int length)
        {
            if (_end)
            {
                return default; // TODO change this to throw instead?
            }

            if (_index + length > _currentSpan.Length)
            {
                var bytes = new byte[length];

                for (int i = 0; i < length; i++)
                {
                    bytes[i] = ReadByte();
                }

                return bytes;
            }

            var value = _currentSpan.Slice(_index, length);

            _index += length;
            _consumedBytes += length;

            if (_index >= _currentSpan.Length)
            {
                MoveNext();
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            Span<byte> bytes = stackalloc byte[sizeof(short)];

            for (int i = 0; i < sizeof(short); i++)
            {
                bytes[i] = ReadByte();
            }

            BinaryPrimitives.TryReadInt16BigEndian(bytes, out var value);

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            Span<byte> bytes = stackalloc byte[sizeof(ushort)];

            for (int i = 0; i < sizeof(ushort); i++)
            {
                bytes[i] = ReadByte();
            }

            BinaryPrimitives.TryReadUInt16BigEndian(bytes, out var value);

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];

            for (int i = 0; i < sizeof(int); i++)
            {
                bytes[i] = ReadByte();
            }

            BinaryPrimitives.TryReadInt32BigEndian(bytes, out var value);

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            Span<byte> bytes = stackalloc byte[sizeof(uint)];

            for (int i = 0; i < sizeof(uint); i++)
            {
                bytes[i] = ReadByte();
            }

            BinaryPrimitives.TryReadUInt32BigEndian(bytes, out var value);

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            Span<byte> bytes = stackalloc byte[sizeof(long)];

            for (int i = 0; i < sizeof(long); i++)
            {
                bytes[i] = ReadByte();
            }

            BinaryPrimitives.TryReadInt64BigEndian(bytes, out var value);

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            Span<byte> bytes = stackalloc byte[sizeof(ulong)];

            for (int i = 0; i < sizeof(ulong); i++)
            {
                bytes[i] = ReadByte();
            }

            BinaryPrimitives.TryReadUInt64BigEndian(bytes, out var value);

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloat()
        {
            var value = ReadUInt32();
            var span = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
            var bytesSpan = MemoryMarshal.AsBytes(span);

            return MemoryMarshal.Read<float>(bytesSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            var value = ReadUInt64();
            var span = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
            var bytesSpan = MemoryMarshal.AsBytes(span);

            return MemoryMarshal.Read<double>(bytesSpan);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void MoveNext()
        {
            var previous = _nextSequencePosition;
            while (_sequence.TryGet(ref _nextSequencePosition, out var memory, true))
            {
                _currentSequencePosition = previous;
                _currentSpan = memory.Span;
                _index = 0;
                if (_currentSpan.Length > 0)
                {
                    return;
                }
            }
            _end = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int byteCount)
        {
            if (!_end && byteCount > 0 && (_index + byteCount) < _currentSpan.Length)
            {
                _consumedBytes += byteCount;
                _index += byteCount;
            }
            else
            {
                AdvanceNext(byteCount);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AdvanceNext(int byteCount)
        {
            if (byteCount < 0)
            {
                BuffersThrowHelper.ThrowArgumentOutOfRangeException(BuffersThrowHelper.ExceptionArgument.length);
            }

            _consumedBytes += byteCount;

            while (!_end && byteCount > 0)
            {
                if ((_index + byteCount) < _currentSpan.Length)
                {
                    _index += byteCount;
                    byteCount = 0;
                    break;
                }

                var remaining = (_currentSpan.Length - _index);

                _index += remaining;
                byteCount -= remaining;

                MoveNext();
            }

            if (byteCount > 0)
            {
                BuffersThrowHelper.ThrowArgumentOutOfRangeException(BuffersThrowHelper.ExceptionArgument.length);
            }
        }
    }

    internal static class BuffersThrowHelper
    {
        public static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
        {
            throw GetArgumentOutOfRangeException(argument);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument)
        {
            return new ArgumentOutOfRangeException(GetArgumentName(argument));
        }

        private static string GetArgumentName(ExceptionArgument argument)
        {
            Debug.Assert(Enum.IsDefined(typeof(ExceptionArgument), argument), "The enum value is not defined, please check the ExceptionArgument Enum.");

            return argument.ToString();
        }

        internal enum ExceptionArgument
        {
            length,
        }
    }
}
