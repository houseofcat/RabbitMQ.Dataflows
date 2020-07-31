using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RabbitMQ.Core.Prototype
{
    internal ref struct CustomBufferWriter<T> where T : IBufferWriter<byte>
    {
        private T _output;
        private Span<byte> _span;
        private int _buffered;
        private long _bytesCommitted;

        public CustomBufferWriter(T output)
        {
            _buffered = 0;
            _bytesCommitted = 0;
            _output = output;
            _span = output.GetSpan();
        }

        public Span<byte> Span => _span;

        public long BytesCommitted => _bytesCommitted;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Commit()
        {
            var buffered = _buffered;
            if (buffered > 0)
            {
                _bytesCommitted += buffered;
                _buffered = 0;
                _output.Advance(buffered);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            _buffered += count;
            _span = _span.Slice(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> source)
        {
            if (_span.Length >= source.Length)
            {
                source.CopyTo(_span);
                Advance(source.Length);
            }
            else
            {
                WriteMultiBuffer(source);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            Ensure(sizeof(byte));
            MemoryMarshal.Write(_span, ref value);
            Advance(sizeof(byte));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(sbyte value)
        {
            Ensure(sizeof(sbyte));
            MemoryMarshal.Write(_span, ref value);
            Advance(sizeof(sbyte));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(short value)
        {
            Ensure(sizeof(short));
            BinaryPrimitives.WriteInt16BigEndian(_span, value);
            Advance(sizeof(short));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ushort value)
        {
            Ensure(sizeof(ushort));
            BinaryPrimitives.WriteUInt16BigEndian(_span, value);
            Advance(sizeof(ushort));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value)
        {
            Ensure(sizeof(int));
            BinaryPrimitives.WriteInt32BigEndian(_span, value);
            Advance(sizeof(int));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value)
        {
            Ensure(sizeof(uint));
            BinaryPrimitives.WriteUInt32BigEndian(_span, value);
            Advance(sizeof(uint));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(long value)
        {
            Ensure(sizeof(long));
            BinaryPrimitives.WriteInt64BigEndian(_span, value);
            Advance(sizeof(long));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong value)
        {
            Ensure(sizeof(ulong));
            BinaryPrimitives.WriteUInt64BigEndian(_span, value);
            Advance(sizeof(ulong));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(float value)
        {
            var span = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
            var bytesSpan = MemoryMarshal.AsBytes(span);

            Write(MemoryMarshal.Read<uint>(bytesSpan));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(double value)
        {
            var span = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
            var bytesSpan = MemoryMarshal.AsBytes(span);

            Write(MemoryMarshal.Read<ulong>(bytesSpan));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ensure(int count = 1)
        {
            if (_span.Length < count)
            {
                EnsureMore(count);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureMore(int count = 0)
        {
            if (_buffered > 0)
            {
                Commit();
            }

            _output.GetMemory(count);
            _span = _output.GetSpan();
        }

        private void WriteMultiBuffer(ReadOnlySpan<byte> source)
        {
            while (source.Length > 0)
            {
                if (_span.Length == 0)
                {
                    EnsureMore();
                }

                var writable = Math.Min(source.Length, _span.Length);
                source.Slice(0, writable).CopyTo(_span);
                source = source.Slice(writable);
                Advance(writable);
            }
        }
    }
}
