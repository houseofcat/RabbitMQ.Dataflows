using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.Serialization
{
    public class NewtonsoftJsonProvider : ISerializationProvider
    {
        private JsonSerializer _jsonSerializer = new JsonSerializer();

        public TOut Deserialize<TOut>(ReadOnlyMemory<byte> input)
        {
            return JsonConvert.DeserializeObject<TOut>(Encoding.UTF8.GetString(input.Span));
        }

        public TOut Deserialize<TOut>(string input)
        {
            return JsonConvert.DeserializeObject<TOut>(input);
        }

        public TOut Deserialize<TOut>(Stream utf8Json)
        {
            throw new NotImplementedException();
        }

        public Task<TOut> DeserializeAsync<TOut>(Stream utf8Json)
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize<TIn>(TIn input)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input));
        }

        public void Serialize<TIn>(Stream output, TIn input)
        {
            output.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input)));
            output.Seek(0, SeekOrigin.Begin);
        }

        public async Task SerializeAsync<TIn>(Stream utf8JsonStream, TIn input)
        {
            using StreamWriter writer = new StreamWriter(utf8JsonStream);
            using JsonTextWriter jsonWriter = new JsonTextWriter(writer);

            _jsonSerializer.Serialize(jsonWriter, input);
            await jsonWriter.FlushAsync();
        }

        public string SerializeToPrettyString<TIn>(TIn input)
        {
            throw new NotImplementedException();
        }

        public string SerializeToString<TIn>(TIn input)
        {
            return JsonConvert.SerializeObject(input);
        }
    }
}
