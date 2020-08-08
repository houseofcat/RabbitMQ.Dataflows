using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.Serialization
{
    public class NewtonsoftJsonProvider : ISerializationProvider
    {
        public byte[] Serialize<TIn>(TIn input)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input));
        }

        public Task SerializeAsync<TIn>(Stream utf8Json, TIn input)
        {
            throw new NotSupportedException();
        }

        public string SerializeToString<TIn>(TIn input)
        {
            return JsonConvert.SerializeObject(input);
        }

        public TOut Deserialize<TOut>(ReadOnlyMemory<byte> input)
        {
            return JsonConvert.DeserializeObject<TOut>(Encoding.UTF8.GetString(input.Span));
        }

        public TOut Deserialize<TOut>(string input)
        {
            return JsonConvert.DeserializeObject<TOut>(input);
        }

        public Task<TOut> DeserializeAsync<TOut>(Stream utf8Json)
        {
            throw new NotImplementedException();
        }

        public string SerializeToPrettyString<TIn>(TIn input)
        {
            throw new NotImplementedException();
        }
    }
}
