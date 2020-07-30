using System.Globalization;
using System.Runtime.CompilerServices;

namespace HouseofCat.Utilities
{
    public static class Strings
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Write(string template, params string[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, template, arguments);
        }
    }
}
