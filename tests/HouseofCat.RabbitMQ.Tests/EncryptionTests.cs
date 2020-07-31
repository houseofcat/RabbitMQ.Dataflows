using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Encryption.Hash;
using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Service;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CookedRabbit.Core.Tests
{
    public class EncryptionTests
    {
        private readonly ITestOutputHelper _output;
        private const string Passphrase = "SuperNintendoHadTheBestZelda";
        private const string Salt = "SegaGenesisIsTheBestConsole";

        public EncryptionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ArgonHashTest()
        {
            var hashKey = await ArgonHash
                .GetHashKeyAsync(Passphrase, Salt, 32)
                .ConfigureAwait(false);

            Assert.True(hashKey.Length == 32);
        }

        [Fact]
        public async Task EncryptDecryptTest()
        {
            var data = new byte[] { 0xFF, 0x00, 0xAA, 0xFF, 0x00, 0x00, 0xFF, 0xAA, 0x00, 0xFF, 0x00, 0xFF };

            var hashKey = await ArgonHash
                .GetHashKeyAsync(Passphrase, Salt, 32)
                .ConfigureAwait(false);

            _output.WriteLine(Encoding.UTF8.GetString(hashKey));
            _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

            var encryptedData = AesEncrypt.Aes256Encrypt(data, hashKey);
            _output.WriteLine($"Encrypted: {Encoding.UTF8.GetString(encryptedData)}");

            var decryptedData = AesEncrypt.Aes256Decrypt(encryptedData, hashKey);
            _output.WriteLine($"Data: {Encoding.UTF8.GetString(data)}");
            _output.WriteLine($"Decrypted: {Encoding.UTF8.GetString(decryptedData)}");

            Assert.Equal(data, decryptedData);
        }

        public class Message
        {
            public ulong MessageId { get; set; }
            public string StringMessage { get; set; }
        }

        [Fact]
        public async Task ComcryptDecomcryptTest()
        {
            var message = new Message { StringMessage = $"Sensitive ReceivedLetter 0", MessageId = 0 };
            var data = JsonSerializer.SerializeToUtf8Bytes(message);

            var hashKey = await ArgonHash
                .GetHashKeyAsync(Passphrase, Salt, HouseofCat.Encryption.Constants.Aes256.KeySize)
                .ConfigureAwait(false);

            _output.WriteLine(Encoding.UTF8.GetString(hashKey));
            _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

            // Comcrypt
            var payload = await Gzip.CompressAsync(data);
            var encryptedPayload = AesEncrypt.Aes256Encrypt(payload, hashKey);

            // Decomcrypt
            var decryptedData = AesEncrypt.Aes256Decrypt(encryptedPayload, hashKey);
            Assert.NotNull(decryptedData);

            var decompressed = await Gzip.DecompressAsync(decryptedData);
            JsonSerializer.SerializeToUtf8Bytes(decompressed);
            _output.WriteLine($"Data: {Encoding.UTF8.GetString(data)}");
            _output.WriteLine($"Decrypted: {Encoding.UTF8.GetString(decryptedData)}");

            Assert.Equal(data, decompressed);
        }

        [Fact]
        public async Task RabbitServiceCCDCTest()
        {
            var rabbitService = new RabbitService(
                "Config.json",
                Passphrase,
                Salt);

            var message = new Message { StringMessage = $"Sensitive ReceivedLetter 0", MessageId = 0 };
            var data = JsonSerializer.SerializeToUtf8Bytes(message);
            var letter = new Letter("", "Test", data);

            var letter2 = letter.Clone();
            letter2.Body = JsonSerializer.SerializeToUtf8Bytes(message);

            await rabbitService.ComcryptAsync(letter2);
            await rabbitService.DecomcryptAsync(letter2);

            Assert.Equal(letter.Body, letter2.Body);
        }
    }
}
