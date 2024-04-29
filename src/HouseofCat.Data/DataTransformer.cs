using CommunityToolkit.HighPerformance;
using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Hashing;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HouseofCat.Data;

public class DataTransformer
{
    private readonly IEncryptionProvider _encryptionProvider;
    private readonly ICompressionProvider _compressionProvider;
    private readonly ISerializationProvider _serializationProvider;

    public DataTransformer(
        ISerializationProvider serializationProvider,
        IEncryptionProvider encryptionProvider = null,
        ICompressionProvider compressionProvider = null)
    {
        Guard.AgainstNull(serializationProvider, nameof(serializationProvider));

        _serializationProvider = serializationProvider;
        _encryptionProvider = encryptionProvider;
        _compressionProvider = compressionProvider;
    }

    public DataTransformer(
        string encryptionPassword = null,
        string encryptionSalt = null,
        int keySize = 32)
    {
        _serializationProvider = new JsonProvider();

        if (string.IsNullOrEmpty(encryptionPassword)
            && string.IsNullOrEmpty(encryptionSalt))
        {
            var hasingProvider = new ArgonHashingProvider();
            var aes256Key = hasingProvider.GetHashKey(
                encryptionPassword,
                encryptionSalt,
                keySize);

            _encryptionProvider = new AesGcmEncryptionProvider(aes256Key);
        }

        _compressionProvider = new GzipProvider();
    }

    /// <summary>
    /// Transforms data back to the original object.
    /// <para>Data was serialized, compressed, then encrypted. So here it is decrypted, decompressed, and deserialized.</para>
    /// </summary>
    /// <typeparam name="TOut"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<TOut> DeserializeAsync<TOut>(ReadOnlyMemory<byte> data)
    {
        if (_encryptionProvider is not null && _compressionProvider is not null)
        {
            return DecryptDecompressDeserializeAsync<TOut>(data);
        }
        else if (_encryptionProvider is not null)
        {
            return DecryptDeserializeAsync<TOut>(data);
        }
        else if (_compressionProvider is not null)
        {
            return DecompressDeserializeAsync<TOut>(data);
        }

        return _serializationProvider.DeserializeAsync<TOut>(data.AsStream());
    }

    /// <summary>
    /// Transforms data back to the original object.
    /// <para>Data was serialized, compressed, then encrypted. So here it is decrypted, decompressed, and deserialized.</para>
    /// </summary>
    /// <typeparam name="TOut"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut Deserialize<TOut>(ReadOnlyMemory<byte> data)
    {
        if (_encryptionProvider is not null && _compressionProvider is not null)
        {
            return DecryptDecompressDeserialize<TOut>(data);
        }
        else if (_encryptionProvider is not null)
        {
            return DecryptDeserialize<TOut>(data);
        }
        else if (_compressionProvider is not null)
        {
            return DecompressDeserialize<TOut>(data);
        }

        return _serializationProvider.Deserialize<TOut>(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut DecryptDecompressDeserialize<TOut>(ReadOnlyMemory<byte> data)
    {
        var decryptedData = _encryptionProvider.Decrypt(data);
        var decompressedData = _compressionProvider.Decompress(decryptedData);

        return _serializationProvider.Deserialize<TOut>(decompressedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<TOut> DecryptDecompressDeserializeAsync<TOut>(ReadOnlyMemory<byte> data)
    {
        var memoryStream = _encryptionProvider.DecryptToStream(data);
        memoryStream.Seek(0, SeekOrigin.Begin);

        memoryStream = await _compressionProvider
            .DecompressAsync(memoryStream)
            .ConfigureAwait(false);

        memoryStream.Seek(0, SeekOrigin.Begin);

        return await _serializationProvider
            .DeserializeAsync<TOut>(memoryStream)
            .ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut DecryptDeserialize<TOut>(ReadOnlyMemory<byte> data)
    {
        var decryptedData = _encryptionProvider.Decrypt(data);

        return _serializationProvider.Deserialize<TOut>(decryptedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<TOut> DecryptDeserializeAsync<TOut>(ReadOnlyMemory<byte> data)
    {
        var memoryStream = _encryptionProvider.DecryptToStream(data);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return await _serializationProvider
            .DeserializeAsync<TOut>(memoryStream)
            .ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut DecompressDeserialize<TOut>(ReadOnlyMemory<byte> data)
    {
        var decompressedData = _compressionProvider.Decompress(data);

        return _serializationProvider.Deserialize<TOut>(decompressedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<TOut> DecompressDeserializeAsync<TOut>(ReadOnlyMemory<byte> data)
    {
        var memoryStream = _compressionProvider.DecompressToStream(data);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return await _serializationProvider
            .DeserializeAsync<TOut>(memoryStream)
            .ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> Serialize<TIn>(TIn input)
    {
        if (_encryptionProvider is not null && _compressionProvider is not null)
        {
            return SerializeCompressEncrypt(input);
        }
        else if (_encryptionProvider is not null)
        {
            return SerializeEncrypt(input);
        }
        else if (_compressionProvider is not null)
        {
            return SerializeEncrypt(input);
        }

        return _serializationProvider.Serialize(input);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<ReadOnlyMemory<byte>> SerializeAsync<TIn>(TIn input)
    {
        if (_encryptionProvider is not null && _compressionProvider is not null)
        {
            return await SerializeCompressEncryptAsync(input).ConfigureAwait(false);
        }
        else if (_encryptionProvider is not null)
        {
            return await SerializeEncryptAsync(input).ConfigureAwait(false);
        }
        else if (_compressionProvider is not null)
        {
            return await SerializeEncryptAsync(input).ConfigureAwait(false);
        }

        return _serializationProvider.Serialize(input);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<ReadOnlyMemory<byte>> SerializeCompressEncryptAsync<TIn>(TIn input)
    {
        var memoryStream = new MemoryStream();
        await _serializationProvider
            .SerializeAsync(memoryStream, input)
            .ConfigureAwait(false);

        memoryStream.Seek(0, SeekOrigin.Begin);

        using var compressionStream = await _compressionProvider
            .CompressAsync(memoryStream)
            .ConfigureAwait(false);

        compressionStream.Seek(0, SeekOrigin.Begin);

        var encryptionStream = await _encryptionProvider
            .EncryptAsync(compressionStream)
            .ConfigureAwait(false);

        encryptionStream.Seek(0, SeekOrigin.Begin);

        return encryptionStream.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> SerializeCompressEncrypt<TIn>(TIn input)
    {
        return _encryptionProvider
            .Encrypt(_compressionProvider
            .Compress(_serializationProvider
            .Serialize(input)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<ReadOnlyMemory<byte>> SerializeEncryptAsync<TIn>(TIn input)
    {
        using var memoryStream = new MemoryStream();
        await _serializationProvider
            .SerializeAsync(memoryStream, input)
            .ConfigureAwait(false);

        memoryStream.Seek(0, SeekOrigin.Begin);

        using var compressionStream = await _encryptionProvider
            .EncryptAsync(memoryStream, false)
            .ConfigureAwait(false);

        compressionStream.Seek(0, SeekOrigin.Begin);

        return compressionStream.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> SerializeEncrypt<TIn>(TIn input)
    {
        return _encryptionProvider
            .Encrypt(_serializationProvider
            .Serialize(input));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<ReadOnlyMemory<byte>> SerializeCompressAsync<TIn>(TIn input)
    {
        using var memoryStream = new MemoryStream();
        await _serializationProvider.SerializeAsync(memoryStream, input);

        memoryStream.Seek(0, SeekOrigin.Begin);

        using var compressionStream = await _compressionProvider
            .CompressAsync(memoryStream, false)
            .ConfigureAwait(false);

        compressionStream.Seek(0, SeekOrigin.Begin);

        return compressionStream.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> SerializeCompress<TIn>(TIn input)
    {
        return _compressionProvider.Compress(_serializationProvider.Serialize(input));
    }
}
