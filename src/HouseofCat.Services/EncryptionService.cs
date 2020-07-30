using HouseofCat.Encryption;
using HouseofCat.Encryption.Hash;
using System;
using System.Threading.Tasks;
using static HouseofCat.Services.Enums;

namespace HouseofCat.Services
{
    public interface IEncryptionService
    {
        byte[] Decrypt(byte[] data, EncryptionMethod method);
        byte[] Encrypt(byte[] data, EncryptionMethod method);
        Task SetHashKeyAsync(string passphrase, string salt, EncryptionMethod method);
    }

    public class EncryptionService : IEncryptionService
    {
        private byte[] _argonHashKey;

        public async Task SetHashKeyAsync(string passphrase, string salt, EncryptionMethod method)
        {
            _argonHashKey = method switch
            {
                _ => await ArgonHash.GetHashKeyAsync(passphrase, salt, Encryption.Constants.Aes256.KeySize).ConfigureAwait(false),
            };
        }

        public byte[] Decrypt(byte[] data, EncryptionMethod method)
        {
            GuardAgainstBadHashKey(method);
            return method switch
            {

                EncryptionMethod.AES256_ARGON2ID => AesEncrypt.Aes256Decrypt(data, _argonHashKey),
                _ => AesEncrypt.Aes256Decrypt(data, _argonHashKey)
            };
        }

        public byte[] Encrypt(byte[] data, EncryptionMethod method)
        {
            GuardAgainstBadHashKey(method);
            return method switch
            {

                EncryptionMethod.AES256_ARGON2ID => AesEncrypt.Aes256Encrypt(data, _argonHashKey),
                _ => AesEncrypt.Aes256Encrypt(data, _argonHashKey)
            };
        }

        private void GuardAgainstBadHashKey(EncryptionMethod method)
        {
            if (method == EncryptionMethod.AES256_ARGON2ID
                && (_argonHashKey == null || _argonHashKey.Length == 0))
            { throw new ArgumentException(Constants.Errors.ArgonHashKeyNotSet); }
        }
    }
}
