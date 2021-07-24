using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HouseofCat.Serialization
{
    public class JsonProvider : ISerializationProvider
    {
        private readonly JsonSerializerOptions _options;

        public JsonProvider(JsonSerializerOptions options = null)
        {
            _options = options;
        }
        public TOut Deserialize<TOut>(string input)
        {
            return JsonSerializer.Deserialize<TOut>(Encoding.UTF8.GetBytes(input), _options);
        }

        public TOut Deserialize<TOut>(ReadOnlyMemory<byte> input)
        {
            return JsonSerializer.Deserialize<TOut>(input.Span, _options);
        }

        public TOut Deserialize<TOut>(Stream inputStream)
        {
            var length = (int)inputStream.Length;
            var buffer = new byte[length];
            var bytesRead = inputStream.Read(buffer.AsSpan(0, length));
            if (bytesRead == 0) throw new InvalidDataException();

            var utf8Reader = new Utf8JsonReader(buffer.AsSpan());
            return JsonSerializer.Deserialize<TOut>(ref utf8Reader, _options);
        }

        public async Task<TOut> DeserializeAsync<TOut>(Stream inputStream)
        {
            return await JsonSerializer.DeserializeAsync<TOut>(inputStream, _options).ConfigureAwait(false);
        }

        public byte[] Serialize<TIn>(TIn input)
        {
            return JsonSerializer.SerializeToUtf8Bytes(input, _options);
        }

        public void Serialize<TIn>(Stream outputStream, TIn input)
        {
            JsonSerializer.Serialize(new Utf8JsonWriter(outputStream), input, _options);
            outputStream.Seek(0, SeekOrigin.Begin);
        }

        public Task SerializeAsync<TIn>(Stream outputStream, TIn input)
        {
            return JsonSerializer.SerializeAsync(outputStream, input, _options);
        }

        public string SerializeToPrettyString<TIn>(TIn input)
        {
            return JsonSerializer.Serialize(input, new JsonSerializerOptions { WriteIndented = true });
        }

        public string SerializeToString<TIn>(TIn input)
        {
            return JsonSerializer.Serialize(input, _options);
        }
    }
}
