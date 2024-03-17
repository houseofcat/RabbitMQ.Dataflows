namespace HouseofCat.Algorithms.Sorting;

public static class BubbleSort
{
    // Complexity O(n^2)
    public static void Sort(int[] input)
    {
        for (int partitionIndex = input.Length - 1; partitionIndex > 0; partitionIndex--)
        {
            var shortCircuit = true;

            for (int i = 0; i < partitionIndex; i++)
            {
                if (input[i] > input[i + 1])
                {
                    Helpers.Swap(input, i, i + 1);
                    shortCircuit = false;
                }
            }

            if (shortCircuit) { break; }
        }
    }
}
