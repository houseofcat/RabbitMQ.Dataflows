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
        public readonly IEncryptionProvider EncryptionProvider;
        public readonly ICompressionProvider CompressionProvider;
        public readonly ISerializationProvider SerializationProvider;

        public RecyclableTransformer(
            ISerializationProvider serializationProvider,
            IEncryptionProvider encryptionProvider,
            ICompressionProvider compressionProvider)
        {
            Guard.AgainstNull(serializationProvider, nameof(serializationProvider));
            Guard.AgainstNull(encryptionProvider, nameof(encryptionProvider));
            Guard.AgainstNull(compressionProvider, nameof(compressionProvider));

            SerializationProvider = serializationProvider;
            EncryptionProvider = encryptionProvider;
            CompressionProvider = compressionProvider;
        }

        /// <summary>
        /// Returns the bytes (which was the buffer) and actual length to use.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public (ArraySegment<byte>, long) Input<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            SerializationProvider.Serialize(serializedStream, input);

            using var compressedStream = CompressionProvider.Compress(serializedStream, false);
            var encryptedStream = EncryptionProvider.Encrypt(compressedStream, false);

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
        public async Task<(ArraySegment<byte>, long)> InputAsync<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));

            await SerializationProvider
                .SerializeAsync(serializedStream, input)
                .ConfigureAwait(false);

            using var compressedStream = await CompressionProvider
                .CompressAsync(serializedStream, false)
                .ConfigureAwait(false);

            var encryptedStream = await EncryptionProvider
                .EncryptAsync(compressedStream, false)
                .ConfigureAwait(false);

            var length = encryptedStream.Length;
            if (encryptedStream.TryGetBuffer(out var buffer))
            { return (buffer, length); }
            else
            { return (encryptedStream.ToArray(), length); }
        }

        public MemoryStream InputToStream<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
            SerializationProvider.Serialize(serializedStream, input);

            using var compressedStream = CompressionProvider.Compress(serializedStream, false);

            return EncryptionProvider.Encrypt(compressedStream, true);
        }

        public async Task<Stream> InputToStreamAsync<TIn>(TIn input)
        {
            using var serializedStream = RecyclableManager.GetStream();
            await SerializationProvider
                .SerializeAsync(serializedStream, input)
                .ConfigureAwait(false);

            using var compressedStream = await CompressionProvider
                .CompressAsync(serializedStream, false)
                .ConfigureAwait(false);

            return await EncryptionProvider
                .EncryptAsync(compressedStream, true)
                .ConfigureAwait(false);
        }

        public TOut Output<TOut>(ReadOnlyMemory<byte> data)
        {
            var memoryStream = EncryptionProvider.DecryptToStream(data);
            memoryStream = CompressionProvider.Decompress(memoryStream, false);
            using (memoryStream)
            {
                return SerializationProvider.Deserialize<TOut>(memoryStream);
            }
        }

        public TOut Output<TOut>(Stream data)
        {
            using var decryptedStream = EncryptionProvider.Decrypt(data);
            using var decompressedStream = CompressionProvider.Decompress(decryptedStream, false);
            return SerializationProvider.Deserialize<TOut>(decompressedStream);
        }

        public async Task<TOut> OutputAsync<TOut>(ReadOnlyMemory<byte> data)
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
