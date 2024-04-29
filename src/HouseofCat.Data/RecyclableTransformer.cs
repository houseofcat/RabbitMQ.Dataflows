using HouseofCat.Compression.Recyclable;
using HouseofCat.Encryption;
using HouseofCat.Serialization;
using HouseofCat.Utilities;
using HouseofCat.Utilities.Errors;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Data;

public class RecyclableTransformer
{
    private readonly RecyclableAesGcmEncryptionProvider _encryptionProvider;
    private readonly RecyclableGzipProvider _compressionProvider;
    private readonly ISerializationProvider _serializationProvider;

    public RecyclableTransformer(
        ISerializationProvider serializationProvider,
        RecyclableGzipProvider compressionProvider,
        RecyclableAesGcmEncryptionProvider encryptionProvider)
    {
        Guard.AgainstNull(serializationProvider, nameof(serializationProvider));
        Guard.AgainstNull(compressionProvider, nameof(compressionProvider));
        Guard.AgainstNull(encryptionProvider, nameof(encryptionProvider));

        _serializationProvider = serializationProvider;
        _compressionProvider = compressionProvider;
        _encryptionProvider = encryptionProvider;
    }

    /// <summary>
    /// Returns the bytes (which was the buffer) and actual length to use.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <param name="input"></param>
    /// <returns></returns>
    public ReadOnlyMemory<byte> Transform<TIn>(TIn input)
    {
        using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
        _serializationProvider.Serialize(serializedStream, input);

        using var compressedStream = _compressionProvider.Compress(serializedStream, false);
        using var encryptedStream = _encryptionProvider.Encrypt(compressedStream, false);

        return encryptedStream.ToArray();
    }

    /// <summary>
    /// Returns the bytes (which was the buffer) and actual length to use.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<ReadOnlyMemory<byte>> TransformAsync<TIn>(TIn input)
    {
        using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
        await _serializationProvider
            .SerializeAsync(serializedStream, input)
            .ConfigureAwait(false);

        using var compressedStream = await _compressionProvider
            .CompressAsync(serializedStream, false)
            .ConfigureAwait(false);

        using var encryptedStream = await _encryptionProvider
            .EncryptAsync(compressedStream, false)
            .ConfigureAwait(false);

        return encryptedStream.ToArray();
    }

    public MemoryStream TransformToStream<TIn>(TIn input)
    {
        using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
        _serializationProvider.Serialize(serializedStream, input);

        using var compressedStream = _compressionProvider.Compress(serializedStream, false);

        return _encryptionProvider.Encrypt(compressedStream, false);
    }

    public async Task<MemoryStream> TransformToStreamAsync<TIn>(TIn input)
    {
        using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
        await _serializationProvider
            .SerializeAsync(serializedStream, input)
            .ConfigureAwait(false);

        using var compressedStream = await _compressionProvider
            .CompressAsync(serializedStream, false)
            .ConfigureAwait(false);

        return await _encryptionProvider
            .EncryptAsync(compressedStream, false)
            .ConfigureAwait(false);
    }

    public TOut Restore<TOut>(ReadOnlyMemory<byte> data)
    {
        using var decryptStream = _encryptionProvider.DecryptToStream(data);
        using var decompressStream = _compressionProvider.Decompress(decryptStream, false);
        return _serializationProvider.Deserialize<TOut>(decompressStream);
    }

    public TOut Restore<TOut>(MemoryStream data)
    {
        using var decryptedStream = _encryptionProvider.Decrypt(data, false);
        using var decompressedStream = _compressionProvider.Decompress(decryptedStream, false);
        return _serializationProvider.Deserialize<TOut>(decompressedStream);
    }

    public async Task<TOut> RestoreAsync<TOut>(ReadOnlyMemory<byte> data)
    {
        using var decryptedStream = _encryptionProvider.DecryptToStream(data);
        using var compressionStream = await _compressionProvider
            .DecompressAsync(decryptedStream, false)
            .ConfigureAwait(false);

        return await _serializationProvider
            .DeserializeAsync<TOut>(compressionStream)
            .ConfigureAwait(false);
    }
}
