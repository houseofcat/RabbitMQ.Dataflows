
using System;
using System.Security.Cryptography;

namespace HouseofCat.Encryption
{
    // Sources:
    // https://docs.microsoft.com/en-us/dotnet/standard/security/cross-platform-cryptography
    // 
    public class AesGcmEncryptionProvider : IEncryptionProvider
    {
        /// <summary>
        /// Safer way of generating random bytes.
        /// https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rngcryptoserviceprovider?redirectedfrom=MSDN&view=net-5.0
        /// </summary>
        private readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();

        private readonly byte[] _key;

        public string Type { get; private set; }

        public AesGcmEncryptionProvider(byte[] key, string hashType)
        {
            if (!Constants.Aes.ValidKeySizes.Contains(key.Length)) throw new ArgumentException("Keysize is an invalid length.");
            _key = key;

            switch (_key.Length)
            {
                case 16: Type = "AES128"; break;
                case 24: Type = "AES192"; break;
                case 32: Type = "AES256"; break;
            }

            if (!string.IsNullOrWhiteSpace(hashType)) { Type = $"{hashType}-{Type}"; }
        }

        public byte[] Encrypt(ReadOnlyMemory<byte> data)
        {
            using var aes = new AesGcm(_key);

            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // MaxSize = 12
            _rng.GetNonZeroBytes(nonce);

            var enryptedBytes = new byte[data.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // MaxSize = 16

            aes.Encrypt(nonce, data.ToArray(), enryptedBytes, tag);

            // Pad ciphertext with nonce and tag.
            var encryptedData = new byte[nonce.Length + tag.Length + enryptedBytes.Length];
            Buffer.BlockCopy(nonce, 0, encryptedData, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, encryptedData, nonce.Length, tag.Length);
            Buffer.BlockCopy(enryptedBytes, 0, encryptedData, nonce.Length + tag.Length, enryptedBytes.Length);

            return encryptedData;
        }

        public byte[] Decrypt(ReadOnlyMemory<byte> encryptedData)
        {
            using var aes = new AesGcm(_key);

            // Byte Allocation Version
            //var encryptedBytes = encryptedData.ToArray();
            //var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // MaxSize = 12
            //var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // MaxSize = 16
            //var ciphertext = new byte[encryptedData.Length - AesGcm.NonceByteSizes.MaxSize - AesGcm.TagByteSizes.MaxSize];
            //var decryptedBytes = new byte[ciphertext.Length];

            // Isolate nonce and tag from ciphertext.
            //Buffer.BlockCopy(encryptedBytes, 0, nonce, 0, nonce.Length);
            //Buffer.BlockCopy(encryptedBytes, nonce.Length, tag, 0, tag.Length);
            //Buffer.BlockCopy(encryptedBytes, nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);

            // Slicing Version
            var nonce = encryptedData
                .Slice(0, AesGcm.NonceByteSizes.MaxSize)
                .Span;

            var tag = encryptedData
                .Slice(AesGcm.NonceByteSizes.MaxSize, AesGcm.TagByteSizes.MaxSize)
                .Span;

            var encryptedBytes = encryptedData
                .Slice(AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize)
                .Span;

            var decryptedBytes = new byte[encryptedBytes.Length];

            aes.Decrypt(nonce, encryptedBytes, tag, decryptedBytes);

            return decryptedBytes;
        }
    }
}
