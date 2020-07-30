using System.Collections.Generic;

namespace HouseofCat.Algorithms.Math
{
    public static class TwoSum
    {
        public static int[] Solve(int[] nums, int target)
        {
            var dict = new Dictionary<int, int>();

            for (int i = 0; i < nums.Length; i++)
            {
                if (dict.ContainsKey(target - nums[i])) // checks if compliment is in dict
                {
                    return new int[] { dict[target - nums[i]], i };
                }
                else if (!dict.ContainsKey(nums[i])) // handles duplicates in array
                {
                    dict.Add(nums[i], i);
                }
            }

            return null;
        }
    }
}
