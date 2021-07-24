using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Serialization
{
    public interface ISerializationProvider
    {
        TOut Deserialize<TOut>(string input);
        TOut Deserialize<TOut>(ReadOnlyMemory<byte> input);
        TOut Deserialize<TOut>(Stream inputStream);
        Task<TOut> DeserializeAsync<TOut>(Stream inputStream);
        byte[] Serialize<TIn>(TIn input);
        void Serialize<TIn>(Stream outputStream, TIn input);
        Task SerializeAsync<TIn>(Stream outputStream, TIn input);
        string SerializeToPrettyString<TIn>(TIn input);
        string SerializeToString<TIn>(TIn input);
    }
}