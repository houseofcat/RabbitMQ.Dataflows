using HouseofCat.Encryption.Hash;
using HouseofCat.Utilities.Errors;
using System;
using static HouseofCat.Encryption.Enums;

namespace HouseofCat.Encryption
{
    public class ArgonAesEncryptionProvider : IEncryptionProvider
    {
        private byte[] _argonHashKey;
        private EncryptionOptions _options;

        public ArgonAesEncryptionProvider(string passphrase, string salt, EncryptionOptions options = null)
        {
            Guard.AgainstNull(passphrase, nameof(passphrase));
            Guard.AgainstNull(salt, nameof(salt));

            _options = options;
            _argonHashKey = ArgonHash.GetHashKeyAsync(passphrase, salt, Encryption.Constants.Aes256.KeySize, _options?.HashOptions).GetAwaiter().GetResult();
        }

        public byte[] Decrypt(ReadOnlyMemory<byte> data, EncryptionMethod method)
        {
            return EncryptionHelper.Encrypt(data, method, _argonHashKey, _options);
        }

        public byte[] Encrypt(ReadOnlyMemory<byte> data, EncryptionMethod method)
        {
            return EncryptionHelper.Decrypt(data, method, _argonHashKey, _options);
        }
    }
}
