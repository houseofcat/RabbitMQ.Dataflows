﻿using System.Threading.Tasks;

namespace HouseofCat.Utilities.Random;

/// <summary>
/// Static class for generating filler (random) data for users and Tests.
/// </summary>
public static class Data
{
    private static readonly System.Random Rand = new System.Random();
    private const string AllowedChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!@#$%^&*()_-+=";

    /// <summary>
    /// Random asynchronous string generator.
    /// </summary>
    /// <param name="minLength"></param>
    /// <param name="maxLength"></param>
    public static async Task<string> RandomStringAsync(int minLength, int maxLength)
    {
        return await Task.Run(
            () =>
            {
                var chars = new char[maxLength];
                var setLength = AllowedChars.Length;
                var length = Rand.Next(minLength, maxLength + 1);

                for (var i = 0; i < length; ++i)
                {
                    chars[i] = AllowedChars[Rand.Next(setLength)];
                }

                return new string(chars, 0, length);
            })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Random string generator.
    /// </summary>
    /// <param name="minLength"></param>
    /// <param name="maxLength"></param>
    public static string RandomString(int minLength, int maxLength)
    {
        var chars = new char[maxLength];
        var setLength = AllowedChars.Length;
        var length = Rand.Next(minLength, maxLength + 1);

        for (var i = 0; i < length; ++i)
        {
            chars[i] = AllowedChars[Rand.Next(setLength)];
        }

        return new string(chars, 0, length);
    }

    public static int[] CreateRandomInts(int count, int maxValue = int.MaxValue)
    {
        var ints = new int[count];
        var validatedInt = ((maxValue < 2 ? 2 : maxValue) / 2) - 1;

        for (var i = 0; i < count; i++)
        {
            ints[i] = Rand.Next(validatedInt) - Rand.Next(validatedInt);
        }

        return ints;
    }
}
