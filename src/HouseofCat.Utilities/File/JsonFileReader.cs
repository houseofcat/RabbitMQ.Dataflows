using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HouseofCat.Utilities.File;

public static class JsonFileReader
{
    /// <summary>
    /// Use this for the simplest of use cases. It uses the builtin System.Text.Json.
    /// </summary>
    public static async Task<TOut> ReadFileAsync<TOut>(string fileNamePath)
    {
        using var stream = new FileStream(fileNamePath, FileMode.Open);

        return await JsonSerializer
            .DeserializeAsync<TOut>(stream)
            .ConfigureAwait(false);
    }
}
