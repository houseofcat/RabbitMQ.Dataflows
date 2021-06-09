using System;
using System.Buffers;
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
        private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Create();

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

            // Byte Allocation Version
            //var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // MaxSize = 12
            //_rng.GetNonZeroBytes(nonce);

            //var encryptedBytes = new byte[data.Length];
            //var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // MaxSize = 16

            //aes.Encrypt(nonce, data, encryptedBytes, tag);

            //var encryptedData = new byte[nonce.Length + tag.Length + enryptedBytes.Length];
            //Buffer.BlockCopy(nonce, 0, encryptedData, 0, nonce.Length);
            //Buffer.BlockCopy(tag, 0, encryptedData, nonce.Length, tag.Length);
            //Buffer.BlockCopy(encryptedBytes, 0, encryptedData, nonce.Length + tag.Length, encryptedBytes.Length);


            // Slicing Version
            // Rented arrays sizes are minimums, not guarantees.
            // Need to perform extra work managing slices to keep the byte sizes correct but the memory allocations are lower by 200%
            var encryptedBytes = _pool.Rent(data.Length);
            var tag = _pool.Rent(AesGcm.TagByteSizes.MaxSize); // MaxSize = 16
            var nonce = _pool.Rent(AesGcm.NonceByteSizes.MaxSize); // MaxSize = 12
            _rng.GetBytes(nonce, 0, AesGcm.NonceByteSizes.MaxSize);

            aes.Encrypt(
                nonce.AsSpan().Slice(0, AesGcm.NonceByteSizes.MaxSize),
                data.Span,
                encryptedBytes.AsSpan().Slice(0, data.Length),
                tag.AsSpan().Slice(0, AesGcm.TagByteSizes.MaxSize));

            // Prefix ciphertext with nonce and tag, since they are fixed length and it will simplify decryption.
            // Our pattern: Nonce Tag Cipher
            // Other patterns people use: Nonce Cipher Tag // couldn't find a solid source.
            var encryptedData = new byte[AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize + data.Length];
            Buffer.BlockCopy(nonce, 0, encryptedData, 0, AesGcm.NonceByteSizes.MaxSize);
            Buffer.BlockCopy(tag, 0, encryptedData, AesGcm.NonceByteSizes.MaxSize, AesGcm.TagByteSizes.MaxSize);
            Buffer.BlockCopy(encryptedBytes, 0, encryptedData, AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize, data.Length);

            _pool.Return(encryptedBytes);
            _pool.Return(tag);
            _pool.Return(nonce);

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
