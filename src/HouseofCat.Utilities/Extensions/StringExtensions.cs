using System;

namespace HouseofCat.Utilities.Extensions;

public static class StringExtensions
{
    private static readonly string _charSet = "abcdefghijklmnopqrstuvwxyz0123456789";

    public static string RandomAlphaNumericChars(int numChars)
    {
        Span<char> buffer = numChars <= 1024
            ? stackalloc char[numChars]
            : new char[numChars];

        for (var i = 0; i < numChars; i++)
        {
            buffer[i] = _charSet[System.Random.Shared.Next(_charSet.Length)];
        }

        return new string(buffer);
    }

    public static int CountWordsInText(this ReadOnlySpan<char> text)
    {
        if (text.Length == 0) return 0;

        var wordCount = 0;
        var index = 0;

        while (index < text.Length && char.IsWhiteSpace(text[index]))
        { index++; }

        while (index < text.Length)
        {
            while (index < text.Length && !char.IsWhiteSpace(text[index]))
            { index++; }

            wordCount++;

            while (index < text.Length && char.IsWhiteSpace(text[index]))
            { index++; }
        }

        return wordCount;
    }

    public static string ReplaceAt(this string input, int index, int length, string replacement)
    {
        if (length == 0) return input;

        return string.Create(
            input.Length - length + replacement.Length,
            (input, index, length, replacement),
            (span, state) =>
            {
                state
                    .input
                    .AsSpan()[..state.index].
                    CopyTo(span);

                state
                    .replacement
                    .AsSpan()
                    .CopyTo(span[state.index..]);

                state
                    .input
                    .AsSpan()[(state.index + state.length)..]
                    .CopyTo(span[(state.index + state.replacement.Length)..]);
            });
    }
}