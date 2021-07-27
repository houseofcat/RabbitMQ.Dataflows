using HouseofCat.Utilities.Errors;
using Microsoft.Toolkit.HighPerformance;
using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
            Guard.AgainstNullOrEmpty(key, nameof(key));

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

        public ArraySegment<byte> Encrypt(ReadOnlyMemory<byte> unencryptedData)
        {
            Guard.AgainstEmpty(unencryptedData, nameof(unencryptedData));

            using var aes = new AesGcm(_key);

            // Slicing Version
            // Rented arrays sizes are minimums, not guarantees.
            // Need to perform extra work managing slices to keep the byte sizes correct but the memory allocations are lower by 200%
            var encryptedBytes = _pool.Rent(unencryptedData.Length);
            var tag = _pool.Rent(AesGcm.TagByteSizes.MaxSize); // MaxSize = 16
            var nonce = _pool.Rent(AesGcm.NonceByteSizes.MaxSize); // MaxSize = 12
            _rng.GetBytes(nonce, 0, AesGcm.NonceByteSizes.MaxSize);

            aes.Encrypt(
                nonce.AsSpan().Slice(0, AesGcm.NonceByteSizes.MaxSize),
                unencryptedData.Span,
                encryptedBytes.AsSpan().Slice(0, unencryptedData.Length),
                tag.AsSpan().Slice(0, AesGcm.TagByteSizes.MaxSize));

            // Prefix ciphertext with nonce and tag, since they are fixed length and it will simplify decryption.
            // Our pattern: Nonce Tag Cipher
            // Other patterns people use: Nonce Cipher Tag // couldn't find a solid source.
            var encryptedData = new byte[AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize + unencryptedData.Length];
            Buffer.BlockCopy(nonce, 0, encryptedData, 0, AesGcm.NonceByteSizes.MaxSize);
            Buffer.BlockCopy(tag, 0, encryptedData, AesGcm.NonceByteSizes.MaxSize, AesGcm.TagByteSizes.MaxSize);
            Buffer.BlockCopy(encryptedBytes, 0, encryptedData, AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize, unencryptedData.Length);

            _pool.Return(encryptedBytes);
            _pool.Return(tag);
            _pool.Return(nonce);

            return encryptedData;
        }

        public MemoryStream Encrypt(Stream unencryptedStream, bool leaveStreamOpen = false)
        {
            Guard.AgainstNullOrEmpty(unencryptedStream, nameof(unencryptedStream));

            if (unencryptedStream.Position == unencryptedStream.Length) { unencryptedStream.Seek(0, SeekOrigin.Begin); }

            using var aes = new AesGcm(_key);

            var length = (int)unencryptedStream.Length;
            var buffer = _pool.Rent(length);
            var bytesRead = unencryptedStream.Read(buffer.AsSpan(0, length));

            if (bytesRead == 0) throw new InvalidDataException();

            // Slicing Version
            // Rented arrays sizes are minimums, not guarantees.
            // Need to perform extra work managing slices to keep the byte sizes correct but the memory allocations are lower by 200%
            var encryptedBytes = _pool.Rent(length);
            var tag = _pool.Rent(AesGcm.TagByteSizes.MaxSize); // MaxSize = 16
            var nonce = _pool.Rent(AesGcm.NonceByteSizes.MaxSize); // MaxSize = 12
            _rng.GetBytes(nonce, 0, AesGcm.NonceByteSizes.MaxSize);

            aes.Encrypt(
                nonce.AsSpan().Slice(0, AesGcm.NonceByteSizes.MaxSize),
                buffer.AsSpan().Slice(0, length),
                encryptedBytes.AsSpan().Slice(0, length),
                tag.AsSpan().Slice(0, AesGcm.TagByteSizes.MaxSize));

            // Prefix ciphertext with nonce and tag, since they are fixed length and it will simplify decryption.
            // Our pattern: Nonce Tag Cipher
            // Other patterns people use: Nonce Cipher Tag // couldn't find a solid source.
            var encryptedStream = new MemoryStream(new byte[AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize + length]);
            using (var binaryWriter = new BinaryWriter(encryptedStream, Encoding.UTF8, true))
            {
                binaryWriter.Write(nonce, 0, AesGcm.NonceByteSizes.MaxSize);
                binaryWriter.Write(tag, 0, AesGcm.TagByteSizes.MaxSize);
                binaryWriter.Write(encryptedBytes, 0, length);
            }

            _pool.Return(buffer);
            _pool.Return(encryptedBytes);
            _pool.Return(tag);
            _pool.Return(nonce);

            encryptedStream.Seek(0, SeekOrigin.Begin);

            if (!leaveStreamOpen) { unencryptedStream.Close(); }

            return encryptedStream;
        }

        public async Task<MemoryStream> EncryptAsync(Stream unencryptedStream, bool leaveStreamOpen = false)
        {
            Guard.AgainstNullOrEmpty(unencryptedStream, nameof(unencryptedStream));

            if (unencryptedStream.Position == unencryptedStream.Length) { unencryptedStream.Seek(0, SeekOrigin.Begin); }

            using var aes = new AesGcm(_key);

            var length = (int)unencryptedStream.Length;
            var buffer = _pool.Rent(length);
            var bytesRead = await unencryptedStream
                .ReadAsync(buffer.AsMemory(0, length))
                .ConfigureAwait(false);

            if (bytesRead == 0) throw new InvalidDataException();

            // Slicing Version
            // Rented arrays sizes are minimums, not guarantees.
            // Need to perform extra work managing slices to keep the byte sizes correct but the memory allocations are lower by 200%
            var encryptedBytes = _pool.Rent(length);
            var tag = _pool.Rent(AesGcm.TagByteSizes.MaxSize); // MaxSize = 16
            var nonce = _pool.Rent(AesGcm.NonceByteSizes.MaxSize); // MaxSize = 12
            _rng.GetBytes(nonce, 0, AesGcm.NonceByteSizes.MaxSize);

            aes.Encrypt(
                nonce.AsSpan().Slice(0, AesGcm.NonceByteSizes.MaxSize),
                buffer.AsSpan().Slice(0, length),
                encryptedBytes.AsSpan().Slice(0, length),
                tag.AsSpan().Slice(0, AesGcm.TagByteSizes.MaxSize));

            // Prefix ciphertext with nonce and tag, since they are fixed length and it will simplify decryption.
            // Our pattern: Nonce Tag Cipher
            // Other patterns people use: Nonce Cipher Tag // couldn't find a solid source.
            var encryptedStream = new MemoryStream(new byte[AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize + length]);
            using (var binaryWriter = new BinaryWriter(encryptedStream, Encoding.UTF8, true))
            {
                binaryWriter.Write(nonce, 0, AesGcm.NonceByteSizes.MaxSize);
                binaryWriter.Write(tag, 0, AesGcm.TagByteSizes.MaxSize);
                binaryWriter.Write(encryptedBytes, 0, length);
            }

            _pool.Return(buffer);
            _pool.Return(encryptedBytes);
            _pool.Return(tag);
            _pool.Return(nonce);

            encryptedStream.Seek(0, SeekOrigin.Begin);

            if (!leaveStreamOpen) { unencryptedStream.Close(); }

            return encryptedStream;
        }

        public MemoryStream EncryptToStream(ReadOnlyMemory<byte> unencryptedData)
        {
            Guard.AgainstEmpty(unencryptedData, nameof(unencryptedData));

            using var aes = new AesGcm(_key);

            // Slicing Version
            // Rented arrays sizes are minimums, not guarantees.
            // Need to perform extra work managing slices to keep the byte sizes correct but the memory allocations are lower by 200%
            var encryptedBytes = _pool.Rent(unencryptedData.Length);
            var tag = _pool.Rent(AesGcm.TagByteSizes.MaxSize); // MaxSize = 16
            var nonce = _pool.Rent(AesGcm.NonceByteSizes.MaxSize); // MaxSize = 12
            _rng.GetBytes(nonce, 0, AesGcm.NonceByteSizes.MaxSize);

            aes.Encrypt(
                nonce.AsSpan().Slice(0, AesGcm.NonceByteSizes.MaxSize),
                unencryptedData.Span,
                encryptedBytes.AsSpan().Slice(0, unencryptedData.Length),
                tag.AsSpan().Slice(0, AesGcm.TagByteSizes.MaxSize));

            // Prefix ciphertext with nonce and tag, since they are fixed length and it will simplify decryption.
            // Our pattern: Nonce Tag Cipher
            // Other patterns people use: Nonce Cipher Tag // couldn't find a solid source.
            var encryptedData = new byte[AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize + unencryptedData.Length];
            Buffer.BlockCopy(nonce, 0, encryptedData, 0, AesGcm.NonceByteSizes.MaxSize);
            Buffer.BlockCopy(tag, 0, encryptedData, AesGcm.NonceByteSizes.MaxSize, AesGcm.TagByteSizes.MaxSize);
            Buffer.BlockCopy(encryptedBytes, 0, encryptedData, AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize, unencryptedData.Length);

            _pool.Return(encryptedBytes);
            _pool.Return(tag);
            _pool.Return(nonce);

            return new MemoryStream(encryptedData);
        }

        public ArraySegment<byte> Decrypt(ReadOnlyMemory<byte> encryptedData)
        {
            Guard.AgainstEmpty(encryptedData, nameof(encryptedData));

            using var aes = new AesGcm(_key);

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

        public MemoryStream Decrypt(Stream encryptedStream, bool leaveStreamOpen = false)
        {
            Guard.AgainstNullOrEmpty(encryptedStream, nameof(encryptedStream));

            if (encryptedStream.Position == encryptedStream.Length) { encryptedStream.Seek(0, SeekOrigin.Begin); }

            using var aes = new AesGcm(_key);

            var encryptedByteLength = (int)encryptedStream.Length - AesGcm.NonceByteSizes.MaxSize - AesGcm.TagByteSizes.MaxSize;
            var encryptedBufferBytes = _pool.Rent(encryptedByteLength);
            var tagBytes = _pool.Rent(AesGcm.TagByteSizes.MaxSize);
            var nonceBytes = _pool.Rent(AesGcm.NonceByteSizes.MaxSize);

            encryptedStream.Read(nonceBytes, 0, AesGcm.NonceByteSizes.MaxSize);
            encryptedStream.Read(tagBytes, 0, AesGcm.TagByteSizes.MaxSize);
            encryptedStream.Read(encryptedBufferBytes, 0, encryptedByteLength);

            // Slicing Version
            var nonce = nonceBytes
                .AsSpan()
                .Slice(0, AesGcm.NonceByteSizes.MaxSize);

            var tag = tagBytes
                .AsSpan()
                .Slice(0, AesGcm.TagByteSizes.MaxSize);

            var encryptedBytes = encryptedBufferBytes
                .AsSpan()
                .Slice(0, encryptedByteLength);

            var decryptedBytes = new byte[encryptedByteLength];
            aes.Decrypt(nonce, encryptedBytes, tag, decryptedBytes);

            _pool.Return(encryptedBufferBytes);
            _pool.Return(tagBytes);
            _pool.Return(nonceBytes);

            if (!leaveStreamOpen) { encryptedStream.Close(); }

            return new MemoryStream(decryptedBytes);
        }

        public MemoryStream DecryptToStream(ReadOnlyMemory<byte> encryptedData)
        {
            Guard.AgainstEmpty(encryptedData, nameof(encryptedData));

            using var aes = new AesGcm(_key);

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

            return new MemoryStream(decryptedBytes);
        }
    }
}
