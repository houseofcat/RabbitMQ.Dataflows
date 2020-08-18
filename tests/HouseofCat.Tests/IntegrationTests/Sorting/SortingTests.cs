using HouseofCat.Algorithms.Sorting;
using Xunit;
using Xunit.Abstractions;

namespace HouseofCat.Tests.IntegrationTests.Sorting
{
    public class SortingTests
    {
        private readonly ITestOutputHelper _output;

        public SortingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static int[][] GenerateSampleArrays()
        {
            var samples = new int[9][];

            samples[0] = new int[] { 1 };
            samples[1] = new int[] { 2, 1 };
            samples[2] = new int[] { 2, 1, 3 };
            samples[3] = new int[] { 1, 1, 1 };
            samples[4] = new int[] { 2, -1, 3, 3 };
            samples[5] = new int[] { 4, -5, 3, 3 };
            samples[6] = new int[] { 0, -5, 3, 3 };
            samples[7] = new int[] { 0, -5, 3, 0 };
            samples[8] = new int[] { 3, 2, 5, 5, 1, 0, 7, 8 };

            return samples;
        }

        [Fact]
        public void BubbleSortTest()
        {
            // Arrange
            var arrayCount = 0;
            foreach (var sample in GenerateSampleArrays())
            {
                // Act
                BubbleSort.Sort(sample);

                // Assert
                for (int i = 0; i < sample.Length - 1; i++)
                {
                    Assert.True(sample[i] <= sample[i + 1], $"Array #{arrayCount} was not sorted.");
                    arrayCount++;
                }
            }
        }

        [Fact]
        public void SelectionSortTest()
        {
            // Arrange
            var arrayCount = 0;
            foreach (var sample in GenerateSampleArrays())
            {
                // Act
                SelectionSort.Sort(sample);

                // Assert
                for (int i = 0; i < sample.Length - 1; i++)
                {
                    Assert.True(sample[i] <= sample[i + 1], $"Array #{arrayCount} was not sorted.");
                    arrayCount++;
                }
            }
        }

        [Fact]
        public void InsertionSortTest()
        {
            // Arrange
            var arrayCount = 0;
            foreach (var sample in GenerateSampleArrays())
            {
                // Act
                InsertionSort.Sort(sample);

                // Assert
                for (int i = 0; i < sample.Length - 1; i++)
                {
                    Assert.True(sample[i] <= sample[i + 1], $"Array #{arrayCount} was not sorted.");
                    arrayCount++;
                }
            }
        }
    }
}
