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

            HashProvider = new Argon2ID_HashingProvider();

            HashKey = HashProvider.GetHashKey(Passphrase, Salt, KeySize);

            EncryptionProvider = new AesGcmEncryptionProvider(HashKey, HashProvider.Type);
            EncryptedPayload1 = EncryptionProvider.Encrypt(Payload1).ToArray();
            EncryptedPayload2 = EncryptionProvider.Encrypt(Payload2).ToArray();
            EncryptedPayload3 = EncryptionProvider.Encrypt(Payload3).ToArray();
            EncryptedPayload4 = EncryptionProvider.Encrypt(Payload4).ToArray();
        }

        [Benchmark(Baseline = true)]
        public void Encrypt_1KB()
        {
            EncryptionProvider.Encrypt(Payload1);
        }

        [Benchmark]
        public void Encrypt_2KB()
        {
            EncryptionProvider.Encrypt(Payload2);
        }

        [Benchmark]
        public void Encrypt_4KB()
        {
            EncryptionProvider.Encrypt(Payload3);
        }

        [Benchmark]
        public void Encrypt_8KB()
        {
            EncryptionProvider.Encrypt(Payload4);
        }

        [Benchmark]
        public void EncryptToStream_1KB()
        {
            EncryptionProvider.EncryptToStream(Payload1);
        }

        [Benchmark]
        public void EncryptToStream_2KB()
        {
            EncryptionProvider.EncryptToStream(Payload2);
        }

        [Benchmark]
        public void EncryptToStream_4KB()
        {
            EncryptionProvider.EncryptToStream(Payload3);
        }

        [Benchmark]
        public void EncryptToStream_8KB()
        {
            EncryptionProvider.EncryptToStream(Payload4);
        }

        [Benchmark]
        public void Decrypt_1KB()
        {
            EncryptionProvider.Decrypt(EncryptedPayload1);
        }

        [Benchmark]
        public void Decrypt_2KB()
        {
            EncryptionProvider.Decrypt(EncryptedPayload2);
        }

        [Benchmark]
        public void Decrypt_4KB()
        {
            EncryptionProvider.Decrypt(EncryptedPayload3);
        }

        [Benchmark]
        public void Decrypt_8KB()
        {
            EncryptionProvider.Decrypt(EncryptedPayload4);
        }

        [Benchmark]
        public void DecryptToStream_1KB()
        {
            EncryptionProvider.DecryptToStream(EncryptedPayload1);
        }

        [Benchmark]
        public void DecryptToStream_2KB()
        {
            EncryptionProvider.DecryptToStream(EncryptedPayload2);
        }

        [Benchmark]
        public void DecryptToStream_4KB()
        {
            EncryptionProvider.DecryptToStream(EncryptedPayload3);
        }

        [Benchmark]
        public void DecryptToStream_8KB()
        {
            EncryptionProvider.DecryptToStream(EncryptedPayload4);
        }

        [Benchmark]
        public void EncryptDecryptTo_8KB()
        {
            var encrypted = EncryptionProvider.Encrypt(Payload4);
            var decrypted = EncryptionProvider.Decrypt(encrypted);
        }

        [Benchmark]
        public void EncryptDecryptToStream_8KB()
        {
            var encryptedStream = EncryptionProvider.EncryptToStream(Payload4);
            var decryptedStream = EncryptionProvider.Decrypt(encryptedStream);
        }
    }
}
