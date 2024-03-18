using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HouseofCat.Utilities.File;

public interface IFileReader
{
    Task<TOut> ReadFileAsync<TOut>(string fileNamePath);
}

public static class JsonFileReader
{
    /// <summary>
    /// Use this for the simplest of use cases. It uses the builtin System.Text.Json.
    /// </summary>
    public static async Task<TOut> ReadFileAsync<TOut>(string fileNamePath)
    {
        using var stream = new FileStream(fileNamePath, FileMode.Open);

        return await System
            .Text
            .Json
            .JsonSerializer
            .DeserializeAsync<TOut>(stream)
            .ConfigureAwait(false);
    }
}

public static class Utf8JsonFileReader
{
    /// <summary>
    /// Use this for fastest of use cases. Utf8Json supports more complex objects (think ICollections) than System.Text.Json.
    /// <para>Also supports Dictionary<string, object>.</para>
    /// </summary>
    public static async Task<TOut> ReadFileAsync<TOut>(string fileNamePath)
    {
        using var stream = new FileStream(fileNamePath, FileMode.Open);

        return await JsonSerializer
            .DeserializeAsync<TOut>(stream)
            .ConfigureAwait(false);
    }
}
