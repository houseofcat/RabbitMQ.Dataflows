using System.Collections.Generic;
using System.Threading.Tasks;

namespace HouseofCat.Library.Dapper
{
    public interface IDapperGridReader
    {
        bool IsReaderConsumed { get; }

        Task<T> GetAsync<T>() where T : class, new();
        Task<IEnumerable<T>> GetManyAsync<T>(bool buffered = true) where T : class, new();
    }
}
