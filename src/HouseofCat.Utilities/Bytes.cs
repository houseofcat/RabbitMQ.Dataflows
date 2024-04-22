using System;

namespace HouseofCat.Utilities;

public static class Bytes
{
    public readonly static byte[] Utf8JsonStartsWith = "{"u8.ToArray();
    public readonly static byte[] Utf8JsonEndsWith = "}"u8.ToArray();

    public readonly static byte[] Utf8JsonArrayStartsWith = "["u8.ToArray();
    public readonly static byte[] Utf8JsonArrayEndsWith = "]"u8.ToArray();

    public static bool IsJson(ReadOnlySpan<byte> data)
    {
        return data.StartsWith(Utf8JsonStartsWith)
            && data.EndsWith(Utf8JsonEndsWith);
    }

    public static bool IsJsonArray(ReadOnlySpan<byte> data)
    {
        return data.StartsWith(Utf8JsonArrayStartsWith)
            && data.StartsWith(Utf8JsonArrayEndsWith);
    }
}
