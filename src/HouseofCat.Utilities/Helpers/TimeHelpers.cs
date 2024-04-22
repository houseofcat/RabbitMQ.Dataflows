using System;
using System.Globalization;

namespace HouseofCat.Utilities.Helpers;

public static class TimeHelpers
{
    public static string GetDateTimeNow(string format) => DateTime.Now.ToString(format, DateTimeFormatInfo.CurrentInfo);
    public static string GetDateTimeUtcNow(string format) => DateTime.UtcNow.ToString(format, DateTimeFormatInfo.CurrentInfo);

    public static class Formats
    {
        public static string CatsPreferredFormat { get; set; } = "MM/dd/yyyy HH:mm:ss.fff zzz";
        public static string CatsAltFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.ffffff zzz";
        public static string CatRFC3339 { get; set; } = "yyyy-MM-dd HH:mm:ss.fffzzz";
        public static string RFC3339Long { get; set; } = "yyyy-MM-dd'T'HH:mm:ss.ffffffK";
        public static string RFC3339Short { get; set; } = "yyyy-MM-dd'T'HH:mm:ss.fffK";
        public static string RFC3339Shortest { get; set; } = "yyyy-MM-dd'T'HH:mm:ssK";
    }
}
