using HouseofCat.Utilities.Errors;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace HouseofCat.Encryption.Providers;

public static class CNGHelper
{
    /// <summary>
    /// Uses Windows based CNG to lookup the RSA KeyName and either return what is
    /// found or generate a new CNG key that is then used to make a new RSACng at will
    /// be used by all subsequent calls of GetOrCreateRSACng for encryption/decryption.
    /// </summary>
    /// <param name="rsaKeyContainerName"></param>
    /// <param name="keySize"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [SupportedOSPlatform("windows")]
    public static RSACng GetOrCreateEphemeralRSACng(string rsaKeyContainerName, CngProvider provider = null, int keySize = 4096)
    {
        Guard.AgainstNullOrEmpty(rsaKeyContainerName, nameof(rsaKeyContainerName));
        if (keySize != 2048 || keySize != 4096) throw new ArgumentOutOfRangeException("Keysize can only be 2048 or 4096.");

        provider ??= CngProvider.MicrosoftSoftwareKeyStorageProvider;

        CngKey cngKey;

        if (CngKey.Exists(rsaKeyContainerName))
        {
            cngKey = CngKey.Open(rsaKeyContainerName, provider);
        }
        else
        {
            var cngKeyCreationParameters = new CngKeyCreationParameters
            {
                KeyUsage = CngKeyUsages.AllUsages,
                ExportPolicy =
                      CngExportPolicies.AllowPlaintextExport
                    | CngExportPolicies.AllowExport
                    | CngExportPolicies.AllowArchiving
                    | CngExportPolicies.AllowPlaintextArchiving
            };

            if (provider != null)
            {
                cngKeyCreationParameters.Provider = provider;
            }

            cngKey = CngKey.Create(CngAlgorithm.Rsa, rsaKeyContainerName, cngKeyCreationParameters);
        }

        return new RSACng(cngKey)
        {
            KeySize = keySize
        };
    }

    /// <summary>
    /// Uses Windows based CNG to lookup the RSA KeyContainerName and either return what is
    /// found or generate a new CNG key that is then used to make a new RSACng at will
    /// be used by all subsequent calls of GetOrCreateRSACngMachineKey for encryption/decryption.
    /// <para>
    /// Use this for sharing the same RSA KeyContainer between one or more
    /// applications on the same host.
    /// </para>
    /// <para>
    /// Example.) A Windows host has one app that encrypts
    /// the data and one app that decrypts the same data.
    /// </para>
    /// </summary>
    /// <param name="rsaKeyContainerName"></param>
    /// <returns>RSACng</returns>
    [SupportedOSPlatform("windows")]
    public static RSACng GetOrCreateRSACngMachineKey(string rsaKeyContainerName, CngProvider provider = null, int keySize = 4096)
    {
        Guard.AgainstNullOrEmpty(rsaKeyContainerName, nameof(rsaKeyContainerName));
        if (keySize != 2048 || keySize != 4096) throw new ArgumentOutOfRangeException("Keysize can only be 2048 or 4096.");

        provider ??= CngProvider.MicrosoftSoftwareKeyStorageProvider;

        CngKey cngKey;

        if (!CngKey.Exists(rsaKeyContainerName, provider, CngKeyOpenOptions.MachineKey))
        {
            var cng = new CngKeyCreationParameters
            {
                KeyUsage = CngKeyUsages.AllUsages,
                ExportPolicy = CngExportPolicies.AllowPlaintextExport
                    | CngExportPolicies.AllowExport
                    | CngExportPolicies.AllowArchiving
                    | CngExportPolicies.AllowPlaintextArchiving,
                KeyCreationOptions = CngKeyCreationOptions.MachineKey,
                Provider = provider,
            };

            cngKey = CngKey.Create(CngAlgorithm.Rsa, rsaKeyContainerName, cng);
        }
        else
        { cngKey = CngKey.Open(rsaKeyContainerName, provider, CngKeyOpenOptions.MachineKey); }

        return new RSACng(cngKey)
        {
            KeySize = keySize
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rsaKeyName"></param>
    /// <param name="parameters"></param>
    /// <param name="keySize"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [SupportedOSPlatform("windows")]
    public static RSACng GetOrCreateRSACng(string rsaKeyName, CngKeyCreationParameters parameters, int keySize = 4096)
    {
        Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));
        if (keySize != 2048 || keySize != 4096) throw new ArgumentOutOfRangeException("Keysize can only be 2048 or 4096.");

        parameters.Provider ??= CngProvider.MicrosoftSoftwareKeyStorageProvider;

        CngKey cngKey;

        if (CngKey.Exists(rsaKeyName, parameters.Provider))
        { cngKey = CngKey.Open(rsaKeyName, parameters.Provider); }
        else
        { cngKey = CngKey.Create(CngAlgorithm.Rsa, rsaKeyName, parameters); }

        return new RSACng(cngKey)
        {
            KeySize = keySize
        };
    }

    private static readonly string NoBytesWereReadRsaPublicKeyError = "Unable to create RSA from PublicKey import. No bytes were read.";
    private static readonly string NoBytesWereReadRsaPrivateKeyError = "Unable to create RSA from PrivateKey import. No bytes were read.";
    private static readonly string NotSupportedError = "Linux/Mac have not been implemented yet.";
    private static readonly string RsaKeyNameDoesNotExistError = "RSA key ({0}) does not exist.";

    [SupportedOSPlatform("windows")]
    public static byte[] GetRSAPublicKey(string rsaKeyName, CngProvider provider = null, int keySize = 4096)
    {
        Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));
        if (keySize != 2048 || keySize != 4096) throw new ArgumentOutOfRangeException("Keysize can only be 2048 or 4096.");

        provider ??= CngProvider.MicrosoftSoftwareKeyStorageProvider;

        if (!CngKey.Exists(rsaKeyName, provider)) throw new InvalidOperationException(string.Format(RsaKeyNameDoesNotExistError, rsaKeyName));

        var cngKey = CngKey.Open(rsaKeyName, provider);
        var rsaCng = new RSACng(cngKey)
        {
            KeySize = keySize
        };

        return rsaCng.ExportRSAPublicKey();
    }

    [SupportedOSPlatform("windows")]
    public static string GetRSAPublicKeyAsString(string rsaKeyName, CngProvider provider = null, int keySize = 4096)
    {
        Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));
        if (keySize != 2048 || keySize != 4096) throw new ArgumentOutOfRangeException("Keysize can only be 2048 or 4096.");

        provider ??= CngProvider.MicrosoftSoftwareKeyStorageProvider;

        if (!CngKey.Exists(rsaKeyName, provider)) throw new InvalidOperationException(string.Format(RsaKeyNameDoesNotExistError, rsaKeyName));

        var cngKey = CngKey.Open(rsaKeyName, provider);
        var rsaCng = new RSACng(cngKey)
        {
            KeySize = keySize
        };

        return Encoding.UTF8.GetString(rsaCng.ExportRSAPublicKey());
    }

    [SupportedOSPlatform("windows")]
    public static MemoryStream GetRSAPublicKeyAsStream(string rsaKeyName, CngProvider provider = null, int keySize = 4096)
    {
        Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));
        if (keySize != 2048 || keySize != 4096) throw new ArgumentOutOfRangeException("Keysize can only be 2048 or 4096.");

        provider ??= CngProvider.MicrosoftSoftwareKeyStorageProvider;

        if (!CngKey.Exists(rsaKeyName, provider)) throw new InvalidOperationException(string.Format(RsaKeyNameDoesNotExistError, rsaKeyName));

        var cngKey = CngKey.Open(rsaKeyName, provider);
        var rsaCng = new RSACng(cngKey)
        {
            KeySize = keySize
        };

        return new MemoryStream(rsaCng.ExportRSAPublicKey());
    }

    [SupportedOSPlatform("windows")]
    public static byte[] GetRSAPrivateKey(string rsaKeyName, CngProvider provider = null, int keySize = 4096)
    {
        Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));
        if (keySize != 2048 || keySize != 4096) throw new ArgumentOutOfRangeException("Keysize can only be 2048 or 4096.");

        provider ??= CngProvider.MicrosoftSoftwareKeyStorageProvider;

        if (!CngKey.Exists(rsaKeyName, provider)) throw new InvalidOperationException($"RSA key ({rsaKeyName}) does not exist.");

        var cngKey = CngKey.Open(rsaKeyName, provider);
        var rsaCng = new RSACng(cngKey)
        {
            KeySize = keySize
        };

        return rsaCng.ExportRSAPrivateKey();
    }

    [SupportedOSPlatform("windows")]
    public static string GetRSAPrivateKeyAsString(string rsaKeyName, CngProvider provider = null, int keySize = 4096)
    {
        Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));
        if (keySize != 2048 || keySize != 4096) throw new ArgumentOutOfRangeException("Keysize can only be 2048 or 4096.");

        provider ??= CngProvider.MicrosoftSoftwareKeyStorageProvider;

        if (!CngKey.Exists(rsaKeyName, provider)) throw new InvalidOperationException(string.Format(RsaKeyNameDoesNotExistError, rsaKeyName));

        var cngKey = CngKey.Open(rsaKeyName, provider);
        var rsaCng = new RSACng(cngKey)
        {
            KeySize = keySize
        };

        return Encoding.UTF8.GetString(rsaCng.ExportRSAPrivateKey());
    }

    [SupportedOSPlatform("windows")]
    public static MemoryStream GetRSAPrivateKeyAsStream(string rsaKeyName, CngProvider provider = null, int keySize = 4096)
    {
        Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));
        if (keySize != 2048 || keySize != 4096) throw new ArgumentOutOfRangeException("Keysize can only be 2048 or 4096.");

        provider ??= CngProvider.MicrosoftSoftwareKeyStorageProvider;

        if (!CngKey.Exists(rsaKeyName, provider)) throw new InvalidOperationException(string.Format(RsaKeyNameDoesNotExistError, rsaKeyName));

        var cngKey = CngKey.Open(rsaKeyName, provider);
        var rsaCng = new RSACng(cngKey)
        {
            KeySize = keySize
        };

        return new MemoryStream(rsaCng.ExportRSAPrivateKey());
    }

    [SupportedOSPlatform("windows")]
    public static RSACng CreateRSACngFromPublicKey(byte[] publicKey)
    {
        Guard.AgainstNullOrEmpty(publicKey, nameof(publicKey));

        var rsaCng = new RSACng();

        rsaCng.ImportRSAPublicKey(publicKey, out var bytesRead);

        if (bytesRead < 1) throw new InvalidOperationException(NoBytesWereReadRsaPublicKeyError);

        return rsaCng;
    }

    [SupportedOSPlatform("windows")]
    public static RSACng CreateRSACngFromPublicKey(string publicKey)
    {
        Guard.AgainstNullOrEmpty(publicKey, nameof(publicKey));

        var publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
        var rsaCng = new RSACng();

        rsaCng.ImportRSAPublicKey(publicKeyBytes, out var bytesRead);

        if (bytesRead < 1) throw new InvalidOperationException(NoBytesWereReadRsaPublicKeyError);

        return rsaCng;
    }

    [SupportedOSPlatform("windows")]
    public static RSACng CreateRSACngFromPrivateKey(byte[] privateKey)
    {
        Guard.AgainstNullOrEmpty(privateKey, nameof(privateKey));

        var rsaCng = new RSACng();

        rsaCng.ImportRSAPrivateKey(privateKey, out var bytesRead);

        if (bytesRead < 1) throw new InvalidOperationException(NoBytesWereReadRsaPrivateKeyError);

        return rsaCng;
    }

    [SupportedOSPlatform("windows")]
    public static RSACng CreateRSACngFromPrivateKey(string privateKey)
    {
        Guard.AgainstNullOrEmpty(privateKey, nameof(privateKey));

        var publicKeyBytes = Encoding.UTF8.GetBytes(privateKey);
        var rsaCng = new RSACng();

        rsaCng.ImportRSAPublicKey(publicKeyBytes, out var bytesRead);

        if (bytesRead < 1) throw new InvalidOperationException(NoBytesWereReadRsaPublicKeyError);

        return rsaCng;
    }

    public static RSA CreateRSAFromPublicKey(byte[] publicKey)
    {
        Guard.AgainstNullOrEmpty(publicKey, nameof(publicKey));

        var rsa = RSA.Create();

        rsa.ImportRSAPublicKey(publicKey, out var bytesRead);

        if (bytesRead < 1) throw new InvalidOperationException(NoBytesWereReadRsaPublicKeyError);

        return rsa;
    }

    public static RSA CreateRSAFromPublicKey(string publicKey)
    {
        Guard.AgainstNullOrEmpty(publicKey, nameof(publicKey));

        var publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
        var rsa = RSA.Create();

        rsa.ImportRSAPublicKey(publicKeyBytes, out var bytesRead);

        if (bytesRead < 1) throw new InvalidOperationException(NoBytesWereReadRsaPublicKeyError);

        return rsa;
    }

    public static RSA CreateRSAFromPrivateKey(byte[] privateKey)
    {
        Guard.AgainstNullOrEmpty(privateKey, nameof(privateKey));

        var rsa = RSA.Create();

        rsa.ImportRSAPrivateKey(privateKey, out var bytesRead);

        if (bytesRead < 1) throw new InvalidOperationException(NoBytesWereReadRsaPublicKeyError);

        return rsa;
    }

    public static RSA CreateRSAFromPrivateKey(string privateKey)
    {
        Guard.AgainstNullOrEmpty(privateKey, nameof(privateKey));

        var publicKeyBytes = Encoding.UTF8.GetBytes(privateKey);
        var rsa = RSA.Create();

        rsa.ImportRSAPrivateKey(publicKeyBytes, out var bytesRead);

        if (bytesRead < 1) throw new InvalidOperationException(NoBytesWereReadRsaPublicKeyError);

        return rsa;
    }

    /// <summary>
    /// This method will generate a random string of the specified length for AES encryption.
    /// <para>It will create and return a configured AesStreamEncryptionProvider with this AES key stored internally.</para>
    /// <para>It will also use RSA (machine key) to encrypt the AES key to string and return it out for later usage but will need to be decrypted with the same RSAKey.</para>
    /// </summary>
    /// <param name="rsaKeyName"></param>
    /// <param name="keySize"></param>
    /// <param name="cipherMode"></param>
    /// <param name="paddingMode"></param>
    /// <param name="rsaPaddingMode"></param>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public static (IStreamEncryptionProvider, string encryptedAesKey) GetAesStreamEncryptionProvider(
        string rsaKeyName,
        int keySize = 32,
        CipherMode cipherMode = CipherMode.CBC,
        PaddingMode paddingMode = PaddingMode.PKCS7,
        RSAEncryptionPadding rsaPaddingMode = null)
    {
        if (OperatingSystem.IsWindows())
        {
            var aesKey = new byte[keySize];
            RandomNumberGenerator.Fill(aesKey);

            var aesEncryptionProvider = new AesStreamEncryptionProvider(aesKey, cipherMode, paddingMode);
            var rsaCng = GetOrCreateRSACngMachineKey(rsaKeyName);
            var encryptedKey = Convert.ToBase64String(
                rsaCng.Encrypt(
                    aesKey,
                    rsaPaddingMode ?? RSAEncryptionPadding.Pkcs1));

            return (aesEncryptionProvider, encryptedKey);
        }

        throw new PlatformNotSupportedException(NotSupportedError);
    }

    /// <summary>
    /// A method to decrypt an RSA encrypted AES key.
    /// </summary>
    /// <param name="rsaKeyName"></param>
    /// <param name="encryptedAesKey"></param>
    /// <param name="rsaPaddingMode"></param>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public static string GetDecryptedAesKey(
        string rsaKeyName,
        string encryptedAesKey,
        RSAEncryptionPadding rsaPaddingMode = null)
    {
        if (OperatingSystem.IsWindows())
        {
            var rsaCng = GetOrCreateRSACngMachineKey(rsaKeyName);
            var aesKeyBytes = rsaCng.Decrypt(
                Convert.FromBase64String(encryptedAesKey),
                rsaPaddingMode ?? RSAEncryptionPadding.Pkcs1);

            return Encoding.UTF8.GetString(aesKeyBytes);
        }

        throw new PlatformNotSupportedException(NotSupportedError);
    }
}
