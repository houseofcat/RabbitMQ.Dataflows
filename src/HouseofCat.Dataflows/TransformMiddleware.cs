using HouseofCat.Compression;
using HouseofCat.Encryption;
using HouseofCat.Serialization;
using HouseofCat.Utilities.Errors;
using Microsoft.Toolkit.HighPerformance;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Dataflows
{
    public class TransformMiddleware
    {
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly ICompressionProvider _compressionProvider;
        private readonly ISerializationProvider _serializationProvider;

        public TransformMiddleware(
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
        public async Task<TOut> DeserializeAsync<TOut>(ReadOnlyMemory<byte> data)
        {
            if (_encryptionProvider != null && _compressionProvider != null)
            {
                return await DecryptDecompressDeserializeAsync<TOut>(data);
            }
            else if (_encryptionProvider != null)
            {
                return await DecryptDeserializeAsync<TOut>(data);
            }
            else if (_compressionProvider != null)
            {
                return await DecompressDeserializeAsync<TOut>(data);
            }

            return await _serializationProvider.DeserializeAsync<TOut>(data.AsStream());
        }

        private async Task<TOut> DecryptDecompressDeserializeAsync<TOut>(ReadOnlyMemory<byte> data)
        {
            var decryptedStream = _encryptionProvider.DecryptToStream(data);
            decryptedStream.Position = 0;

            var decompressedStream = await _compressionProvider.DecompressStreamAsync(decryptedStream);
            decompressedStream.Position = 0;

            return await _serializationProvider.DeserializeAsync<TOut>(decompressedStream);
        }

        private async Task<TOut> DecryptDeserializeAsync<TOut>(ReadOnlyMemory<byte> data)
        {
            var decryptedStream = _encryptionProvider.DecryptToStream(data);
            decryptedStream.Position = 0;

            return await _serializationProvider.DeserializeAsync<TOut>(decryptedStream);
        }

        private async Task<TOut> DecompressDeserializeAsync<TOut>(ReadOnlyMemory<byte> data)
        {
            var decompressedStream = _compressionProvider.DecompressToStream(data);
            decompressedStream.Position = 0;

            return await _serializationProvider.DeserializeAsync<TOut>(decompressedStream);
        }

        public async Task<ArraySegment<byte>> SerializeAsync<TIn>(TIn input)
        {
            if (_encryptionProvider != null && _compressionProvider != null)
            {
                var memoryStream = new MemoryStream();
                await _serializationProvider.SerializeAsync(memoryStream, input);
                memoryStream.Position = 0;

                memoryStream = await _compressionProvider.CompressStreamAsync(memoryStream);
                memoryStream.Position = 0;

                memoryStream = await _encryptionProvider.EncryptAsync(memoryStream);
                memoryStream.Position = 0;

                if (memoryStream.TryGetBuffer(out var buffer))
                {
                    return buffer;
                }
                else
                { return memoryStream.ToArray(); }
            }
            else if (_encryptionProvider != null)
            {
                var memoryStream = new MemoryStream();
                await _serializationProvider.SerializeAsync(memoryStream, input);
                memoryStream.Position = 0;

                memoryStream = await _encryptionProvider.EncryptAsync(memoryStream);
                memoryStream.Position = 0;

                if (memoryStream.TryGetBuffer(out var buffer))
                {
                    return buffer;
                }
                else
                { return memoryStream.ToArray(); }
            }
            else if (_compressionProvider != null)
            {
                var memoryStream = new MemoryStream();
                await _serializationProvider.SerializeAsync(memoryStream, input);
                memoryStream.Position = 0;

                memoryStream = await _compressionProvider.CompressStreamAsync(memoryStream);
                memoryStream.Position = 0;

                if (memoryStream.TryGetBuffer(out var buffer))
                { return buffer;
                }
                else
                { return memoryStream.ToArray(); }
            }

            return _serializationProvider.Serialize(input);
        }
    }
}
