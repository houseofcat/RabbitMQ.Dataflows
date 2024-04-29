using System.Text.Json.Serialization;

namespace HouseofCat.Compression;

public static class Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CompressionType
    {
        Gzip,
        Deflate,
        Brotli,
        LZ4Stream,
        LZ4Pickle
    }
}
