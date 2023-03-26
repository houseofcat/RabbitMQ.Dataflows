using System;
using System.Runtime.InteropServices;

namespace HouseofCat.Utilities;

public static class GuidExtensions
{
    public static string ConvertToBase64Url(this Guid guid)
    {
        Span<byte> guidBytes = stackalloc byte[16];
        MemoryMarshal.TryWrite(guidBytes, ref guid);

        return Bytes.ConvertGuidBytesToBase64Url(guidBytes);
    }
}