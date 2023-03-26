using System;
using System.Buffers.Text;
using System.Runtime.InteropServices;

namespace HouseofCat.Utilities
{
    public static class Bytes
    {
        private const byte ForwardSlashByte = (byte)'/';
        private const byte PlusByte = (byte)'+';
        private const char Underscore = '_';
        private const char Dash = '-';

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

        public static string ConvertGuidToBytes(Guid guid)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            MemoryMarshal.TryWrite(guidBytes, ref guid); // write bytes from the Guid

            return ConvertGuidBytesToBase64Url(guidBytes);
        }

        public static string ConvertGuidBytesToBase64Url(ReadOnlySpan<byte> data)
        {
            Span<byte> encodedBytes = stackalloc byte[24];
            Base64.EncodeToUtf8(data, encodedBytes, out _, out _);
            Span<char> chars = stackalloc char[22];
            // replace any characters which are not URL safe
            // skip the final two bytes as these will be '==' padding we don't need
            for (var i = 0; i < 22; i++)
            {
                chars[i] = encodedBytes[i] switch
                {
                    ForwardSlashByte => Underscore,
                    PlusByte => Dash,
                    _ => (char)encodedBytes[i]
                };
            }

            return new string(chars);
        }
    }
}
