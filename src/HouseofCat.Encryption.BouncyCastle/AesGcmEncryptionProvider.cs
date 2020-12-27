using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.IO;
using System.Runtime.Intrinsics.X86;

namespace HouseofCat.Encryption
{
    public class AesGcmEncryptionProvider : IEncryptionProvider
    {
        private readonly Random _random = new Random();
        private readonly AesEncryptionOptions _options;

        private readonly KeyParameter _keyParameter;
        private readonly int _macBitSize;
        private readonly int _nonceSize;

        public string Type { get; private set; }

        public AesGcmEncryptionProvider(byte[] key, string hashType, AesEncryptionOptions options = null)
        {
            if (!Constants.Aes.ValidKeySizes.Contains(key.Length)) throw new ArgumentException("Keysize is an invalid length.");

            _options = options;
            _keyParameter = new KeyParameter(key);
            _macBitSize = _options?.MacBitSize ?? Constants.Aes.MacBitSize;
            _nonceSize = _options?.NonceSize ?? Constants.Aes.NonceSize;

            switch (key.Length)
            {
                case 16: Type = "AES128"; break;
                case 24: Type = "AES192"; break;
                case 32: Type = "AES256"; break;
            }

            if (!string.IsNullOrWhiteSpace(hashType)) { Type = $"{hashType}-{Type}"; }
        }

        public byte[] Encrypt(ReadOnlyMemory<byte> data)
        {
            var nonce = new byte[_nonceSize];
            _random.NextBytes(nonce);

            var cipher = GetGcmBlockCipher(true, nonce);

            var cipherLength = cipher.GetOutputSize(data.Length);
            var cipherText = new byte[cipherLength];
            cipher.DoFinal(cipherText, cipher.ProcessBytes(data.ToArray(), 0, data.Length, cipherText, 0));

            using var cs = new MemoryStream();
            using (var bw = new BinaryWriter(cs))
            {
                bw.Write(nonce);
                bw.Write(cipherText);
            }

            return cs.ToArray();
        }

        public byte[] Decrypt(ReadOnlyMemory<byte> encryptedData)
        {
            using var cipherStream = new MemoryStream(encryptedData.ToArray());
            using var cipherReader = new BinaryReader(cipherStream);

            var nonce = cipherReader.ReadBytes(_nonceSize);
            var cipher = GetGcmBlockCipher(false, nonce);

            var cipherText = cipherReader.ReadBytes(encryptedData.Length - nonce.Length);
            var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];

            try
            { cipher.DoFinal(plainText, cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0)); }
            catch (InvalidCipherTextException)
            { return null; }

            return plainText;
        }

        private GcmBlockCipher GetGcmBlockCipher(bool forEncryption, byte[] nonce)
        {
            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(_keyParameter, _macBitSize, nonce);

            cipher.Init(forEncryption, parameters);

            return cipher;
        }
    }
}
