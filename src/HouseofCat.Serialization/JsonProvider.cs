using HouseofCat.Utilities.Errors;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HouseofCat.Serialization;

public class JsonProvider : ISerializationProvider
{
    public string ContentType { get; private set; } = "application/json";

    private readonly JsonSerializerOptions _options;

    public JsonProvider(JsonSerializerOptions options = null)
    {
        _options = options;
    }

    public TOut Deserialize<TOut>(string input)
    {
        Guard.AgainstNullOrEmpty(input, nameof(input));

        return JsonSerializer.Deserialize<TOut>(Encoding.UTF8.GetBytes(input), _options);
    }

    public TOut Deserialize<TOut>(ReadOnlyMemory<byte> input)
    {
        Guard.AgainstEmpty(input, nameof(input));

        return JsonSerializer.Deserialize<TOut>(input.Span, _options);
    }

    public TOut Deserialize<TOut>(Stream inputStream)
    {
        Guard.AgainstNullOrEmpty(inputStream, nameof(inputStream));

        if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

        return JsonSerializer.Deserialize<TOut>(inputStream, _options);
    }

    public async Task<TOut> DeserializeAsync<TOut>(Stream inputStream)
    {
        Guard.AgainstNullOrEmpty(inputStream, nameof(inputStream));

        if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

        return await JsonSerializer
            .DeserializeAsync<TOut>(inputStream, _options)
            .ConfigureAwait(false);
    }

    public ReadOnlyMemory<byte> Serialize<TIn>(TIn input)
    {
        return JsonSerializer.SerializeToUtf8Bytes(input, _options);
    }

    public void Serialize<TIn>(Stream outputStream, TIn input)
    {
        JsonSerializer.Serialize(outputStream, input, _options);
        outputStream.Seek(0, SeekOrigin.Begin);
    }

    public async Task SerializeAsync<TIn>(Stream outputStream, TIn input)
    {
        await JsonSerializer
            .SerializeAsync(outputStream, input, _options)
            .ConfigureAwait(false);
    }

    private static readonly JsonSerializerOptions _prettyOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    public string SerializeToPrettyString<TIn>(TIn input)
    {
        return JsonSerializer.Serialize(input, _prettyOptions);
    }

    public string SerializeToString<TIn>(TIn input)
    {
        return JsonSerializer.Serialize(input, _options);
    }
}
