using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Utilities.Random;
using System.Threading.Tasks;

namespace Benchmarks.RabbitMQ
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.NetCoreApp31)]
    public class EncryptBenchmark
    {
        private XorShift XorShift;
        private IHashingProvider HashProvider;
        private IEncryptionProvider EncryptionProvider;

        private byte[] Payload1 { get; set; }
        private byte[] Payload2 { get; set; }
        private byte[] Payload3 { get; set; }
        private byte[] Payload4 { get; set; }

        private byte[] EncryptedPayload1 { get; set; }
        private byte[] EncryptedPayload2 { get; set; }
        private byte[] EncryptedPayload3 { get; set; }
        private byte[] EncryptedPayload4 { get; set; }

        private string Passphrase { get; } = "TestMessageToEncrypt";
        private string Salt { get; } = "SaltySaltSaltSalt";

        private const int KeySize = 32;
        private byte[] HashKey { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            XorShift = new XorShift(true);
            Payload1 = XorShift.GetRandomBytes(1024);
            Payload2 = XorShift.GetRandomBytes(2048);
            Payload3 = XorShift.GetRandomBytes(4096);
            Payload4 = XorShift.GetRandomBytes(8192);

            HashProvider = new Argon2IDHasher();

            HashKey = HashProvider
                .GetHashKeyAsync(Passphrase, Salt, KeySize)
                .GetAwaiter()
                .GetResult();

            EncryptionProvider = new AesGcmEncryptionProvider(HashKey, HashProvider.Type);
            EncryptedPayload1 = EncryptionProvider.Encrypt(Payload1);
            EncryptedPayload2 = EncryptionProvider.Encrypt(Payload2);
            EncryptedPayload3 = EncryptionProvider.Encrypt(Payload3);
            EncryptedPayload4 = EncryptionProvider.Encrypt(Payload4);
        }

        [Benchmark, IterationCount(10)]
        public async Task CreateArgonHashKeyAsync()
        {
            var hashKey = await HashProvider
                .GetHashKeyAsync(Passphrase, Salt, KeySize)
                .ConfigureAwait(false);
        }

        [Benchmark(Baseline = true)]
        public void Encrypt1KBytes()
        {
            EncryptionProvider.Encrypt(Payload1);
        }

        [Benchmark]
        public void Encrypt2KBytes()
        {
            EncryptionProvider.Encrypt(Payload2);
        }

        [Benchmark]
        public void Encrypt4KBytes()
        {
            EncryptionProvider.Encrypt(Payload3);
        }

        [Benchmark]
        public void Encrypt8KBytes()
        {
            EncryptionProvider.Encrypt(Payload4);
        }

        [Benchmark]
        public void Decrypt1KBytes()
        {
            EncryptionProvider.Decrypt(EncryptedPayload1);
        }

        [Benchmark]
        public void Decrypt2KBytes()
        {
            EncryptionProvider.Decrypt(EncryptedPayload2);
        }

        [Benchmark]
        public void Decrypt4KBytes()
        {
            EncryptionProvider.Decrypt(EncryptedPayload3);
        }

        [Benchmark]
        public void Decrypt8KBytes()
        {
            EncryptionProvider.Decrypt(EncryptedPayload4);
        }
    }
}
