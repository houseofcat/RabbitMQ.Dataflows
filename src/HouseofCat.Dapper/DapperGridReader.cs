using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using static Dapper.SqlMapper;

namespace HouseofCat.Dapper
{
    public interface IDapperGridReader
    {
        bool IsReaderConsumed { get; }

        Task<T> GetSingleAsync<T>() where T : class, new();
        Task<T> GetFirstAsync<T>() where T : class, new();
        Task<IEnumerable<T>> GetManyAsync<T>(bool buffered = true) where T : class, new();
    }

    public class DapperGridReader : IDapperGridReader, IDisposable
    {
        private readonly IDbConnection _connection;
        private readonly GridReader _reader;

        public bool IsReaderConsumed => _reader.IsConsumed;

        public DapperGridReader(IDbConnection connection, GridReader reader)
        {
            _connection = connection;
            _reader = reader;
        }

        public Task<T> GetSingleAsync<T>() where T : class, new()
        {
            if (IsReaderConsumed) throw new InvalidOperationException();

            return _reader.ReadSingleOrDefaultAsync<T>();
        }

        public Task<T> GetFirstAsync<T>() where T : class, new()
        {
            if (IsReaderConsumed) throw new InvalidOperationException();

            return _reader.ReadFirstOrDefaultAsync<T>();
        }

        public Task<IEnumerable<T>> GetManyAsync<T>(bool buffered = true) where T : class, new()
        {
            if (IsReaderConsumed) throw new InvalidOperationException();

            return _reader.ReadAsync<T>(buffered);
        }

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _reader.Dispose();
                    _connection.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
