using Konscious.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.Library.Encryption.Hash
{
    public static class ArgonHash
    {
        /// <summary>
        /// Create a Hash byte array using Argon2id.
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="salt"></param>
        /// <param name="size"></param>
        public static async Task<byte[]> GetHashKeyAsync(string passphrase, string salt, int size)
        {
            using var argon2 = GetArgon2id(Encoding.UTF8.GetBytes(passphrase), Encoding.UTF8.GetBytes(salt));

            return await argon2.GetBytesAsync(size).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a Hash byte array using Argon2id.
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="salt"></param>
        /// <param name="size"></param>
        public static async Task<byte[]> GetHashKeyAsync(string passphrase, byte[] salt, int size)
        {
            using var argon2 = GetArgon2id(Encoding.UTF8.GetBytes(passphrase), salt);

            return await argon2.GetBytesAsync(size).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a Hash byte array using Argon2id.
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="salt"></param>
        /// <param name="size"></param>
        public static async Task<byte[]> GetHashKeyAsync(byte[] passphrase, byte[] salt, int size)
        {
            using var argon2 = GetArgon2id(passphrase, salt);

            return await argon2.GetBytesAsync(size).ConfigureAwait(false);
        }

        private static Argon2id GetArgon2id(byte[] passphrase, byte[] salt)
        {
            return new Argon2id(passphrase)
            {
                DegreeOfParallelism = Constants.Argon.DoP,
                MemorySize = Constants.Argon.MemorySize,
                Salt = salt,
                Iterations = Constants.Argon.Iterations
            };
        }
    }
}
