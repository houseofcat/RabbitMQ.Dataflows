namespace HouseofCat.Algorithms.Sorting
{
    public static class InsertionSort
    {
        // Complexity O(n^2)
        public static void Sort(int[] input)
        {
            for (int partitionIndex = 1; partitionIndex < input.Length; partitionIndex++)
            {
                var currentUnsorted = input[partitionIndex];

                int iterator;
                for (iterator = partitionIndex; iterator > 0 && input[iterator - 1] > currentUnsorted; iterator--)
                {
                    input[iterator] = input[iterator - 1];
                }

                input[iterator] = currentUnsorted;
            }
        }
    }
}
