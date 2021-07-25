using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Recyclable;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Data.Recyclable
{
    public class RecyclableTransformer
    {
        public readonly RecyclableAesGcmEncryptionProvider EncryptionProvider;
        public readonly RecyclableGzipProvider CompressionProvider;
        public readonly ISerializationProvider SerializationProvider;

        public RecyclableTransformer(
            ISerializationProvider serializationProvider,
            RecyclableGzipProvider compressionProvider,
            RecyclableAesGcmEncryptionProvider encryptionProvider)
        {
            Guard.AgainstNull(serializationProvider, nameof(serializationProvider));
            Guard.AgainstNull(compressionProvider, nameof(compressionProvider));
            Guard.AgainstNull(encryptionProvider, nameof(encryptionProvider));

            SerializationProvider = serializationProvider;
            CompressionProvider = compressionProvider;
            EncryptionProvider = encryptionProvider;
        }

        /// <summary>
        /// Returns the bytes (which was the buffer) and actual length to use.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public (ArraySegment<byte>, long) Transform<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            SerializationProvider.Serialize(serializedStream, input);

            using var compressedStream = CompressionProvider.Compress(serializedStream, true);
            var encryptedStream = EncryptionProvider.Encrypt(compressedStream, true);

            var length = encryptedStream.Length;
            if (encryptedStream.TryGetBuffer(out var buffer))
            { return (buffer, length); }
            else
            { return (encryptedStream.ToArray(), length); }
        }

        /// <summary>
        /// Returns the bytes (which was the buffer) and actual length to use.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<(ArraySegment<byte>, long)> TransformAsync<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            await SerializationProvider
                .SerializeAsync(serializedStream, input)
                .ConfigureAwait(false);

            using var compressedStream = await CompressionProvider
                .CompressAsync(serializedStream, true)
                .ConfigureAwait(false);

            var encryptedStream = await EncryptionProvider
                .EncryptAsync(compressedStream, true)
                .ConfigureAwait(false);

            var length = encryptedStream.Length;
            if (encryptedStream.TryGetBuffer(out var buffer))
            { return (buffer, length); }
            else
            { return (encryptedStream.ToArray(), length); }
        }

        public MemoryStream TransformToStream<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            SerializationProvider.Serialize(serializedStream, input);

            using var compressedStream = CompressionProvider.Compress(serializedStream, true);

            return EncryptionProvider.Encrypt(compressedStream, true);
        }

        public async Task<MemoryStream> TransformToStreamAsync<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream();
            await SerializationProvider
                .SerializeAsync(serializedStream, input)
                .ConfigureAwait(false);

            using var compressedStream = await CompressionProvider
                .CompressAsync(serializedStream, true)
                .ConfigureAwait(false);

            return await EncryptionProvider
                .EncryptAsync(compressedStream, true)
                .ConfigureAwait(false);
        }

        public TOut Restore<TOut>(ReadOnlyMemory<byte> data)
        {
            var memoryStream = EncryptionProvider.DecryptToStream(data);
            memoryStream = CompressionProvider.Decompress(memoryStream, true);
            using (memoryStream)
            {
                return SerializationProvider.Deserialize<TOut>(memoryStream);
            }
        }

        public TOut Restore<TOut>(Stream data)
        {
            using var decryptedStream = EncryptionProvider.Decrypt(data);
            using var decompressedStream = CompressionProvider.Decompress(decryptedStream, true);
            return SerializationProvider.Deserialize<TOut>(decompressedStream);
        }

        public async Task<TOut> RestoreAsync<TOut>(ReadOnlyMemory<byte> data)
        {
            var memoryStream = EncryptionProvider.DecryptToStream(data);

            memoryStream = await CompressionProvider
                .DecompressAsync(memoryStream, true)
                .ConfigureAwait(false);

            using (memoryStream)
            {
                return await SerializationProvider
                    .DeserializeAsync<TOut>(memoryStream)
                    .ConfigureAwait(false);
            }
        }
    }
}
