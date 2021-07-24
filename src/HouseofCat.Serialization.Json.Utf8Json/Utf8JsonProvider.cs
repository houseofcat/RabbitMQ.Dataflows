using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;
using Utf8Json.Resolvers;

namespace HouseofCat.Serialization
{
    public class Utf8JsonProvider : ISerializationProvider
    {
        private readonly IJsonFormatterResolver _resolver;

        public Utf8JsonProvider(IJsonFormatterResolver resolver = null)
        {
            _resolver = resolver ?? StandardResolver.Default;
        }

        public TOut Deserialize<TOut>(ReadOnlyMemory<byte> input)
        {
            return JsonSerializer.Deserialize<TOut>(input.ToArray(), _resolver);
        }

        public TOut Deserialize<TOut>(string input)
        {
            return JsonSerializer.Deserialize<TOut>(Encoding.UTF8.GetBytes(input));
        }

        public TOut Deserialize<TOut>(Stream inputStream)
        {
            return JsonSerializer.Deserialize<TOut>(inputStream);
        }

        public async Task<TOut> DeserializeAsync<TOut>(Stream inputStream)
        {
            return await JsonSerializer.DeserializeAsync<TOut>(inputStream).ConfigureAwait(false);
        }

        public byte[] Serialize<TIn>(TIn input)
        {
            return JsonSerializer.Serialize(input, _resolver);
        }

        public void Serialize<TIn>(Stream outputStream, TIn input)
        {
            JsonSerializer.Serialize(outputStream, input, _resolver);
            outputStream.Seek(0, SeekOrigin.Begin);
        }

        public async Task SerializeAsync<TIn>(Stream outputStream, TIn input)
        {
            await JsonSerializer.SerializeAsync(outputStream, input, _resolver);
            outputStream.Seek(0, SeekOrigin.Begin);
        }

        public string SerializeToPrettyString<TIn>(TIn input)
        {
            return JsonSerializer.PrettyPrint(JsonSerializer.ToJsonString(input, _resolver));
        }

        public string SerializeToString<TIn>(TIn input)
        {
            return JsonSerializer.ToJsonString(input, _resolver);
        }
    }
}
