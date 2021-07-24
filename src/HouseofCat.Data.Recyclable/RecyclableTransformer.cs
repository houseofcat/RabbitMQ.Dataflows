using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Recyclable;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using Microsoft.IO;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Data.Recyclable
{
    public class RecyclableTransformer
    {
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly ICompressionProvider _compressionProvider;
        private readonly ISerializationProvider _serializationProvider;

        public RecyclableTransformer(
            ISerializationProvider serializationProvider,
            IEncryptionProvider encryptionProvider,
            ICompressionProvider compressionProvider)
        {
            Guard.AgainstNull(serializationProvider, nameof(serializationProvider));
            Guard.AgainstNull(encryptionProvider, nameof(encryptionProvider));
            Guard.AgainstNull(compressionProvider, nameof(compressionProvider));

            _serializationProvider = serializationProvider;
            _encryptionProvider = encryptionProvider;
            _compressionProvider = compressionProvider;
        }

        /// <summary>
        /// Returns the bytes (which was the buffer) and actual length to use.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public (ArraySegment<byte>, long) Input<TIn>(TIn input)
        {
            var memoryStream = RecyclableManager.GetStream();

            _serializationProvider.Serialize(memoryStream, input);
            memoryStream = _compressionProvider.Compress(memoryStream, true) as RecyclableMemoryStream;
            memoryStream = _encryptionProvider.Encrypt(memoryStream) as RecyclableMemoryStream;

            var length = memoryStream.Length;
            if (memoryStream.TryGetBuffer(out var buffer))
            { return (buffer, length); }
            else
            { return (memoryStream.ToArray(), length); }
        }

        /// <summary>
        /// Returns the bytes (which was the buffer) and actual length to use.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<(ArraySegment<byte>, long)> InputAsync<TIn>(TIn input)
        {
            var memoryStream = RecyclableManager.GetStream();

            await _serializationProvider
                .SerializeAsync(memoryStream, input)
                .ConfigureAwait(false);

            memoryStream = await _compressionProvider
                .CompressAsync(memoryStream, true)
                .ConfigureAwait(false) as RecyclableMemoryStream;

            memoryStream = await _encryptionProvider
                .EncryptAsync(memoryStream)
                .ConfigureAwait(false) as RecyclableMemoryStream;

            var length = memoryStream.Length;
            if (memoryStream.TryGetBuffer(out var buffer))
            { return (buffer, length); }
            else
            { return (memoryStream.ToArray(), length); }
        }

        public Stream InputToStream<TIn>(TIn input)
        {
            var memoryStream = RecyclableManager.GetStream();
            _serializationProvider.Serialize(memoryStream, input);

            memoryStream = _compressionProvider
                .Compress(memoryStream, true) as RecyclableMemoryStream;

            memoryStream = _encryptionProvider
                .Encrypt(memoryStream) as RecyclableMemoryStream;

            return memoryStream;
        }

        public async Task<Stream> InputToStreamAsync<TIn>(TIn input)
        {
            var memoryStream = RecyclableManager.GetStream();
            await _serializationProvider
                .SerializeAsync(memoryStream, input)
                .ConfigureAwait(false);

            memoryStream = await _compressionProvider
                .CompressAsync(memoryStream, true)
                .ConfigureAwait(false) as RecyclableMemoryStream;

            memoryStream = await _encryptionProvider
                .EncryptAsync(memoryStream)
                .ConfigureAwait(false) as RecyclableMemoryStream;

            return memoryStream;
        }

        public async Task<TOut> OutputAsync<TOut>(ReadOnlyMemory<byte> data)
        {
            var memoryStream = _encryptionProvider.DecryptToStream(data);

            memoryStream = await _compressionProvider
                .DecompressAsync(memoryStream, true)
                .ConfigureAwait(false);

            using (memoryStream)
            {
                return await _serializationProvider
                    .DeserializeAsync<TOut>(memoryStream)
                    .ConfigureAwait(false);
            }
        }

        public TOut Output<TOut>(ReadOnlyMemory<byte> data)
        {
            var memoryStream = _encryptionProvider.DecryptToStream(data);
            memoryStream = _compressionProvider.Decompress(memoryStream, true);
            using (memoryStream)
            {
                return _serializationProvider.Deserialize<TOut>(memoryStream);
            }
        }

        public TOut Output<TOut>(Stream data)
        {
            var memoryStream = _encryptionProvider.Decrypt(data);
            memoryStream = _compressionProvider.Decompress(memoryStream, true);
            using (memoryStream)
            {
                return _serializationProvider.Deserialize<TOut>(memoryStream);
            }
        }
    }
}
