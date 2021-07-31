using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Jobs;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Hashing.Argon;
using HouseofCat.Recyclable;
using HouseofCat.Utilities.Random;
using System.Threading.Tasks;

namespace Benchmarks.Encryption
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50 | RuntimeMoniker.NetCoreApp31)]
    public class AllocationEncryptionBenchmark
    {
        private XorShift XorShift;
        private IHashingProvider HashProvider;
        private IEncryptionProvider EncryptionProvider;
        private IEncryptionProvider RecyclableEncryptionProvider;

        private byte[] Payload { get; set; }

        private byte[] EncryptedPayload { get; set; }

        private string Passphrase { get; } = "TestMessageToEncrypt";
        private string Salt { get; } = "SaltySaltSaltSalt";

        private const int KeySize = 32;
        private byte[] HashKey { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            XorShift = new XorShift(true);
            Payload = XorShift.GetRandomBytes(1024);

            HashProvider = new Argon2ID_HashingProvider();

            HashKey = HashProvider.GetHashKey(Passphrase, Salt, KeySize);

            EncryptionProvider = new AesGcmEncryptionProvider(HashKey, HashProvider.Type);
            RecyclableEncryptionProvider = new RecyclableAesGcmEncryptionProvider(HashKey, HashProvider.Type);

            //RecyclableManager.ConfigureNewStaticManagerWithDefaults();

            EncryptedPayload = EncryptionProvider.Encrypt(Payload).ToArray();
        }

        [Benchmark(Baseline = true)]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        public void Encrypt_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                EncryptionProvider.Encrypt(Payload);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        public void EncryptToStream_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var stream = EncryptionProvider.EncryptToStream(Payload);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        public void Decrypt_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                EncryptionProvider.Decrypt(EncryptedPayload);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        public void DecryptToStream_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var decryptedStream = EncryptionProvider.DecryptToStream(EncryptedPayload);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        public void EncryptDecryptToStream_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var encryptedStream = EncryptionProvider.EncryptToStream(Payload);
                using var decryptedStream = EncryptionProvider.Decrypt(encryptedStream);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        public void Recyclabe_Encrypt_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                RecyclableEncryptionProvider.Encrypt(Payload);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        public void Recyclabe_EncryptToStream_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var stream = RecyclableEncryptionProvider.EncryptToStream(Payload);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        public void Recyclabe_Decrypt_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                RecyclableEncryptionProvider.Decrypt(EncryptedPayload);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        public void Recyclabe_DecryptToStream_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var decryptedStream = RecyclableEncryptionProvider.DecryptToStream(EncryptedPayload);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1_000)]
        public void Recyclabe_EncryptDecryptToStream_1KB(int x)
        {
            for (var i = 0; i < x; i++)
            {
                using var encryptedStream = RecyclableEncryptionProvider.EncryptToStream(Payload);
                using var decryptedStream = RecyclableEncryptionProvider.Decrypt(encryptedStream);
            }
        }
    }
}
