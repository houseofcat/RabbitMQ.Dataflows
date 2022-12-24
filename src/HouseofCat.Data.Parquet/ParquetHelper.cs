using HouseofCat.Utilities.Errors;
using HouseofCat.Utilities.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.Types;
using MySql.Data.Types;
using Newtonsoft.Json;
using Parquet;
using Parquet.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DataColumn = Parquet.Data.DataColumn;

namespace HouseofCat.Data.Parquet
{
    public static class ParquetHelper
    {
        public static string DateTimeFormat { get; set; } = Time.Formats.CatsAltFormat;

        private static bool _skipTypeBreaker;

        private static readonly HashSet<Type> _skipTypes = new HashSet<Type>
        {
            typeof(SqlGeography), typeof(SqlGeometry)
        };

        private static readonly string SchemaReaderClosedErrorTemplate = "Can't read a closed {0}.";

        public static Schema BuildSchemaFromDataReader(IDataReader reader)
        {
            if (reader.IsClosed)
            {
                throw new InvalidOperationException(
                    string.Format(SchemaReaderClosedErrorTemplate,
                    nameof(IDataReader)));
            }

            var dataFields = new List<DataField>();

            for (var i = 0; i < reader.FieldCount; i++)
            {
                dataFields.Add(GetParquetType(reader, i));
            }

            return new Schema(dataFields);
        }

        private static readonly string ParquetDataTypeErrorTemplate = "Unable to safely determine a backup Parquet DataType for DatabaseType: {0}.";

        public static DataField GetParquetType(IDataReader reader, int index)
        {
            var rawDatabaseType = reader.GetDataTypeName(index);
            var databaseType = NormalizeDatabaseTypeName(rawDatabaseType);

            var fieldClrType = reader.GetFieldType(index);
            var fieldName = reader.GetName(index);

            if (_dbToParquetTypeMap.ContainsKey(databaseType))
            {
                var parquetType = _dbToParquetTypeMap[databaseType];

                // Single Byte/Signed/Unsigned Byte are casted to Int32 and annotate as UINT_8 which isn't supported
                //by Apache Spark.
                // Here we force the Schema to convert to Int32 and annotate as INT32 which is supported.
                DataField dataField;
                if (parquetType != DataType.Byte)
                { dataField = new DataField(fieldName, parquetType, hasNulls: true); }
                else
                { dataField = new DataField<int?>(fieldName); }

                return dataField;
            }

            if (_skipTypes.Contains(fieldClrType)) // Add blank string columns for data that refuses to map in testing.
            {
                try
                {
                    // SqlGeography/SqlGeometry just doesn't work! Let's add this blank column to the dictionary.
                    if (!_dbToParquetTypeMap.ContainsKey(databaseType)) // double check
                    {
                        _dbToParquetTypeMap[databaseType] = DataType.String;
                    }

                    return new DataField(fieldName, DataType.String, hasNulls: true);
                }
                catch
                { }
            }

            // If mapping is not known, will string work?
            try
            {
                // Throws exception if it can't map.
                _ = new DataColumn(
                    new DataField(fieldName, typeof(string)),
                    new[] { reader.GetValue(index) });

                // String works! Let's also add it to the dictionary to skip this step next time.
                if (!_dbToParquetTypeMap.ContainsKey(databaseType)) // double check
                {
                    _dbToParquetTypeMap[databaseType] = DataType.String;
                }

                return new DataField(fieldName, DataType.String, hasNulls: true);
            }
            catch
            { }

            // String did not work, so let's try once more with raw bytes.
            try
            {
                // Throws exception if it can't map.
                _ = new DataColumn(
                    new DataField(reader.GetName(index), typeof(byte[])),
                    new[] { reader.GetValue(index) });

                // byte[] works! Let's also add it to the dictionary to skip this step next time.
                if (!_dbToParquetTypeMap.ContainsKey(databaseType)) // double check
                {
                    _dbToParquetTypeMap[databaseType] = DataType.ByteArray;
                }

                return new DataField(fieldName, DataType.ByteArray);
            }
            catch
            { }

            // All backup conversions failed.
            throw new InvalidOperationException(string.Format(ParquetDataTypeErrorTemplate, databaseType, fieldClrType.FullName));
        }

        public static void LoadParquetMappingFromDictionary(Dictionary<string, DataType> map)
        {
            if (map.Count > 0)
            {
                _dbToParquetTypeMap = map;
            }
        }

        private static readonly string _parquetMapConfigurationKey = "HoC:Parquet:TypeMap";
        public static void LoadParquetMappingFromConfiguration(IConfiguration configuration)
        {
            var map = configuration
                .GetSection(_parquetMapConfigurationKey)
                .Get<Dictionary<string, DataType>>();

            if (map.Count > 0)
            {
                _dbToParquetTypeMap = map;
            }
        }

        private static Dictionary<string, DataType> _dbToParquetTypeMap = new Dictionary<string, DataType>
        {
            { "tinyint",            DataType.Byte },
            { "tinyint unsigned",   DataType.Byte },

            { "bit",        DataType.Boolean },
            { "bit(1)",     DataType.Boolean },
            { "bool",       DataType.Boolean },
            { "boolean",    DataType.Boolean },
            { "logical",    DataType.Boolean },

            { "short",      DataType.Int16 },
            { "smallint",   DataType.Int16 },
            { "year",       DataType.Int16 },

            { "autoinc",              DataType.Int32 },
            { "int",                  DataType.Int32 },
            { "integer",              DataType.Int32 },
            { "smallint unsigned",    DataType.Int32 },
            { "medium int",           DataType.Int32 },
            { "inet",                 DataType.Int32 },

            { "oid",        DataType.Int64 },
            { "xid",        DataType.Int64 },
            { "cid",        DataType.Int64 },

            { "bigint",     DataType.Int64 },

            { "serial",             DataType.Decimal },
            { "double unsigned",    DataType.Decimal },
            { "float unsigned",     DataType.Decimal },
            { "bigint unsigned",    DataType.Decimal },
            { "fixed",              DataType.Decimal },
            { "money",              DataType.Decimal },
            { "smallmoney",         DataType.Decimal },
            { "decimal",            DataType.Decimal },
            { "numeric",            DataType.Decimal },

            { "float",              DataType.Double },
            { "real",               DataType.Double },
            { "curdouble",          DataType.Double },
            { "double precision",   DataType.Double },
            { "double",             DataType.Double },

            { "raw",                DataType.ByteArray },
            { "blob",               DataType.ByteArray },
            { "tinyblob",           DataType.ByteArray },
            { "mediumblob",         DataType.ByteArray },
            { "longblob",           DataType.ByteArray },
            { "char byte",          DataType.ByteArray },
            { "varbinary",          DataType.ByteArray },
            { "binary",             DataType.ByteArray },
            { "image",              DataType.ByteArray },
            { "rowversion",         DataType.ByteArray },

            { "sys.hierarchyid",                DataType.String },
            { "cidr",                           DataType.String },

            { "guid",                           DataType.String },
            { "uuid",                           DataType.String },
            { "uniqueidentifier",               DataType.String },

            { "set",                            DataType.String },
            { "char",                           DataType.String },
            { "nchar",                          DataType.String },
            { "text",                           DataType.String },
            { "ntext",                          DataType.String },
            { "tinytext",                       DataType.String },
            { "longtext",                       DataType.String },
            { "varchar",                        DataType.String },
            { "nvarchar",                       DataType.String },
            { "xml",                            DataType.String },
            { "(internal) char",                DataType.String },
            { "character varying",              DataType.String },
            { "character",                      DataType.String },
            { "citext",                         DataType.String },
            { "cistring",                       DataType.String },
            { "cichar",                         DataType.String },
            { "nmemo",                          DataType.String },
            { "memo",                           DataType.String },
            { "name",                           DataType.String },

            { "date",                           DataType.String },
            { "datetime",                       DataType.String },
            { "datetime2",                      DataType.String },
            { "datetimeoffset",                 DataType.String },
            { "smalldatetime",                  DataType.String },
            { "time",                           DataType.String },
            { "timetz",                         DataType.String },
            { "timestamp",                      DataType.String },
            { "timestamptz",                    DataType.String },
            { "timestamp without time zone",    DataType.String },
            { "timestamp with time zone",       DataType.String },
            { "time with time zone",            DataType.String },
            { "time without time zone",         DataType.String },
            { "modtime",                        DataType.String },

            { "enum",                           DataType.String },
            { "json",                           DataType.String },
            { "jsonb",                          DataType.String },
            { "bytea",                          DataType.String },
            { "interval",                       DataType.String },

            // Weird stuff to JSON
            { "hstore",          DataType.String }, // PostgreSql: type is a Dictionary<string, string>, should map as JSON (string).
            { "array types",     DataType.String }, // PostgreSql: type is an Array(T), should map as JSON (string).
            { "composite types", DataType.String }, // PostgreSql: type is a T, should map as JSON (string).
            { "record",          DataType.String }  // PostgreSql: record is an object[], we map to JSON (string).
        };

        private static readonly string _sysHierarchyId = "sys.hierarchyid";
        private static readonly string _geography = "geography";
        private static readonly string _numeric = "numeric";
        private static readonly string _numericLong = $"{_numeric}(";
        private static readonly string _character = "character";
        private static readonly string _characterLong = $"{_character}(";
        private static readonly string _characterVarying = "character varying";
        private static readonly string _characterVaryingLong = $"{_characterVarying}(";

        // This is a helper method to help bridge the gap on any unexpected DatabaseType name formats whose
        // underlying types work but name needs to remove non-matching characters.
        private static string NormalizeDatabaseTypeName(string input)
        {
            var lowerInput = input.ToLower(CultureInfo.InvariantCulture);
            return lowerInput switch
            {
                _ when input.Contains(_sysHierarchyId) => _sysHierarchyId,
                _ when input.Contains(_geography) => _geography,
                _ when input.StartsWith(_numericLong) => _numeric,
                _ when input.StartsWith(_characterLong) => _character,
                _ when input.StartsWith(_characterVaryingLong) => _characterVarying,
                _ => lowerInput,
            };
        }

        private static readonly string InvalidValueErrorTemplate = "An unknown type ({0}) was returned from the database at runtime.";
        private static readonly string _dateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffffffK";
        private static readonly string _mySqlPointFormat = "POINT({0} {1})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object GetValueForParquetV2(
            this IDataReader reader,
            int index,
            string normalizedDatabaseType,
            Type fieldType)
        {
            if (_dbToParquetTypeMap.ContainsKey(normalizedDatabaseType))
            {
                switch (_dbToParquetTypeMap[normalizedDatabaseType])
                {
                    case DataType.Boolean:
                        return reader.GetBoolean(index);

                    case DataType.Byte:
                        return reader.GetByte(index);

                    case DataType.ByteArray:

                        return (byte[])reader.GetValue(index);

                    case DataType.Int16:
                        return reader.GetInt16(index);

                    case DataType.Int32:
                        return reader.GetInt32(index);

                    case DataType.Int64:
                        return reader.GetInt64(index);

                    case DataType.Float:
                    case DataType.Double:
                        return reader.GetDouble(index);

                    case DataType.Decimal:
                        return reader.GetDecimal(index);

                    case DataType.String:

                        if (fieldType == typeof(string))
                        { return reader.GetString(index); }
                        else if (fieldType == typeof(DateTime))
                        { return reader.GetDateTime(index).ToString(_dateTimeFormat); }
                        else if (fieldType == typeof(Guid))
                        { return reader.GetGuid(index).ToString(); }
                        else
                        { return reader.GetComplexValueForParquet(index); }

                    // Catch all.
                    default:
                        return reader.GetComplexValueForParquet(index);
                }
            }

            throw new InvalidDataException(string.Format(InvalidValueErrorTemplate, normalizedDatabaseType));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetComplexValueForParquet(this IDataReader reader, int index)
        {
            if (reader.IsClosed) return null;

            object value = null;
            if (_skipTypeBreaker && _skipTypes.Contains(reader.GetFieldType(index)))
            { return null; }

            try { value = reader.GetValue(index); }
            catch
            {
                if (_skipTypes.Contains(reader.GetFieldType(index)))
                {
                    _skipTypeBreaker = true;
                }
            }

            switch (value)
            {
                case null: return null;
                case string x: return x;
                case DateTime dt: return dt.ToString(_dateTimeFormat);
                case Guid g: return g.ToString();
                case MySqlDateTime mydt: return mydt.GetDateTime().ToString(_dateTimeFormat);
                case byte[] binary: return FailsafeString(binary);
                case object[] os: return FailsafeJsonString(os);
                case DateTimeOffset dto: return dto.ToString();
                case MySqlDecimal mydec: return mydec.Value.ToString();
                case TimeSpan ts: return ts.ToString("g");
                case Array a: return FailsafeJsonString(a);
                case SqlHierarchyId docId: return docId.ToString();
                case SqlGeography sqlgeo: return sqlgeo.ToString();
                case MySqlGeometry mygeo: return string.Format(_mySqlPointFormat, mygeo.XCoordinate, mygeo.YCoordinate);
                case object o: return FailsafeJsonString(o);
            }
        }

        private static string FailsafeString(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        private static string FailsafeJsonString(object value)
        {
            try
            { return Utf8Json.JsonSerializer.ToJsonString(value); } // fast & lower memory - handles 99% use cases
            catch
            {
                try
                { return JsonConvert.SerializeObject(value); } // slow & high memory - more complex collection types
                catch
                { return value.ToString(); } // should indicate in our files we have potential for remediation.
            }
        }

        public static async Task<(int, int)> WriteDataReaderToParquetFiles(
             IDataReader reader,
             Schema schema,
             string[] normalizedDatabaseTypes,
             string outputDirectory,
             string fileNameWithoutExtension,
             CancellationTokenSource cts,
             long maxFileSize = int.MaxValue - 1,
             long maxRowGroupSize = int.MaxValue - 1,
             CancellationToken token = default)
        {
            if (reader.IsClosed) return (0, 0);

            Guard.AgainstNull(reader, nameof(reader));
            Guard.AgainstNull(outputDirectory, nameof(outputDirectory));
            Guard.AgainstNull(fileNameWithoutExtension, nameof(fileNameWithoutExtension));
            Guard.AgainstNull(schema, nameof(schema));
            Guard.AgainstNullOrEmpty(normalizedDatabaseTypes, nameof(normalizedDatabaseTypes));

            if (!Directory.Exists(outputDirectory))
            { Directory.CreateDirectory(outputDirectory); }

            if (Directory.Exists(outputDirectory))
            {
                if (reader.IsClosed) return (0, 0);

                var fileCount = 0;
                var rowCount = 0;

                while (!reader.IsClosed)
                {
                    if (cts.IsCancellationRequested)
                    { break; }

                    var fileName = $"{fileNameWithoutExtension}-{fileCount:c000#}.snappy.parquet";
                    var fileNamePath = $"{outputDirectory}\\{fileName}";

                    rowCount += await WriteDataReaderToParquetFileAsync(
                        reader,
                        schema,
                        normalizedDatabaseTypes,
                        fileNamePath,
                        maxFileSize,
                        maxRowGroupSize,
                        token);

                    fileCount++;
                }

                return (fileCount, rowCount);
            }

            return (0, 0);
        }

        public static long GoodEnoughThresholdLength = 1024 * 24;

        public static async Task<int> WriteDataReaderToParquetFileAsync(
            IDataReader reader,
            Schema schema,
            string[] normalizedDatabaseTypes,
            string fileNamePath,
            long maxFileSize = int.MaxValue - 1,
            long maxRowGroupSize = int.MaxValue - 1,
            CancellationToken token = default)
        {
            var postWriteFileSize = 0L;
            var rowCount = 0;

            var dataFields = schema.GetDataFields();

            var readerFieldTypes = new Type[dataFields.Length];
            for (var i = 0; i < dataFields.Length; i++)
            { readerFieldTypes[i] = reader.GetFieldType(i); }

            var columns = new ArrayList[schema.Fields.Count];
            for (var i = 0; i < schema.Fields.Count; i++)
            { columns[i] = new ArrayList(); }

            using var fileStream = File.Open(fileNamePath, FileMode.Create);
            using var parquetWriter = await ParquetWriter.CreateAsync(schema, fileStream, formatOptions: new ParquetOptions { TreatBigIntegersAsDates = false }, cancellationToken: token);

            while (!reader.IsClosed)
            {
                if (token.IsCancellationRequested)
                { break; }

                var currentFileSize = new FileInfo(fileNamePath).Length;

                // This is the file size after the first RowGroup is
                // written to file (with compression included).
                if (currentFileSize > 0 && postWriteFileSize == 0)
                { postWriteFileSize = currentFileSize; }

                if (maxFileSize <= currentFileSize)
                { break; }

                var remainingAllowedFileSize = maxFileSize - currentFileSize;

                // File size is good enough check.
                //
                // If we proceed past this point, postCompressedRowGroupSize
                // will be applied and write essentially up full RowGroup to file.
                // If the RowGroup is sufficiently large enough, this could
                // drastically alter the file size.
                if (remainingAllowedFileSize < GoodEnoughThresholdLength)
                { break; }

                FillColumns(reader, columns, readerFieldTypes, normalizedDatabaseTypes, maxRowGroupSize);
                rowCount += columns[0].Count;

                using var groupWriter = parquetWriter.CreateRowGroup();
                for (var i = 0; i < columns.Length; i++)
                {
                    await groupWriter.WriteColumnAsync(new DataColumn(dataFields[i], columns[i].ToArray(dataFields[i].ClrNullableIfHasNullsType)), token);
                    columns[i].Clear(); // Remove elements after conversion to DataColumn. We re-use the columns though so we don't trim.
                }
            }

            return rowCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillColumns(
            IDataReader reader,
            ArrayList[] columns,
            Type[] readerFieldTypes,
            string[] normalizedDatabaseTypes,
            long maxRowGroupSize = int.MaxValue - 1)
        {
            var currentRowCount = 0L;
            while (!reader.IsClosed && reader.Read())
            {
                // Create a Row (an object across each list)
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    reader.TransferValue(i, columns[i], readerFieldTypes[i], normalizedDatabaseTypes[i]);
                }

                currentRowCount++;

                // Table is at the maximum allowed rows.
                if (currentRowCount >= maxRowGroupSize) return;
            }

            // Everything has been read, indicate that all upstream work can stop.
            if (!reader.IsClosed)
            { reader.Close(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TransferValue(this IDataReader reader, int index, ArrayList column, Type readerFieldType, string normalizedDatabaseType)
        {
            if (reader.IsDBNull(index))
            {
                column.Add(null);
            }
            else
            {
                column.Add(
                    reader
                        .GetValueForParquetV2(
                            index,
                            normalizedDatabaseType,
                            readerFieldType));
            }
        }
    }
}
