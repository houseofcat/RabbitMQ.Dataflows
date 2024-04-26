using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HouseofCat.Utilities.Errors;

public static class Guard
{
    private static readonly string _cantBeNull = "{0} can't be null or empty.";
    private static readonly string _bothCantBeNull = "Both {0} and {1} can't be null or empty.";
    private static readonly string _cantBeTrue = "{0} can't be true for this method.";
    private static readonly string _cantBeFalse = "{0} can't be true for this method.";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstNull(object argumentValue, string argumentName)
    {
        ArgumentNullException.ThrowIfNull(argumentValue, argumentName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstEmpty<T>(ArraySegment<T> argumentValue, string argumentName)
    {
        if (argumentValue.Count == 0)
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, _cantBeNull, argumentName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstEmpty<T>(ReadOnlyMemory<T> argumentValue, string argumentName)
    {
        if (argumentValue.Length == 0)
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, _cantBeNull, argumentName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstNullOrEmpty<T>(IEnumerable<T> argumentValue, string argumentName)
    {
        if (argumentValue?.Any() == false)
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, _cantBeNull, argumentName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstNullOrEmpty(string argumentValue, string argumentName)
    {
        ArgumentException.ThrowIfNullOrEmpty(argumentValue, argumentName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstNullOrEmpty(Stream argumentValue, string argumentName)
    {
        if (argumentValue is null || argumentValue.Length == 0)
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, _cantBeNull, argumentName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstBothNullOrEmpty(string argumentValue, string argumentName, string secondArgumentValue, string secondArgumentName)
    {
        if (string.IsNullOrEmpty(argumentValue) && string.IsNullOrEmpty(secondArgumentValue))
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, _bothCantBeNull, argumentName, secondArgumentName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstTrue(bool argumentValue, string argumentName)
    {
        if (argumentValue)
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, _cantBeTrue, argumentName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstFalse(bool argumentValue, string argumentName)
    {
        if (argumentValue)
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, _cantBeFalse, argumentName));
    }
}
