using MessagePack;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.Serialization;

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

    public void Serialize<TIn>(Stream outputStream, TIn input)
    {
        MessagePackSerializer.Serialize(outputStream, input, _options);
        outputStream.Seek(0, SeekOrigin.Begin);
    }

    public Task SerializeAsync<TIn>(Stream outputStream, TIn input)
    {
        return MessagePackSerializer.SerializeAsync(outputStream, input, _options);
    }

    public string SerializeToPrettyString<TIn>(TIn input)
    {
        throw new NotImplementedException();
    }

    public string SerializeToString<TIn>(TIn input)
    {
        return MessagePackSerializer.SerializeToJson(input, _options);
    }

    public TOut Deserialize<TOut>(ReadOnlyMemory<byte> input)
    {
        return MessagePackSerializer.Deserialize<TOut>(input, _options);
    }

    public TOut Deserialize<TOut>(string input)
    {
        return MessagePackSerializer.Deserialize<TOut>(Encoding.UTF8.GetBytes(input), _options);
    }

    public TOut Deserialize<TOut>(Stream inputStream)
    {
        return MessagePackSerializer.Deserialize<TOut>(inputStream, _options);
    }

    public async Task<TOut> DeserializeAsync<TOut>(Stream inputStream)
    {
        return await MessagePackSerializer.DeserializeAsync<TOut>(inputStream, _options).ConfigureAwait(false);
    }
}
