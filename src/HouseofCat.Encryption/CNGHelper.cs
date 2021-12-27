using HouseofCat.Utilities.Errors;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace HouseofCat.Encryption
{
    public static class CNGHelper
    {
        public static int KeySize { get; set; } = 4096;

        private static readonly string _lengthPropertyName = "Length";

        /// <summary>
        /// Uses Windows based CNG to lookup the RSA KeyName and either return what is
        /// found or generate a new CNG key that is then used to make a new RSACng at will
        /// be used by all subsequent calls of GetOrCreateRSACng for encryption/decryption.
        /// </summary>
        /// <param name="rsaKeyName"></param>
        /// <returns>RSACng</returns>
        [SupportedOSPlatform("windows")]
        public static RSACng GetOrCreateRSACng(string rsaKeyName)
        {
            Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));

            CngKey cngKey;

            if (CngKey.Exists(rsaKeyName))
            {
                cngKey = CngKey.Open(rsaKeyName);
            }
            else
            {
                var cngLengthProperty = new CngProperty(
                    _lengthPropertyName,
                    BitConverter.GetBytes(KeySize),
                    CngPropertyOptions.None);

                var cngKeyCreationParameters = new CngKeyCreationParameters
                {
                    KeyUsage = CngKeyUsages.AllUsages,
                    ExportPolicy =
                          CngExportPolicies.AllowPlaintextExport
                        | CngExportPolicies.AllowExport
                        | CngExportPolicies.AllowArchiving
                        | CngExportPolicies.AllowPlaintextArchiving
                };

                cngKeyCreationParameters.Parameters.Add(cngLengthProperty);

                cngKey = CngKey.Create(CngAlgorithm.Rsa, rsaKeyName, cngKeyCreationParameters);
            }

            return new RSACng(cngKey)
            {
                KeySize = KeySize
            };
        }

        [SupportedOSPlatform("windows")]
        public static RSACng GetOrCreateRSACng(string rsaKeyName, CngKeyCreationParameters parameters = null)
        {
            Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));

            CngKey cngKey;

            if (CngKey.Exists(rsaKeyName))
            { cngKey = CngKey.Open(rsaKeyName); }
            else
            { cngKey = CngKey.Create(CngAlgorithm.Rsa, rsaKeyName, parameters); }

            return new RSACng(cngKey)
            {
                KeySize = KeySize
            };
        }

        private static readonly string NoBytesWereReadRsaPublicKeyError = "Unable to create RSA from PublicKey import. No bytes were read.";
        private static readonly string NoBytesWereReadRsaPrivateKeyError = "Unable to create RSA from PrivateKey import. No bytes were read.";
        private static readonly string NotSupportedError = "Linux/Mac have not been implemented yet.";
        private static readonly string RsaKeyNameDoesNotExistError = "RSA key ({0}) does not exist.";

        [SupportedOSPlatform("windows")]
        public static byte[] GetRSAPublicKey(string rsaKeyName)
        {
            Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));

            if (!CngKey.Exists(rsaKeyName)) throw new InvalidOperationException(string.Format(RsaKeyNameDoesNotExistError, rsaKeyName));

            var cngKey = CngKey.Open(rsaKeyName);
            var rsaCng = new RSACng(cngKey)
            {
                KeySize = KeySize
            };

            return rsaCng.ExportRSAPublicKey();
        }

        [SupportedOSPlatform("windows")]
        public static string GetRSAPublicKeyAsString(string rsaKeyName)
        {
            Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));

            if (!CngKey.Exists(rsaKeyName)) throw new InvalidOperationException(string.Format(RsaKeyNameDoesNotExistError, rsaKeyName));

            var cngKey = CngKey.Open(rsaKeyName);
            var rsaCng = new RSACng(cngKey)
            {
                KeySize = KeySize
            };

            return Encoding.UTF8.GetString(rsaCng.ExportRSAPublicKey());
        }

        [SupportedOSPlatform("windows")]
        public static MemoryStream GetRSAPublicKeyAsStream(string rsaKeyName)
        {
            Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));

            if (!CngKey.Exists(rsaKeyName)) throw new InvalidOperationException(string.Format(RsaKeyNameDoesNotExistError, rsaKeyName));

            var cngKey = CngKey.Open(rsaKeyName);
            var rsaCng = new RSACng(cngKey)
            {
                KeySize = KeySize
            };

            return new MemoryStream(rsaCng.ExportRSAPublicKey());
        }

        [SupportedOSPlatform("windows")]
        public static byte[] GetRSAPrivateKey(string rsaKeyName)
        {
            Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));

            if (!CngKey.Exists(rsaKeyName)) throw new InvalidOperationException($"RSA key ({rsaKeyName}) does not exist.");

            var cngKey = CngKey.Open(rsaKeyName);
            var rsaCng = new RSACng(cngKey)
            {
                KeySize = KeySize
            };

            return rsaCng.ExportRSAPrivateKey();
        }

        [SupportedOSPlatform("windows")]
        public static string GetRSAPrivateKeyAsString(string rsaKeyName)
        {
            Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));

            if (!CngKey.Exists(rsaKeyName)) throw new InvalidOperationException(string.Format(RsaKeyNameDoesNotExistError, rsaKeyName));

            var cngKey = CngKey.Open(rsaKeyName);
            var rsaCng = new RSACng(cngKey)
            {
                KeySize = KeySize
            };

            return Encoding.UTF8.GetString(rsaCng.ExportRSAPrivateKey());
        }

        [SupportedOSPlatform("windows")]
        public static MemoryStream GetRSAPrivateKeyAsStream(string rsaKeyName)
        {
            Guard.AgainstNullOrEmpty(rsaKeyName, nameof(rsaKeyName));

            if (!CngKey.Exists(rsaKeyName)) throw new InvalidOperationException(string.Format(RsaKeyNameDoesNotExistError, rsaKeyName));

            var cngKey = CngKey.Open(rsaKeyName);
            var rsaCng = new RSACng(cngKey)
            {
                KeySize = KeySize
            };

            return new MemoryStream(rsaCng.ExportRSAPrivateKey());
        }

        [SupportedOSPlatform("windows")]
        public static RSACng CreateRSACngFromPublicKey(byte[] publicKey)
        {
            Guard.AgainstNullOrEmpty(publicKey, nameof(publicKey));

            var rsaCng = new RSACng()
            {
                KeySize = KeySize
            };

            rsaCng.ImportRSAPublicKey(publicKey, out var bytesRead);

            if (bytesRead < 1) throw new InvalidOperationException(NoBytesWereReadRsaPublicKeyError);

            return rsaCng;
        }

        [SupportedOSPlatform("windows")]
        public static RSACng CreateRSACngFromPublicKey(string publicKey)
        {
            Guard.AgainstNullOrEmpty(publicKey, nameof(publicKey));

            var publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
            var rsaCng = new RSACng()
            {
                KeySize = KeySize
            };

            rsaCng.ImportRSAPublicKey(publicKeyBytes, out var bytesRead);

            if (bytesRead < 1) throw new InvalidOperationException(NoBytesWereReadRsaPublicKeyError);

            return rsaCng;
        }

        [SupportedOSPlatform("windows")]
        public static RSACng CreateRSACngFromPrivateKey(byte[] privateKey)
        {
            Guard.AgainstNullOrEmpty(privateKey, nameof(privateKey));

            var rsaCng = new RSACng()
            {
                KeySize = KeySize
            };

            rsaCng.ImportRSAPrivateKey(privateKey, out var bytesRead);

            if (bytesRead < 1) throw new InvalidOperationException(NoBytesWereReadRsaPrivateKeyError);

            return rsaCng;
        }

        [SupportedOSPlatform("windows")]
        public static RSACng CreateRSACngFromPrivateKey(string privateKey)
        {
            Guard.AgainstNullOrEmpty(privateKey, nameof(privateKey));

            var publicKeyBytes = Encoding.UTF8.GetBytes(privateKey);
            var rsaCng = new RSACng()
            {
                KeySize = KeySize
            };

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

        public static (IStreamEncryptionProvider, string encryptedKey) GetAesStreamEncryptionProvider(
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
                var rsaCng = GetOrCreateRSACng(rsaKeyName);
                var encryptedKey = Convert.ToBase64String(
                    rsaCng.Encrypt(aesKey, rsaPaddingMode ?? RSAEncryptionPadding.Pkcs1));

                return (aesEncryptionProvider, encryptedKey);
            }

            throw new PlatformNotSupportedException(NotSupportedError);
        }
    }
}
