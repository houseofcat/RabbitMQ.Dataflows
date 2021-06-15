using System.Collections.Generic;

namespace HouseofCat.Algorithms.Strings
{
    public static class Strings
    {
        public static int LongestSubstring_v1(string input)
        {
            var currentSubstring = 0;
            var maxSubstring = 0;
            var characterLocations = new Dictionary<char, int>();

            if (string.IsNullOrEmpty(input)) { return maxSubstring; }
            if (input.Length == 1) { return input.Length; }

            for (var i = 0; i < input.Length; i++)
            {
                if (characterLocations.ContainsKey(input[i])) // reset event
                {
                    // reset i back to the char position following the first entry
                    // i.e. when finding second char 'd', i is reset to position of the first 'd' encounter + 1
                    // to begin from that position
                    i = characterLocations[input[i]] + 1;

                    // this is the end of our current substring, check if its bigger than the last one
                    if (currentSubstring > maxSubstring)
                    { maxSubstring = currentSubstring; }

                    // clear out current working memory.
                    characterLocations.Clear();
                    currentSubstring = 0;

                    // then we add this duplicate letter as the start of the next substring
                    // since it now needs to be included
                    characterLocations.Add(input[i], i);
                    currentSubstring++;
                }
                else
                {
                    characterLocations.Add(input[i], i);
                    currentSubstring++;
                }

                // Exit condition check the last substring count.
                if (i == input.Length - 1)
                {
                    if (currentSubstring > maxSubstring)
                    { maxSubstring = currentSubstring; }
                }
            }

            return maxSubstring;
        }

        // Don't remember my thinking, but its faster.
        public static int LongestSubstring_v2(string input)
        {
            if (string.IsNullOrEmpty(input)) { return 0; }
            if (input.Length == 1) { return input.Length; }

            var result = 0;
            var dict = new Dictionary<char, int>();

            for (int j = 0, i = 0; j < input.Length; j++)
            {
                if (dict.ContainsKey(input[j]))
                {
                    i = System.Math.Max(dict[input[j]], i);
                }

                result = System.Math.Max(result, j - i + 1);

                dict[input[j]] = j + 1;
            }

            return result;
        }

        // Meant to learn how this works. Don't think it's faster than v2.
        public static int LongestSubstring_v3(string input)
        {
            // Edge Cases
            var result = 0;
            if (string.IsNullOrEmpty(input)) { return result; }
            if (input.Length == 1) { return input.Length; }

            // Window
            var charIndexWindow = new int[256];
            var leftIndex = 0;

            // Initialize values to -1. O(n)
            for (int i = 0; i < charIndexWindow.Length; i++)
            {
                charIndexWindow[i] = -1;
            } // Faster than Enumerable.Repeat(T, TSize).ToArray()

            charIndexWindow[input[0]] = leftIndex; // Window Origin

            // Sliding Window O(n)
            for (var currentIndex = 1; currentIndex < input.Length; currentIndex++)
            {
                var rightChar = input[currentIndex];
                if (charIndexWindow[rightChar] != -1 && charIndexWindow[rightChar] >= leftIndex)
                {
                    leftIndex = charIndexWindow[rightChar] + 1;
                }
                charIndexWindow[rightChar] = currentIndex;
                result = System.Math.Max(result, currentIndex - leftIndex + 1);
            }

            return result;
        }
    }
}
