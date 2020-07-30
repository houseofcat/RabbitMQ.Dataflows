using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HouseofCat.Library.Utilities.Errors
{
    public static class Guard
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AgainstNull(object argumentValue, string argumentName)
        {
            if (argumentValue is null)
                throw new ArgumentNullException(Strings.FormatWrite(Constants.Guard.CantBeNull, argumentName));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AgainstNullOrEmpty(string argumentValue, string argumentName)
        {
            if (string.IsNullOrEmpty(argumentValue))
                throw new ArgumentException(Strings.FormatWrite(Constants.Guard.CantBeNull, argumentName));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AgainstBothNullOrEmpty(string argumentValue, string argumentName, string secondArgumentValue, string secondArgumentName)
        {
            if (string.IsNullOrEmpty(argumentValue) && string.IsNullOrEmpty(secondArgumentValue))
                throw new ArgumentException(Strings.FormatWrite(Constants.Guard.BothCantBeNull, argumentName, secondArgumentName));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AgainstNullOrEmpty<T>(IEnumerable<T> argumentValue, string argumentName)
        {
            if (argumentValue?.Any() != true)
                throw new ArgumentNullException(Strings.FormatWrite(Constants.Guard.CantBeNull, argumentName));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AgainstTrue(bool argumentValue, string argumentName)
        {
            if (argumentValue)
                throw new ArgumentException(Strings.FormatWrite(Constants.Guard.CantBeTrue, argumentName));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AgainstFalse(bool argumentValue, string argumentName)
        {
            if (argumentValue)
                throw new ArgumentException(Strings.FormatWrite(Constants.Guard.CantBeFalse, argumentName));
        }
    }
}
