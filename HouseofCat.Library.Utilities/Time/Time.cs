using System;
using System.Globalization;

namespace HouseofCat.Library.Utilities.Time
{
    public static class Time
    {
        public static string GetDateTimeNow(string format) => DateTime.Now.ToString(format, DateTimeFormatInfo.InvariantInfo);
        public static string GetDateTimeUtcNow(string format) => DateTime.UtcNow.ToString(format, DateTimeFormatInfo.InvariantInfo);

        public static class Formats
        {
            public const string CatsPreferredFormat = "MM/dd/yyyy HH:mm:ss.fff zzz";
            public const string CatsAltFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";
            public const string CatRFC3339 = "yyyy-MM-dd HH:mm:ss.fffzzz";
            public const string RFC3339Long = "yyyy-MM-dd'T'HH:mm:ss.ffffffK";
            public const string RFC3339Short = "yyyy-MM-dd'T'HH:mm:ss.fffK";
            public const string RFC3339Shortest = "yyyy-MM-dd'T'HH:mm:ssK";
        }
    }
}
