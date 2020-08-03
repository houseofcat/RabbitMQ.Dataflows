using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HouseofCat.Serialization
{
    public class SerializationProvider
    {
        public byte[] Serialize<TIn>(TIn input)
        {
            return JsonSerializer.SerializeToUtf8Bytes(input);
        }

        public string SerializeToString<TIn>(TIn input)
        {
            return JsonSerializer.Serialize(input);
        }

        public TOut Deserialize<TOut>(ReadOnlySpan<byte> input)
        {
            return JsonSerializer.Deserialize<TOut>(input);
        }

        public TOut Deserializee<TOut>(string input)
        {
            return JsonSerializer.Deserialize<TOut>(Encoding.UTF8.GetBytes(input));
        }
    }
}
