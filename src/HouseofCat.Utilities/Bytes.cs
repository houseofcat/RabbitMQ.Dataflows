using System;

namespace HouseofCat.Utilities;

public static class Bytes
{
    private readonly static byte[] _utf8JsonStartsWith = "{"u8.ToArray();
    private readonly static byte[] _utf8JsonEndsWith = "}"u8.ToArray();

    private readonly static byte[] _utf8JsonArrayStartsWith = "["u8.ToArray();
    private readonly static byte[] _utf8JsonArrayEndsWith = "]"u8.ToArray();

    public static bool IsJson(ReadOnlySpan<byte> data)
    {
        return data.StartsWith(_utf8JsonStartsWith)
            && data.EndsWith(_utf8JsonEndsWith);
    }

    public static bool IsJsonArray(ReadOnlySpan<byte> data)
    {
        return data.StartsWith(_utf8JsonArrayStartsWith)
            && data.StartsWith(_utf8JsonArrayEndsWith);
    }
}
