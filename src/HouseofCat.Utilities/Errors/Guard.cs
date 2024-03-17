using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HouseofCat.Utilities.Errors;

public static class Guard
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstNull(object argumentValue, string argumentName)
    {
        ArgumentNullException.ThrowIfNull(argumentValue, argumentName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstEmpty<T>(ArraySegment<T> argumentValue, string argumentName)
    {
        if (argumentValue.Count == 0)
            throw new ArgumentException(Strings.Write(Constants.Guards.CantBeNull, argumentName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstEmpty<T>(ReadOnlyMemory<T> argumentValue, string argumentName)
    {
        if (argumentValue.Length == 0)
            throw new ArgumentException(Strings.Write(Constants.Guards.CantBeNull, argumentName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstNullOrEmpty<T>(IEnumerable<T> argumentValue, string argumentName)
    {
        if (argumentValue?.Any() == false)
            throw new ArgumentException(Strings.Write(Constants.Guards.CantBeNull, argumentName));
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
            throw new ArgumentException(Strings.Write(Constants.Guards.CantBeNull, argumentName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstBothNullOrEmpty(string argumentValue, string argumentName, string secondArgumentValue, string secondArgumentName)
    {
        if (string.IsNullOrEmpty(argumentValue) && string.IsNullOrEmpty(secondArgumentValue))
            throw new ArgumentException(Strings.Write(Constants.Guards.BothCantBeNull, argumentName, secondArgumentName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstTrue(bool argumentValue, string argumentName)
    {
        if (argumentValue)
            throw new ArgumentException(Strings.Write(Constants.Guards.CantBeTrue, argumentName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AgainstFalse(bool argumentValue, string argumentName)
    {
        if (argumentValue)
            throw new ArgumentException(Strings.Write(Constants.Guards.CantBeFalse, argumentName));
    }
}
