using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.IO;

namespace HouseofCat.Encryption
{
    public static class AesEncrypt
    {
        private readonly static Random Random = new Random();

        public static byte[] Aes256Encrypt(ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> key, EncryptionOptions options = null)
        {
            if (key.Length != Constants.Aes256.KeySize || data.Length == 0)
            { return null; }

            var nonce = new byte[options?.NonceSize ?? Constants.Aes256.NonceSize];
            Random.NextBytes(nonce);

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(true, new AeadParameters(new KeyParameter(key.ToArray()), options?.MacBitSize ?? Constants.Aes256.MacBitSize, nonce));

            var cipherText = new byte[cipher.GetOutputSize(data.Length)];
            cipher.DoFinal(cipherText, cipher.ProcessBytes(data.ToArray(), 0, data.Length, cipherText, 0));

            using var cs = new MemoryStream();
            using (var bw = new BinaryWriter(cs))
            {
                bw.Write(nonce);
                bw.Write(cipherText);
            }

            return cs.ToArray();
        }

        public static byte[] Aes256Decrypt(ReadOnlyMemory<byte> encryptedData, ReadOnlyMemory<byte> key, EncryptionOptions options = null)
        {
            if (key.Length != Constants.Aes256.KeySize || encryptedData.Length == 0)
            { return null; }

            using var cipherStream = new MemoryStream(encryptedData.ToArray());
            using var cipherReader = new BinaryReader(cipherStream);

            var nonce = cipherReader.ReadBytes(options?.NonceSize ?? Constants.Aes256.NonceSize);

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key.ToArray()), options?.MacBitSize ?? Constants.Aes256.MacBitSize, nonce);

            cipher.Init(false, parameters);

            var cipherText = cipherReader.ReadBytes(encryptedData.Length - nonce.Length);
            var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];

            try
            { cipher.DoFinal(plainText, cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0)); }
            catch (InvalidCipherTextException)
            { return null; }

            return plainText;
        }
    }
}
