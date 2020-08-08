using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Serialization
{
    public interface ISerializationProvider
    {
        TOut Deserialize<TOut>(ReadOnlyMemory<byte> input);
        Task<TOut> DeserializeAsync<TOut>(Stream utf8Json);
        TOut Deserialize<TOut>(string input);
        byte[] Serialize<TIn>(TIn input);
        Task SerializeAsync<TIn>(Stream utf8Json, TIn input);
        string SerializeToPrettyString<TIn>(TIn input);
        string SerializeToString<TIn>(TIn input);
    }
}