using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using static Dapper.SqlMapper;

namespace HouseofCat.Dapper
{
    public class SqlServerGridReader : IDapperGridReader, IDisposable
    {
        private readonly SqlConnection _connection;
        private readonly GridReader _reader;

        public bool IsReaderConsumed => _reader.IsConsumed;

        public SqlServerGridReader(SqlConnection connection, GridReader reader)
        {
            _connection = connection;
            _reader = reader;
        }

        public async Task<T> GetAsync<T>() where T : class, new()
        {
            if (!IsReaderConsumed)
            { return await _reader.ReadSingleAsync<T>().ConfigureAwait(false); }

            throw new InvalidOperationException();
        }

        public async Task<IEnumerable<T>> GetManyAsync<T>(bool buffered = true) where T : class, new()
        {
            if (!IsReaderConsumed)
            { return await _reader.ReadAsync<T>(buffered).ConfigureAwait(false); }

            throw new InvalidOperationException();
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
        void IDisposable.Dispose()
        {
            Dispose(true);
        }
    }
}
