using System;

namespace HouseofCat.Utilities.Random;

// Fast but flawed implementation, can only generate divisible by 4 sets of data in Unsafe.
public class XorShift
{
    private System.Random Rand { get; } = new System.Random();

    private uint X { get; set; }
    private uint Y { get; set; }
    private uint Z { get; set; }
    private uint W { get; set; }

    private const int Mask = 0xFF;

    public XorShift()
    {
        X = 123456789;
        Y = 362436069;
        Z = 521288629;
        W = 88675123;
    }

    public XorShift(bool reseed)
    {
        if (reseed)
        {
            var buffer = new byte[sizeof(uint)];
            Rand.NextBytes(buffer);
            X = BitConverter.ToUInt32(buffer);

            buffer = new byte[sizeof(uint)];
            Rand.NextBytes(buffer);
            Y = BitConverter.ToUInt32(buffer);

            buffer = new byte[sizeof(uint)];
            Rand.NextBytes(buffer);
            Z = BitConverter.ToUInt32(buffer);

            buffer = new byte[sizeof(uint)];
            Rand.NextBytes(buffer);
            W = BitConverter.ToUInt32(buffer);
        }
    }

    public byte[] GetRandomBytes(int size)
    {
        var buffer = new byte[size];
        FillBuffer(buffer, 0, size);
        return buffer;
    }

    public byte[] UnsafeGetRandomBytes(int size)
    {
        var buffer = new byte[size];
        UnsafeFillBuffer(buffer, 0, size);
        return buffer;
    }

    public void FillBuffer(byte[] buffer)
    {
        uint offset = 0, offsetEnd = (uint)buffer.Length;
        while (offset < offsetEnd)
        {
            uint t = X ^ (X << 11);
            X = Y; Y = Z; Z = W;
            W = W ^ (W >> 19) ^ (t ^ (t >> 8));

            if (offset < offsetEnd)
            {
                buffer[offset++] = (byte)(W & Mask);
                buffer[offset++] = (byte)((W >> 8) & Mask);
                buffer[offset++] = (byte)((W >> 16) & Mask);
                buffer[offset++] = (byte)((W >> 24) & Mask);
            }
            else { break; }
        }
    }

    public void FillBuffer(byte[] buffer, int offset, int offsetEnd)
    {
        while (offset < offsetEnd)
        {
            uint t = X ^ (X << 11);
            X = Y; Y = Z; Z = W;
            W = W ^ (W >> 19) ^ (t ^ (t >> 8));

            if (offset < offsetEnd)
            {
                buffer[offset++] = (byte)(W & Mask);
                buffer[offset++] = (byte)((W >> 8) & Mask);
                buffer[offset++] = (byte)((W >> 16) & Mask);
                buffer[offset++] = (byte)((W >> 24) & Mask);
            }
            else { break; }
        }
    }

    public unsafe void UnsafeFillBuffer(byte[] buf)
    {
        uint x = X, y = Y, z = Z, w = W;
        fixed (byte* pbytes = buf)
        {
            uint* pbuf = (uint*)(pbytes + 0);
            uint* pend = (uint*)(pbytes + buf.Length);
            while (pbuf < pend)
            {
                uint tx = x ^ (x << 11);
                uint ty = y ^ (y << 11);
                uint tz = z ^ (z << 11);
                uint tw = w ^ (w << 11);
                *(pbuf++) = x = w ^ (w >> 19) ^ (tx ^ (tx >> 8));
                *(pbuf++) = y = x ^ (x >> 19) ^ (ty ^ (ty >> 8));
                *(pbuf++) = z = y ^ (y >> 19) ^ (tz ^ (tz >> 8));
                *(pbuf++) = w = z ^ (z >> 19) ^ (tw ^ (tw >> 8));
            }
        }
        X = x; Y = y; Z = z; W = w;
    }

    public unsafe void UnsafeFillBuffer(byte[] buf, int offset, int offsetEnd)
    {
        uint x = X, y = Y, z = Z, w = W;
        fixed (byte* pbytes = buf)
        {
            uint* pbuf = (uint*)(pbytes + offset);
            uint* pend = (uint*)(pbytes + offsetEnd);
            while (pbuf < pend)
            {
                uint tx = x ^ (x << 11);
                uint ty = y ^ (y << 11);
                uint tz = z ^ (z << 11);
                uint tw = w ^ (w << 11);
                *(pbuf++) = x = w ^ (w >> 19) ^ (tx ^ (tx >> 8));
                *(pbuf++) = y = x ^ (x >> 19) ^ (ty ^ (ty >> 8));
                *(pbuf++) = z = y ^ (y >> 19) ^ (tz ^ (tz >> 8));
                *(pbuf++) = w = z ^ (z >> 19) ^ (tw ^ (tw >> 8));
            }
        }
        X = x; Y = y; Z = z; W = w;
    }
}
