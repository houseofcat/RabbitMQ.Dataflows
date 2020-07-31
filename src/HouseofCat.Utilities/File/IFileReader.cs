using System.Threading.Tasks;

namespace HouseofCat.Utilities.File
{
    public interface IFileReader
    {
        Task<TOut> ReadFileAsync<TOut>(string fileNamePath);
    }
}
