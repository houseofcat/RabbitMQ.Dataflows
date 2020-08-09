using HouseofCat.Compression.Builtin;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HouseofCat.RabbitMQ.Tests
{
    public class EncryptionTests
    {
        private readonly ITestOutputHelper _output;
        private readonly IHashingProvider _hashingProvider;
        private const string Passphrase = "SuperNintendoHadTheBestZelda";
        private const string Salt = "SegaGenesisIsTheBestConsole";

        public EncryptionTests(ITestOutputHelper output)
        {
            _output = output;
            _hashingProvider = new Argon2IDHasher();
        }

        [Fact]
        public async Task Aes256_SymmetricKey()
        {
            var data = new byte[] { 0xFF, 0x00, 0xAA, 0xFF, 0x00, 0x00, 0xFF, 0xAA, 0x00, 0xFF, 0x00, 0xFF };

            var hashKey = await _hashingProvider
                .GetHashKeyAsync(Passphrase, Salt, 32)
                .ConfigureAwait(false);

            _output.WriteLine(Encoding.UTF8.GetString(hashKey));
            _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

            var encryptionProvider = new AesSymmetricEncryptionProvider(hashKey);

            var encryptedData = encryptionProvider.Encrypt(data);
            _output.WriteLine($"Encrypted: {Encoding.UTF8.GetString(encryptedData)}");

            var decryptedData = encryptionProvider.Decrypt(encryptedData);
            _output.WriteLine($"Data: {Encoding.UTF8.GetString(data)}");
            _output.WriteLine($"Decrypted: {Encoding.UTF8.GetString(decryptedData)}");

            Assert.Equal(data, decryptedData);
        }

        [Fact]
        public async Task Aes192_SymmetricKey()
        {
            var data = new byte[] { 0xFF, 0x00, 0xAA, 0xFF, 0x00, 0x00, 0xFF, 0xAA, 0x00, 0xFF, 0x00, 0xFF };

            var hashKey = await _hashingProvider
                .GetHashKeyAsync(Passphrase, Salt, 24)
                .ConfigureAwait(false);

            _output.WriteLine(Encoding.UTF8.GetString(hashKey));
            _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

            var encryptionProvider = new AesSymmetricEncryptionProvider(hashKey);

            var encryptedData = encryptionProvider.Encrypt(data);
            _output.WriteLine($"Encrypted: {Encoding.UTF8.GetString(encryptedData)}");

            var decryptedData = encryptionProvider.Decrypt(encryptedData);
            _output.WriteLine($"Data: {Encoding.UTF8.GetString(data)}");
            _output.WriteLine($"Decrypted: {Encoding.UTF8.GetString(decryptedData)}");

            Assert.Equal(data, decryptedData);
        }

        [Fact]
        public async Task Aes128_SymmetricKey()
        {
            var data = new byte[] { 0xFF, 0x00, 0xAA, 0xFF, 0x00, 0x00, 0xFF, 0xAA, 0x00, 0xFF, 0x00, 0xFF };

            var hashKey = await _hashingProvider
                .GetHashKeyAsync(Passphrase, Salt, 16)
                .ConfigureAwait(false);

            _output.WriteLine(Encoding.UTF8.GetString(hashKey));
            _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

            var encryptionProvider = new AesSymmetricEncryptionProvider(hashKey);

            var encryptedData = encryptionProvider.Encrypt(data);
            _output.WriteLine($"Encrypted: {Encoding.UTF8.GetString(encryptedData)}");

            var decryptedData = encryptionProvider.Decrypt(encryptedData);
            _output.WriteLine($"Data: {Encoding.UTF8.GetString(data)}");
            _output.WriteLine($"Decrypted: {Encoding.UTF8.GetString(decryptedData)}");

            Assert.Equal(data, decryptedData);
        }
    }
}
