using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.IO;
using System.Runtime.Intrinsics.X86;

namespace HouseofCat.Encryption
{
    public class AesSymmetricEncryptionProvider : IEncryptionProvider
    {
        private readonly Random _random = new Random();
        private readonly AesEncryptionOptions _options;
        private readonly byte[] _key;

        private KeyParameter _keyParameter;
        private int _macBitSize;
        private int _nonceSize;

        public AesSymmetricEncryptionProvider(byte[] key, AesEncryptionOptions options = null)
        {
            if (!Constants.Aes.ValidKeySizes.Contains(_key.Length)) throw new ArgumentException("Keysize is an invalid length.");

            _options = options;
            _key = key;
            _keyParameter = new KeyParameter(_key);
            _macBitSize = _options?.MacBitSize ?? Constants.Aes.MacBitSize;
            _nonceSize = _options?.NonceSize ?? Constants.Aes.NonceSize;
        }

        public byte[] Encrypt(ReadOnlyMemory<byte> data)
        {
            var nonce = new byte[_nonceSize];
            _random.NextBytes(nonce);

            var cipher = GetGcmBlockCipher(nonce);

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

        public byte[] Decrypt(ReadOnlyMemory<byte> encryptedData)
        {
            using var cipherStream = new MemoryStream(encryptedData.ToArray());
            using var cipherReader = new BinaryReader(cipherStream);

            var nonce = cipherReader.ReadBytes(_nonceSize);
            var cipher = GetGcmBlockCipher(nonce);

            var cipherText = cipherReader.ReadBytes(encryptedData.Length - nonce.Length);
            var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];

            try
            { cipher.DoFinal(plainText, cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0)); }
            catch (InvalidCipherTextException)
            { return null; }

            return plainText;
        }

        private GcmBlockCipher GetGcmBlockCipher(byte[] nonce)
        {
            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(_keyParameter, _macBitSize, nonce);

            cipher.Init(false, parameters);

            return cipher;
        }
    }
}
