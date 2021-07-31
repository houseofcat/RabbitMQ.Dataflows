using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Hashing.Argon;
using HouseofCat.Utilities.Random;
using System.Threading.Tasks;

namespace Benchmarks.Encryption
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.NetCoreApp31)]
    public class ArgonHashingBenchmark
    {
        private IHashingProvider HashProvider;

        private string Passphrase { get; } = "TestMessageForPassword";
        private string Salt { get; } = "SaltySaltSaltSalt";

        private const int KeySize = 32;

        [GlobalSetup]
        public void Setup()
        {
            HashProvider = new Argon2ID_HashingProvider();
        }

        [Benchmark(Baseline = true)]
        public async Task CreateArgonHashKeyAsync()
        {
            var hashKey = await HashProvider
                .GetHashKeyAsync(Passphrase, Salt, KeySize)
                .ConfigureAwait(false);
        }
    }
}
