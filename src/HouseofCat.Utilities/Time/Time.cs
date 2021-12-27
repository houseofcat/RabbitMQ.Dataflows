using System;
using System.Globalization;

namespace HouseofCat.Utilities.Time
{
    public static class Time
    {
        public static string GetDateTimeNow(string format) => DateTime.Now.ToString(format, DateTimeFormatInfo.InvariantInfo);
        public static string GetDateTimeUtcNow(string format) => DateTime.UtcNow.ToString(format, DateTimeFormatInfo.InvariantInfo);

        public static class Formats
        {
            public static string CatsPreferredFormat = "MM/dd/yyyy HH:mm:ss.fff zzz";
            public static string CatsAltFormat = "yyyy-MM-dd HH:mm:ss.ffffff zzz";
            public static string CatRFC3339 = "yyyy-MM-dd HH:mm:ss.fffzzz";
            public static string RFC3339Long = "yyyy-MM-dd'T'HH:mm:ss.ffffffK";
            public static string RFC3339Short = "yyyy-MM-dd'T'HH:mm:ss.fffK";
            public static string RFC3339Shortest = "yyyy-MM-dd'T'HH:mm:ssK";
        }
    }
}
