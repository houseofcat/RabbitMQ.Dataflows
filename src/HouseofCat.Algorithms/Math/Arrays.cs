using System.Runtime.CompilerServices;

namespace HouseofCat.Algorithms.Math
{
    public static class Arrays
    {
        /// <summary>
        /// Inligning lowers memory allocations. There is actually another performance optimization but it makes it messier to read and I like this one better.
        /// <para>You don't actually have to build the entire 3rd array, you can stop at the median values as you know the length its supposed to be.</para>
        /// <para>Once you make that realization, you realize that in fact, you don't have to build a 3rd array at all.</para>
        /// Using the same mechanism of ordering sorted arrays into a 3rd array, you could just grab the median values from the original array.
        /// </summary>
        /// <param name="nums1"></param>
        /// <param name="nums2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FindMedianOfSortedArrays_v1(int[] nums1, int[] nums2)
        {
            if (nums1.Length + nums2.Length == 0) return 0;
            if (nums1.Length == 1 && nums2.Length == 0) return nums1[0];
            if (nums1.Length == 0 && nums2.Length == 1) return nums2[0];

            var mergedArray = MergeSortedArrays(nums1, nums2);
            var center = mergedArray.Length / 2;

            if (mergedArray.Length % 2 == 0)
            {
                return (mergedArray[center - 1] + mergedArray[center]) / 2.0;
            }
            else
            {
                return mergedArray[center];
            }

        }

        // Lower allocations.
        public static double FindMedianOfSortedArrays_v2(int[] nums1, int[] nums2)
        {
            var totalElements = nums1.Length + nums2.Length;
            if (totalElements == 0) return 0;

            if (nums1.Length == 1 && nums2.Length == 0) return nums1[0];
            if (nums1.Length == 0 && nums2.Length == 1) return nums2[0];

            var center = totalElements / 2;

            var index1 = 0;
            var index2 = 0;
            var index3 = 0;

            if (totalElements % 2 == 1)
            {
                while (index1 < nums1.Length && index2 < nums2.Length)
                {
                    if (nums1[index1] < nums2[index2])
                    {
                        if (index3 == center) // short-cut
                        {
                            return nums1[index1];
                        }
                        index3++;
                        index1++;
                    }
                    else if (nums1[index1] == nums2[index2]) // handle duplicates in one pass
                    {
                        if (index3 == center)
                        {
                            return nums1[index1];
                        }
                        index3++;
                        index1++;

                        if (index3 == center)
                        {
                            return nums2[index2];
                        }
                        index3++;
                        index2++;
                    }
                    else
                    {
                        if (index3 == center)
                        {
                            return nums2[index2];
                        }
                        index3++;
                        index2++;
                    }
                }

                // Add any remaining unused values.
                while (index1 < nums1.Length)
                {
                    if (index3 == center)
                    {
                        return nums1[index1];
                    }
                    index3++;
                    index1++;
                }

                // Add any remaining unused values.
                while (index2 < nums2.Length)
                {
                    if (index3 == center)
                    {
                        return nums2[index2];
                    }
                    index3++;
                    index2++;
                }
            }
            else
            {
                var medianValue1 = 0;

                while (index1 < nums1.Length && index2 < nums2.Length)
                {
                    if (nums1[index1] < nums2[index2])
                    {
                        if (index3 == center - 1)
                        {
                            medianValue1 = nums1[index1];
                        }
                        else if (index3 == center)
                        {
                            return (medianValue1 + nums1[index1]) / 2.0;
                        }

                        index3++;
                        index1++;
                    }
                    else if (nums1[index1] == nums2[index2]) // handle dupelicates in one pass
                    {
                        if (index3 == center - 1)
                        {
                            medianValue1 = nums1[index1];
                        }
                        else if (index3 == center)
                        {
                            return (medianValue1 + nums1[index1]) / 2.0;
                        }
                        index3++;
                        index1++;

                        if (index3 == center - 1)
                        {
                            medianValue1 = nums2[index2];
                        }
                        else if (index3 == center)
                        {
                            return (medianValue1 + nums2[index2]) / 2.0;
                        }
                        index3++;
                        index2++;
                    }
                    else
                    {
                        if (index3 == center - 1)
                        {
                            medianValue1 = nums2[index2];
                        }
                        else if (index3 == center)
                        {
                            return (medianValue1 + nums2[index2]) / 2.0;
                        }
                        index3++;
                        index2++;
                    }
                }

                // Add any remaining unused values.
                while (index1 < nums1.Length)
                {
                    if (index3 == center - 1)
                    {
                        medianValue1 = nums1[index1];
                    }
                    else if (index3 == center)
                    {
                        return (medianValue1 + nums1[index1]) / 2.0;
                    }
                    index3++;
                    index1++;
                }

                // Add any remaining unused values.
                while (index2 < nums2.Length)
                {
                    if (index3 == center - 1)
                    {
                        medianValue1 = nums2[index2];
                    }
                    else if (index3 == center)
                    {
                        return (medianValue1 + nums2[index2]) / 2.0;
                    }
                    index3++;
                    index2++;
                }
            }

            return -1.0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] MergeSortedArrays(int[] nums1, int[] nums2)
        {
            var mergedArray = new int[nums1.Length + nums2.Length];

            var index1 = 0;
            var index2 = 0;
            var index3 = 0;

            // Assemble new array that is also sorted by checking both arrays
            // and incrementing index for the associated array
            // when it has been used.
            while (index1 < nums1.Length && index2 < nums2.Length)
            {
                if (nums1[index1] < nums2[index2])
                {
                    mergedArray[index3++] = nums1[index1++];
                }
                else if (nums1[index1] == nums2[index2]) // handle duplicates in one pass
                {
                    mergedArray[index3++] = nums1[index1++];
                    mergedArray[index3++] = nums2[index2++];
                }
                else
                {
                    mergedArray[index3++] = nums2[index2++];
                }
            }

            // Add any remaining unused values.
            while (index1 < nums1.Length)
            {
                mergedArray[index3++] = nums1[index1++];
            }

            // Add any remaining unused values.
            while (index2 < nums2.Length)
            {
                mergedArray[index3++] = nums2[index2++];
            }

            return mergedArray;
        }
    }
}
