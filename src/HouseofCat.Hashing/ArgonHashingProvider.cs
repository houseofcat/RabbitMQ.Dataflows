using HouseofCat.Utilities.Errors;
using Konscious.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.Hashing;

// https://github.com/P-H-C/phc-winner-argon2/blob/master/README.md
public sealed class ArgonHashingProvider : IHashingProvider
{
    // Recommend 4 threads for most security scenarios.
    // Recommend 1 thread for low security scenarios.
    public static int DoP { get; set; } = 2;

    // CAUTION: You most likely need this much RAM per Hash.
    // Recommend 2 GB for the highest security scenarios.
    // Recommend a minimum of 64 MB in high security scenarios.
    // Recommend a minimum of 2 MB in low security scenarios.
    public static int MemorySize { get; set; } = 1024 * 64;

    // Recommend a minimum of 3 for most security scenarios.
    public static int Iterations { get; set; } = 3;

    private readonly int _degreesofParallelism;
    private readonly int _memorySize;
    private readonly int _iterations;

    public string Type { get; } = "ARGON2ID";

    public ArgonHashingProvider(ArgonHashOptions options = null)
    {
        _degreesofParallelism = options?.DoP ?? DoP;
        _memorySize = options?.MemorySize ?? MemorySize;
        _iterations = options?.Iterations ?? Iterations;
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

        using var argon2 = GetArgon2id(
            Encoding.UTF8.GetBytes(passphrase),
            Encoding.UTF8.GetBytes(salt ?? string.Empty));

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

        using var argon2 = GetArgon2id(
            Encoding.UTF8.GetBytes(passphrase),
            Encoding.UTF8.GetBytes(salt ?? string.Empty));

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
