using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using CommunityToolkit.HighPerformance;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Data
{
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

        /// <summary>
        /// Transforms data back to the original object.
        /// <para>Data was serialized, compressed, then encrypted. So here it is decrypted, decompressed, and deserialized.</para>
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<TOut> DeserializeAsync<TOut>(ReadOnlyMemory<byte> data)
        {
            if (_encryptionProvider != null && _compressionProvider != null)
            {
                return DecryptDecompressDeserializeAsync<TOut>(data);
            }
            else if (_encryptionProvider != null)
            {
                return DecryptDeserializeAsync<TOut>(data);
            }
            else if (_compressionProvider != null)
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
        public TOut Deserialize<TOut>(ReadOnlyMemory<byte> data)
        {
            if (_encryptionProvider != null && _compressionProvider != null)
            {
                return DecryptDecompressDeserialize<TOut>(data);
            }
            else if (_encryptionProvider != null)
            {
                return DecryptDeserialize<TOut>(data);
            }
            else if (_compressionProvider != null)
            {
                return DecompressDeserialize<TOut>(data);
            }

            return _serializationProvider.Deserialize<TOut>(data);
        }

        public TOut DecryptDecompressDeserialize<TOut>(ReadOnlyMemory<byte> data)
        {
            var decryptedData = _encryptionProvider.Decrypt(data);
            var decompressedData = _compressionProvider.Decompress(decryptedData);

            return _serializationProvider.Deserialize<TOut>(decompressedData);
        }

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

        public TOut DecryptDeserialize<TOut>(ReadOnlyMemory<byte> data)
        {
            var decryptedData = _encryptionProvider.Decrypt(data);

            return _serializationProvider.Deserialize<TOut>(decryptedData);
        }

        public async Task<TOut> DecryptDeserializeAsync<TOut>(ReadOnlyMemory<byte> data)
        {
            var memoryStream = _encryptionProvider.DecryptToStream(data);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return await _serializationProvider
                .DeserializeAsync<TOut>(memoryStream)
                .ConfigureAwait(false);
        }

        public TOut DecompressDeserialize<TOut>(ReadOnlyMemory<byte> data)
        {
            var decompressedData = _compressionProvider.Decompress(data);

            return _serializationProvider.Deserialize<TOut>(decompressedData);
        }

        public async Task<TOut> DecompressDeserializeAsync<TOut>(ReadOnlyMemory<byte> data)
        {
            var memoryStream = _compressionProvider.DecompressToStream(data);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return await _serializationProvider
                .DeserializeAsync<TOut>(memoryStream)
                .ConfigureAwait(false);
        }

        public ArraySegment<byte> Serialize<TIn>(TIn input)
        {
            if (_encryptionProvider != null && _compressionProvider != null)
            {
                return SerializeCompressEncrypt(input);
            }
            else if (_encryptionProvider != null)
            {
                return SerializeEncrypt(input);
            }
            else if (_compressionProvider != null)
            {
                return SerializeEncrypt(input);
            }

            return _serializationProvider.Serialize(input);
        }

        public async Task<ArraySegment<byte>> SerializeAsync<TIn>(TIn input)
        {
            if (_encryptionProvider != null && _compressionProvider != null)
            {
                return await SerializeCompressEncryptAsync(input).ConfigureAwait(false);
            }
            else if (_encryptionProvider != null)
            {
                return await SerializeEncryptAsync(input).ConfigureAwait(false);
            }
            else if (_compressionProvider != null)
            {
                return await SerializeEncryptAsync(input).ConfigureAwait(false);
            }

            return _serializationProvider.Serialize(input);
        }

        public async Task<ArraySegment<byte>> SerializeCompressEncryptAsync<TIn>(TIn input)
        {
            var memoryStream = new MemoryStream();
            await _serializationProvider
                .SerializeAsync(memoryStream, input)
                .ConfigureAwait(false);

            memoryStream.Seek(0, SeekOrigin.Begin);

            memoryStream = await _compressionProvider
                .CompressAsync(memoryStream)
                .ConfigureAwait(false);

            memoryStream.Seek(0, SeekOrigin.Begin);

            memoryStream = await _encryptionProvider
                .EncryptAsync(memoryStream)
                .ConfigureAwait(false);

            memoryStream.Seek(0, SeekOrigin.Begin);

            if (memoryStream.TryGetBuffer(out var buffer))
            {
                return buffer;
            }
            else
            { return memoryStream.ToArray(); }
        }

        public ArraySegment<byte> SerializeCompressEncrypt<TIn>(TIn input)
        {
            return _encryptionProvider
                .Encrypt(_compressionProvider
                .Compress(_serializationProvider
                .Serialize(input)));
        }

        public async Task<ArraySegment<byte>> SerializeEncryptAsync<TIn>(TIn input)
        {
            var memoryStream = new MemoryStream();
            await _serializationProvider
                .SerializeAsync(memoryStream, input)
                .ConfigureAwait(false);

            memoryStream.Seek(0, SeekOrigin.Begin);

            memoryStream = await _encryptionProvider
                .EncryptAsync(memoryStream)
                .ConfigureAwait(false);

            memoryStream.Seek(0, SeekOrigin.Begin);

            if (memoryStream.TryGetBuffer(out var buffer))
            {
                return buffer;
            }
            else
            { return memoryStream.ToArray(); }
        }

        public ArraySegment<byte> SerializeEncrypt<TIn>(TIn input)
        {
            return _encryptionProvider
                .Encrypt(_serializationProvider
                .Serialize(input));
        }

        public async Task<ArraySegment<byte>> SerializeCompressAsync<TIn>(TIn input)
        {
            var memoryStream = new MemoryStream();
            await _serializationProvider.SerializeAsync(memoryStream, input);

            memoryStream.Seek(0, SeekOrigin.Begin);

            memoryStream = await _compressionProvider
                .CompressAsync(memoryStream)
                .ConfigureAwait(false);

            memoryStream.Seek(0, SeekOrigin.Begin);

            if (memoryStream.TryGetBuffer(out var buffer))
            {
                return buffer;
            }
            else
            { return memoryStream.ToArray(); }
        }

        public ArraySegment<byte> SerializeCompress<TIn>(TIn input)
        {
            return _compressionProvider
                .Compress(_serializationProvider
                .Serialize(input));
        }
    }
}
