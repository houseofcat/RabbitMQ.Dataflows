using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.Encryption.BouncyCastle
{
    public class AesGcmEncryptionProvider : IEncryptionProvider
    {
        /// <summary>
        /// Safer way of generating random bytes.
        /// https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rngcryptoserviceprovider?redirectedfrom=MSDN&view=net-5.0
        /// </summary>
        private readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Create();
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

        public ArraySegment<byte> Encrypt(ReadOnlyMemory<byte> data)
        {
            return EncryptToStream(data).ToArray();
        }

        public async Task<MemoryStream> EncryptAsync(Stream data)
        {
            var buffer = _pool.Rent((int)data.Length);
            var bytesRead = await data
                .ReadAsync(buffer.AsMemory(0, (int)data.Length))
                .ConfigureAwait(false);

            if (bytesRead == 0) throw new InvalidDataException();

            var nonce = _pool.Rent(_nonceSize);
            _rng.GetNonZeroBytes(nonce);

            var cipher = GetGcmBlockCipher(true, nonce.AsSpan().Slice(0, _nonceSize));

            var cipherLength = cipher.GetOutputSize((int)data.Length);
            var cipherText = new byte[cipherLength];
            cipher.DoFinal(cipherText, cipher.ProcessBytes(buffer, 0, (int)data.Length, cipherText, 0));

            var encryptedStream = new MemoryStream();
            using (var bw = new BinaryWriter(encryptedStream, Encoding.UTF8, true))
            {
                bw.Write(nonce, 0, _nonceSize);
                bw.Write(cipherText);
            }

            _pool.Return(buffer);
            _pool.Return(nonce);

            encryptedStream.Seek(0, SeekOrigin.Begin);
            return encryptedStream;
        }

        public MemoryStream EncryptToStream(ReadOnlyMemory<byte> data)
        {
            var nonce = new byte[_nonceSize];
            _rng.GetNonZeroBytes(nonce);

            var cipher = GetGcmBlockCipher(true, nonce);

            var cipherLength = cipher.GetOutputSize(data.Length);
            var cipherText = new byte[cipherLength];
            cipher.DoFinal(cipherText, cipher.ProcessBytes(data.ToArray(), 0, data.Length, cipherText, 0));

            var encryptedStream = new MemoryStream();
            using (var bw = new BinaryWriter(encryptedStream, Encoding.UTF8, true))
            {
                bw.Write(nonce);
                bw.Write(cipherText);
            }

            encryptedStream.Seek(0, SeekOrigin.Begin);
            return encryptedStream;
        }

        public ArraySegment<byte> Decrypt(ReadOnlyMemory<byte> encryptedData)
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

        public MemoryStream Decrypt(Stream encryptedData)
        {
            using var binaryReader = new BinaryReader(encryptedData);

            var nonce = binaryReader.ReadBytes(_nonceSize);
            var cipher = GetGcmBlockCipher(false, nonce);

            var cipherText = binaryReader.ReadBytes((int)encryptedData.Length - nonce.Length);
            var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];

            try
            { cipher.DoFinal(plainText, cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0)); }
            catch (InvalidCipherTextException)
            { return null; }

            return new MemoryStream(plainText);
        }

        public MemoryStream DecryptToStream(ReadOnlyMemory<byte> encryptedData)
        {
            return new MemoryStream(Decrypt(encryptedData).ToArray());
        }

        private GcmBlockCipher GetGcmBlockCipher(bool forEncryption, Span<byte> nonce)
        {
            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(_keyParameter, _macBitSize, nonce.ToArray());

            cipher.Init(forEncryption, parameters);

            return cipher;
        }
    }
}
