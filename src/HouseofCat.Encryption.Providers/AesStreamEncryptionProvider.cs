using HouseofCat.Utilities.Errors;
using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;

namespace HouseofCat.Encryption.Providers;

public sealed class AesStreamEncryptionProvider : IStreamEncryptionProvider
{
    public string Type { get; }

    private readonly ReadOnlyMemory<byte> _key;
    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    private readonly CipherMode _cipherMode;
    private readonly PaddingMode _paddingMode;

    public AesStreamEncryptionProvider(
        ReadOnlyMemory<byte> key,
        CipherMode cipherMode = CipherMode.CBC,
        PaddingMode paddingMode = PaddingMode.PKCS7)
    {
        Guard.AgainstEmpty(key, nameof(key));

        if (!Constants.Aes.ValidKeySizes.Contains(key.Length)) throw new ArgumentException("Keysize is an invalid length.");
        _key = key;

        _cipherMode = cipherMode;
        _paddingMode = paddingMode;

        switch (_key.Length)
        {
            case 16: Type = $"AES{_cipherMode}-128_Padding-{_paddingMode}"; break;
            case 24: Type = $"AES{_cipherMode}-192_Padding-{_paddingMode}"; break;
            case 32: Type = $"AES{_cipherMode}-256_Padding-{_paddingMode}"; break;
        }
    }

    /// <summary>
    /// Creates a CryptoStream from the input Stream, but with Nonce/IV size and nonce pre-appended to the Stream
    /// before creating the CryptoStream. Primarily intended to be used with FileStreams.
    /// <para>
    /// Byte Structure
    /// </para>
    /// <para>
    /// IV Nonce Length (NL)
    /// [ 0 - 3 ]
    /// </para>
    /// <para>
    /// IV Nonce
    /// [ 3 - (NL) ]
    /// </para>
    /// <para>
    /// CipherText
    /// [ (NL) - n ]
    /// </para>
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public CryptoStream GetEncryptStream(Stream stream, bool leaveOpen = false)
    {
        stream.Seek(0, SeekOrigin.Begin);

        using Aes aes = Aes.Create();
        aes.Key = _key.ToArray();
        aes.Mode = _cipherMode;
        aes.Padding = _paddingMode;

        var nonceSize = aes.BlockSize / 8;
        stream.Write(BitConverter.GetBytes(nonceSize));
        stream.Write(aes.IV, 0, aes.IV.Length);

        return new CryptoStream(
            stream,
            aes.CreateEncryptor(aes.Key, aes.IV),
            CryptoStreamMode.Write,
            leaveOpen);
    }

    /// <summary>
    /// Creates a CryptoStream from the input Stream, but with Nonce/IV size and nonce pre-read to the Stream
    /// before creating the CryptoStream. Primarily designed for FileStreams.
    /// <para>
    /// Byte Structure
    /// </para>
    /// <para>
    /// IV Nonce Length (NL)
    /// [ 0 - 3 ]
    /// </para>
    /// <para>
    /// IV Nonce
    /// [ 3 - (NL) ]
    /// </para>
    /// <para>
    /// CipherText
    /// [ (NL) - n ]
    /// </para>
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public CryptoStream GetDecryptStream(Stream stream, bool leaveOpen = false)
    {
        stream.Seek(0, SeekOrigin.Begin);

        using Aes aes = Aes.Create();
        aes.Key = _key.ToArray();
        aes.Mode = _cipherMode;
        aes.Padding = _paddingMode;

        var nonceSizeBytes = _pool.Rent(4);
        stream.Read(nonceSizeBytes, 0, 4);

        var nonceSize = BitConverter.ToInt32(nonceSizeBytes);
        var nonce = _pool.Rent(nonceSize);

        stream.Read(nonce, 0, nonceSize);

        aes.IV = nonce
            .AsSpan()
            .Slice(0, nonceSize)
            .ToArray();

        _pool.Return(nonceSizeBytes);
        _pool.Return(nonce);

        return new CryptoStream(
            stream,
            aes.CreateDecryptor(aes.Key, aes.IV),
            CryptoStreamMode.Read,
            leaveOpen);
    }
}
