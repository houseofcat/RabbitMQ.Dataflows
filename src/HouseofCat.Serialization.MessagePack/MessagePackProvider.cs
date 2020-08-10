using MessagePack;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.Serialization
{
    public class MessagePackProvider : ISerializationProvider
    {
        private readonly MessagePackSerializerOptions _options;

        public MessagePackProvider(MessagePackSerializerOptions options = null)
        {
            _options = options;
        }

        public byte[] Serialize<TIn>(TIn input)
        {
            return MessagePackSerializer.Serialize(input, _options);
        }

        public Task SerializeAsync<TIn>(Stream utf8Json, TIn input)
        {
            return MessagePackSerializer.SerializeAsync(utf8Json, input, _options);
        }

        public string SerializeToString<TIn>(TIn input)
        {
            return MessagePackSerializer.SerializeToJson(input, _options);
        }

        public string SerializeToPrettyString<TIn>(TIn input)
        {
            throw new NotImplementedException();
        }

        public TOut Deserialize<TOut>(ReadOnlyMemory<byte> input)
        {
            return MessagePackSerializer.Deserialize<TOut>(input, _options);
        }

        public TOut Deserialize<TOut>(string input)
        {
            return MessagePackSerializer.Deserialize<TOut>(Encoding.UTF8.GetBytes(input), _options);
        }

        public async Task<TOut> DeserializeAsync<TOut>(Stream utf8Json)
        {
            return await MessagePackSerializer.DeserializeAsync<TOut>(utf8Json, _options);
        }
    }
}
