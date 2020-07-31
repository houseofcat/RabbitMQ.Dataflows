using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HouseofCat.Encryption;
using HouseofCat.Encryption.Hash;
using HouseofCat.Utilities.Random;
using System.Threading.Tasks;

namespace Examples.RabbitMQ.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser, ThreadingDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.NetCoreApp31)]
    public class EncryptBenchmark
    {
        private XorShift XorShift;
        private byte[] Payload1 { get; set; }
        private byte[] Payload2 { get; set; }
        private byte[] Payload3 { get; set; }
        private byte[] Payload4 { get; set; }

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

            HashKey = ArgonHash
                .GetHashKeyAsync(Passphrase, Salt, KeySize)
                .GetAwaiter()
                .GetResult();
        }

        [Benchmark]
        public async Task CreateArgonHashKeyAsync()
        {
            var hashKey = await ArgonHash
                .GetHashKeyAsync(Passphrase, Salt, KeySize)
                .ConfigureAwait(false);
        }

        [Benchmark]
        public void Encrypt256()
        {
            var encryptedData = AesEncrypt.Aes256Encrypt(Payload1, HashKey);
        }

        [Benchmark]
        public void Encrypt512()
        {
            var encryptedData = AesEncrypt.Aes256Encrypt(Payload2, HashKey);
        }

        [Benchmark]
        public void Encrypt1024()
        {
            var encryptedData = AesEncrypt.Aes256Encrypt(Payload3, HashKey);
        }

        [Benchmark]
        public void Encrypt2048()
        {
            var encryptedData = AesEncrypt.Aes256Encrypt(Payload4, HashKey);
        }

        [Benchmark]
        public void EncryptDecrypt256()
        {
            var decryptedData = AesEncrypt.Aes256Decrypt(AesEncrypt.Aes256Encrypt(Payload1, HashKey), HashKey);
        }

        [Benchmark]
        public void EncryptDecrypt512()
        {
            var decryptedData = AesEncrypt.Aes256Decrypt(AesEncrypt.Aes256Encrypt(Payload2, HashKey), HashKey);
        }

        [Benchmark]
        public void EncryptDecrypt1024()
        {
            var decryptedData = AesEncrypt.Aes256Decrypt(AesEncrypt.Aes256Encrypt(Payload3, HashKey), HashKey);
        }

        [Benchmark]
        public void EncryptDecrypt2048()
        {
            var decryptedData = AesEncrypt.Aes256Decrypt(AesEncrypt.Aes256Encrypt(Payload4, HashKey), HashKey);
        }
    }
}
