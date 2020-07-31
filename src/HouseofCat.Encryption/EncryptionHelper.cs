using System;
using static HouseofCat.Encryption.Enums;

namespace HouseofCat.Encryption
{
    public static class EncryptionHelper
    {
        public static byte[] Decrypt(ReadOnlyMemory<byte> data, EncryptionMethod method, ReadOnlyMemory<byte> hashKey, EncryptionOptions options = null)
        {
            GuardAgainstBadHashKey(method, hashKey);
            return method switch
            {
                EncryptionMethod.AES256_ARGON2ID => AesEncrypt.Aes256Decrypt(data, hashKey, options),
                _ => AesEncrypt.Aes256Decrypt(data, hashKey)
            };
        }

        public static byte[] Encrypt(ReadOnlyMemory<byte> data, EncryptionMethod method, ReadOnlyMemory<byte> hashKey, EncryptionOptions options = null)
        {
            GuardAgainstBadHashKey(method, hashKey);
            return method switch
            {
                EncryptionMethod.AES256_ARGON2ID => AesEncrypt.Aes256Encrypt(data, hashKey, options),
                _ => AesEncrypt.Aes256Encrypt(data, hashKey)
            };
        }

        private static void GuardAgainstBadHashKey(EncryptionMethod method, ReadOnlyMemory<byte> hashKey)
        {
            if (method == EncryptionMethod.AES256_ARGON2ID && hashKey.Length != Constants.Aes256.KeySize)
            { throw new ArgumentException(Constants.Argon.ArgonHashKeyNotSet); }
        }
    }
}
