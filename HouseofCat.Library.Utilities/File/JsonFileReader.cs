using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HouseofCat.Library.Utilities.File
{
    public static class JsonFileReader
    {
        public static async Task<TOut> ReadJsonFileAsync<TOut>(string fileNamePath)
        {
            using var stream = new FileStream(fileNamePath, FileMode.Open);

            return await JsonSerializer.DeserializeAsync<TOut>(stream).ConfigureAwait(false);
        }
    }
}
