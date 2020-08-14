using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HouseofCat.Serialization
{
    public class JsonProvider : ISerializationProvider
    {
        private JsonSerializerOptions _options;

        public JsonProvider(JsonSerializerOptions options = null)
        {
            _options = options;
        }

        public byte[] Serialize<TIn>(TIn input)
        {
            return JsonSerializer.SerializeToUtf8Bytes(input, _options);
        }

        public Task SerializeAsync<TIn>(Stream utf8Json, TIn input)
        {
            return JsonSerializer.SerializeAsync(utf8Json, input, _options);
        }

        public string SerializeToString<TIn>(TIn input)
        {
            return JsonSerializer.Serialize(input, _options);
        }

        public string SerializeToPrettyString<TIn>(TIn input)
        {
            return JsonSerializer.Serialize(input, new JsonSerializerOptions { WriteIndented = true });
        }

        public TOut Deserialize<TOut>(ReadOnlyMemory<byte> input)
        {
            return JsonSerializer.Deserialize<TOut>(input.Span, _options);
        }

        public TOut Deserialize<TOut>(string input)
        {
            return JsonSerializer.Deserialize<TOut>(Encoding.UTF8.GetBytes(input), _options);
        }

        public async Task<TOut> DeserializeAsync<TOut>(Stream utf8Json)
        {
            return await JsonSerializer.DeserializeAsync<TOut>(utf8Json, _options).ConfigureAwait(false);
        }
    }
}
