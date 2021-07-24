using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Algorithms.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Benchmarks.Compression
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.NetCoreApp31)]
    public class MedianOfTwoSortedArraysBenchmark
    {
        private static int ArraySize = 5000;

        private int[] Array1 { get; set; } = new int[ArraySize];
        private int[] Array2 { get; set; } = new int[ArraySize];

        [GlobalSetup]
        public void Setup()
        {
            var rand = new Random();
            var nums1 = new List<int>(ArraySize);
            var nums2 = new List<int>(ArraySize);
            var minInt = int.MinValue + 1 / 2;
            var maxInt = int.MaxValue - 1 / 2;

            for (int i = 0; i < ArraySize; i++)
            {
                nums1.Add(rand.Next(minInt, maxInt));
                nums2.Add(rand.Next(minInt, maxInt));
            }

            nums1.Sort();
            nums2.Sort();

            Array1 = nums1.ToArray();
            Array2 = nums1.ToArray();

        }

        [Benchmark(Baseline = true)]
        public void FindMedianOfTwoSortedArrays_v1()
        {
            Arrays.FindMedianOfSortedArrays_v1(Array1, Array2);
        }

        [Benchmark]
        public void FindMedianOfTwoSortedArrays_v2()
        {
            Arrays.FindMedianOfSortedArrays_v2(Array1, Array2);
        }
    }
}
