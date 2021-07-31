using HouseofCat.Utilities.Errors;
using Konscious.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.Hashing.Argon
{
    public class Argon2ID_HashingProvider : IHashingProvider
    {
        private readonly int _degreesofParallelism;
        private readonly int _memorySize;
        private readonly int _iterations;

        public string Type { get; } = "ARGON2ID";

        public Argon2ID_HashingProvider(ArgonHashOptions options = null)
        {
            _degreesofParallelism = options?.DoP ?? Constants.Argon.DoP;
            _memorySize = options?.MemorySize ?? Constants.Argon.MemorySize;
            _iterations = options?.Iterations ?? Constants.Argon.Iterations;
        }

        /// <summary>
        /// Create a Hash byte array using Argon2ID.
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="salt"></param>
        /// <param name="size"></param>
        public byte[] GetHashKey(string passphrase, string salt, int size)
        {
            Guard.AgainstNull(passphrase, nameof(passphrase));

            using var argon2 = GetArgon2id(Encoding.UTF8.GetBytes(passphrase), Encoding.UTF8.GetBytes(salt ?? string.Empty));

            return argon2.GetBytes(size);
        }

        /// <summary>
        /// Create a Hash byte array using Argon2id with UTF8 string inputs.
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="salt"></param>
        /// <param name="size"></param>
        public async Task<byte[]> GetHashKeyAsync(string passphrase, string salt, int size)
        {
            Guard.AgainstNull(passphrase, nameof(passphrase));

            using var argon2 = GetArgon2id(Encoding.UTF8.GetBytes(passphrase), Encoding.UTF8.GetBytes(salt ?? string.Empty));

            return await argon2.GetBytesAsync(size).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a Hash byte array using Argon2id.
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="salt"></param>
        /// <param name="size"></param>
        public byte[] GetHashKey(byte[] passphrase, byte[] salt, int size)
        {
            Guard.AgainstNullOrEmpty(passphrase, nameof(passphrase));

            using var argon2 = GetArgon2id(passphrase, salt);

            return argon2.GetBytes(size);
        }

        /// <summary>
        /// Create a Hash byte array using Argon2id with UTF8 string inputs.
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="salt"></param>
        /// <param name="size"></param>
        public async Task<byte[]> GetHashKeyAsync(byte[] passphrase, byte[] salt, int size)
        {
            Guard.AgainstNullOrEmpty(passphrase, nameof(passphrase));

            using var argon2 = GetArgon2id(passphrase, salt);

            return await argon2.GetBytesAsync(size).ConfigureAwait(false);
        }

        private Argon2id GetArgon2id(byte[] passphrase, byte[] salt)
        {
            return new Argon2id(passphrase)
            {
                DegreeOfParallelism = _degreesofParallelism,
                MemorySize = _memorySize,
                Salt = salt,
                Iterations = _iterations
            };
        }
    }
}
