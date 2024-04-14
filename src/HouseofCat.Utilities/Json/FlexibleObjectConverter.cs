using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HouseofCat.Utilities.Json;

public class FlexibleObjectJsonConverter : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartObject => JsonSerializer.Deserialize(ref reader, typeToConvert, options),
            JsonTokenType.StartArray => JsonSerializer.Deserialize(ref reader, typeToConvert, options),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => reader.TryGetInt64(out long l) ? l : reader.GetDouble(),
            JsonTokenType.String => reader.GetString(),
            _ => GetObjectFromJsonDocument(ref reader)
        };
    }

    private static object GetObjectFromJsonDocument(ref Utf8JsonReader reader)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.Clone();
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else if (value.GetType() == typeof(object))
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
        else
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
