namespace Benchmarks.Misc
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Jobs;
    using HouseofCat.Extensions;
    using System;

    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.NetCoreApp31)]
    public class IsNumericBenchmark
    {
        public int IntValue = 1;
        public long? LongValue = int.MaxValue;

        public object ObjIntValue => (object)IntValue;
        public object ObjLongValue => (object)LongValue;

        [Benchmark(Baseline = true)]
        public void IsNumeric()
        {
            IntValue.IsNumeric();
        }

        [Benchmark]
        public void IsNumericAtRuntime()
        {
            ObjIntValue.IsNumericAtRuntime();
        }

        [Benchmark]
        public void IsNullableNumeric()
        {
            LongValue.IsNullableNumeric();
        }
    }
}
