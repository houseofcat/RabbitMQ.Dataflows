using HouseofCat.Encryption.Hash;
using HouseofCat.Utilities.Errors;
using System;
using static HouseofCat.Encryption.Enums;

namespace HouseofCat.Encryption
{
    public class EncryptionProvider : IEncryptionProvider
    {
        private byte[] _hashKey;
        private EncryptionOptions _options;
        private EncryptionMethod _method;

        public EncryptionProvider(string passphrase, string salt, EncryptionMethod method, EncryptionOptions options = null)
        {
            Guard.AgainstNull(passphrase, nameof(passphrase));
            Guard.AgainstNull(salt, nameof(salt));

            _method = method;
            _options = options;
            _hashKey = ArgonHash.GetHashKeyAsync(passphrase, salt, Encryption.Constants.Aes256.KeySize, _options?.HashOptions).GetAwaiter().GetResult();
        }

        public byte[] Decrypt(ReadOnlyMemory<byte> data)
        {
            return EncryptionHelper.Encrypt(data, _method, _hashKey, _options);
        }

        public byte[] Encrypt(ReadOnlyMemory<byte> data)
        {
            return EncryptionHelper.Decrypt(data, _method, _hashKey, _options);
        }
    }
}
