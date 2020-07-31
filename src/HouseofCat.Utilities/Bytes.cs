using System;

namespace HouseofCat.Utilities
{
    public static class Bytes
    {
        public readonly static byte[] Utf8JsonStartsWith = new byte[] { (byte)'{' };
        public readonly static byte[] Utf8JsonEndsWith = new byte[] { (byte)'}' };

        public readonly static byte[] Utf8JsonArrayStartsWith = new byte[] { (byte)'[' };
        public readonly static byte[] Utf8JsonArrayEndsWith = new byte[] { (byte)']' };

        public static bool IsJson(ReadOnlySpan<byte> data)
        {
            return
            // Json
            data.StartsWith(Utf8JsonStartsWith) && data.EndsWith(Utf8JsonEndsWith);
        }

        public static bool IsJsonArray(ReadOnlySpan<byte> data)
        {
            return
            // JsonArray
            data.StartsWith(Utf8JsonArrayStartsWith) && data.StartsWith(Utf8JsonArrayEndsWith);
        }
    }
}
