namespace HouseofCat.Algorithms.Sorting;

public static class ShellSort
{
    // Complexity O(n^3/2)
    public static void Sort(int[] input)
    {
        var gap = 1;
        while (gap < input.Length / 3) { gap = (3 * gap) + 1; }

        while (gap >= 1)
        {
            for (int i = gap; i < input.Length; i++)
            {
                for (int j = i; j >= gap && input[j] < input[j - gap]; j -= gap)
                {
                    Helpers.Swap(input, j, j - gap);
                }
            }

            gap /= 3;
        }
    }
}
