using HouseofCat.Reflection;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HouseofCat.Gremlins
{
    public static class SqlExceptionHelpers
    {
        private static Random _random = new Random();
        private static int _sqlErrorCount = -1;

        public static async Task ThrowsRandomSqlExceptionAsync()
        {
            _sqlErrorCount = SqlErrors.ErrorList.Count;

            switch (_random.Next(0, 9))
            {
                case 0: throw new Exception();
                case 1: throw new AccessViolationException();
                case 2: throw new InvalidOperationException();
                case 3: throw new IOException();
                case 4: throw new SocketException();
                case 5: throw new WebException();
                case 6: throw new TimeoutException();
                case 7: throw new COMException();
                case 8: await GenerateSqlExceptionAsync(49918); break;
                case 9: await GenerateSqlExceptionAsync(_random.Next(0, _sqlErrorCount)); break;
                default: break;
            }
        }

        public static Task GenerateSqlExceptionAsync(int errorNumber)
        {
            SqlException e = null;

            var collection = Constructor.Construct<SqlErrorCollection>();

            if (collection != null)
            {
                var error = Constructor.Construct<SqlError>(errorNumber, 2, 3, "server name", "Gremlins generated SqlException.", "proc", 100);

                if (error != null)
                {
                    typeof(SqlErrorCollection)
                        .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(collection, new object[] { error });

                    e = typeof(SqlException)
                        .GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static, null, CallingConventions.ExplicitThis, new[] { typeof(SqlErrorCollection), typeof(string) }, new ParameterModifier[] { })
                        .Invoke(null, new object[] { collection, "7.0.0" }) as SqlException;

                    throw e;
                }
            }

            return Task.CompletedTask;
        }
    }
}
