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

        public byte[] Serialize<TIn>(TIn input)
        {
            return JsonSerializer.Serialize(input, _resolver);
        }

        public Task SerializeAsync<TIn>(Stream utf8Json, TIn input)
        {
            return JsonSerializer.SerializeAsync(utf8Json, input, _resolver);
        }

        public string SerializeToString<TIn>(TIn input)
        {
            return JsonSerializer.ToJsonString(input, _resolver);
        }

        public string SerializeToPrettyString<TIn>(TIn input)
        {
            return JsonSerializer.PrettyPrint(JsonSerializer.ToJsonString(input, _resolver));
        }

        public TOut Deserialize<TOut>(ReadOnlyMemory<byte> input)
        {
            return JsonSerializer.Deserialize<TOut>(input.ToArray(), _resolver);
        }

        public TOut Deserialize<TOut>(string input)
        {
            return JsonSerializer.Deserialize<TOut>(Encoding.UTF8.GetBytes(input));
        }

        public async Task<TOut> DeserializeAsync<TOut>(Stream utf8Json)
        {
            return await JsonSerializer.DeserializeAsync<TOut>(utf8Json);
        }
    }
}
