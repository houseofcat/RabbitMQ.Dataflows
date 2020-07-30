namespace HouseofCat.Library.Algorithms.Math
{
    public static class ThreeSum
    {
        // Input Example
        // 1, 3, -2, 1, -3, 0, 2

        // Triples Example
        // 1, 3, -2
        // 1, 3, 1
        // 1, 3, -3
        // 1, 3, 0

        // Restrictions
        // Can only use each element once.
        public static int BruteForceSolve(int[] input, int targetSum)
        {
            var solutionCount = 0;

            // Walk Full Array [Avg. Complexity O(n^1)]
            for (int i = 0; i < input.Length; i++)
            {
                // Walk For Second Value [Avg. Complexity O(n^2)]
                for (int j = i + 1; j < input.Length; j++)
                {
                    // Walk For Third Value [Avg. Complexity O(n^3)]
                    for (int k = j + 1; k < input.Length; k++)
                    {
                        if (input[i] + input[j] + input[k] == targetSum)
                        {
                            solutionCount++;
                        }
                    }
                }
            }

            return solutionCount;
        }
    }
}
