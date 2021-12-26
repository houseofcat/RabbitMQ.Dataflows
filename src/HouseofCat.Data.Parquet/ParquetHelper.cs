using HouseofCat.Encryption;
using HouseofCat.Extensions;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.Types;
using Parquet;
using Parquet.Data;
using Parquet.Data.Rows;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using DataColumn = Parquet.Data.DataColumn;

namespace HouseofCat.Data.Parquet
{
    public static class ParquetHelper
    {

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
            var rawDataBaseType = reader.GetDataTypeName(index);
            var databaseType = NormalizeDatabaseTypeName(rawDataBaseType);

            var fieldClrType = reader.GetFieldType(index);
            var fieldName = reader.GetName(index);

            if (_dbToParquetTypeMap.ContainsKey(databaseType))
            {
                var parquetType = _dbToParquetTypeMap[databaseType];

                return new DataField(fieldName, parquetType, hasNulls: true);
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
                // Throws exception if it can't internally map.
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
                // Throws exception if it can't internally map.
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
            throw new InvalidOperationException(string.Format(ParquetDataTypeErrorTemplate, databaseType));
        }

        public static void LoadParquetMappingFromDictionary(Dictionary<string, DataType> map)
        {
            if (map.Count > 0)
            {
                _dbToParquetTypeMap = map;
            }
        }

        private static readonly string _parquetMapConfigurationKey = "Parquet:TypeMap";
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
            { "tinyint",    DataType.Byte },

            { "bit",        DataType.Boolean },
            { "boolean",    DataType.Boolean },

            { "smallint",   DataType.Int16 },

            { "int",        DataType.Int32 },
            { "integer",    DataType.Int32 },
            { "inet",       DataType.Int32 },
            { "oid",        DataType.Int32 },
            { "xid",        DataType.Int32 },
            { "cid",        DataType.Int32 },

            { "bigint",     DataType.Int64 },

            { "money",        DataType.Decimal },
            { "smallmoney",   DataType.Decimal },
            { "decimal",      DataType.Decimal },
            { "numeric",      DataType.Decimal },

            { "float",              DataType.Double },
            { "double precision",   DataType.Double },
            { "double",             DataType.Double },
            { "real",               DataType.Double },

            { "varbinary",          DataType.ByteArray },
            { "binary",             DataType.ByteArray },
            { "image",              DataType.ByteArray },
            { "rowversion",         DataType.ByteArray },

            { "sys.hierarchyid",                DataType.String },
            { "cidr",                           DataType.String },
            { "guid",                           DataType.String },
            { "uuid",                           DataType.String },
            { "char",                           DataType.String },
            { "date",                           DataType.String },
            { "datetime",                       DataType.String },
            { "datetime2",                      DataType.String },
            { "datetimeoffset",                 DataType.String },
            { "nchar",                          DataType.String },
            { "ntext",                          DataType.String },
            { "nvarchar",                       DataType.String },
            { "smalldatetime",                  DataType.String },
            { "text",                           DataType.String },
            { "time",                           DataType.String },
            { "uniqueidentifier",               DataType.String },
            { "varchar",                        DataType.String },
            { "xml",                            DataType.String },
            { "(internal) char",                DataType.String },
            { "character varying",              DataType.String },
            { "character",                      DataType.String },
            { "citext",                         DataType.String },
            { "json",                           DataType.String },
            { "jsonb",                          DataType.String },
            { "name",                           DataType.String },
            { "bytea",                          DataType.String },
            { "time without time zone",         DataType.String },
            { "interval",                       DataType.String },
            { "timetz",                         DataType.String },
            { "timestamp",                      DataType.String },
            { "timestamptz",                    DataType.String },
            { "timestamp without time zone",    DataType.String },
            { "timestamp with time zone",       DataType.String },
            { "time with time zone",            DataType.String },
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

        /// <summary>
        /// Writes the data inside the IDataReader, row by row, to a Parquet Table Row, then into multiple partitions and optionally multiple files.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="schema"></param>
        /// <param name="outputDirectory"></param>
        /// <param name="fileNameWithoutExtension"></param>
        /// <param name="encryptionProvider"></param>
        /// <param name="maxFileSize"></param>
        /// <param name="maxRowGroupSize"></param>
        /// <param name="compressionMethod"></param>
        /// <param name="compressionLevel"></param>
        /// <returns>An integer representing the files created count.</returns>
        public static int WriteDataReaderToParquetFiles(
             IDataReader reader,
             Schema schema,
             string outputDirectory,
             string fileNameWithoutExtension,
             IStreamEncryptionProvider encryptionProvider = null,
             long maxFileSize = long.MaxValue,
             long maxRowGroupSize = long.MaxValue,
             CompressionMethod compressionMethod = CompressionMethod.Snappy,
             int compressionLevel = -1)
        {
            if (reader.IsClosed) return 0;

            Guard.AgainstNull(reader, nameof(reader));
            Guard.AgainstNull(outputDirectory, nameof(outputDirectory));
            Guard.AgainstNull(fileNameWithoutExtension, nameof(fileNameWithoutExtension));
            Guard.AgainstNull(schema, nameof(schema));

            if (!Directory.Exists(outputDirectory))
            { Directory.CreateDirectory(outputDirectory); }

            if (Directory.Exists(outputDirectory))
            {
                if (reader.IsClosed) return 0;

                var fileCount = 0;

                for (; !reader.IsClosed; fileCount++)
                {
                    if (encryptionProvider != null)
                    {
                        WriteDataReaderToEncryptedParquetFile(
                            reader,
                            schema,
                            $"{outputDirectory}/{fileNameWithoutExtension}_c{fileCount:000#}.parquet",
                            encryptionProvider,
                            maxFileSize,
                            maxRowGroupSize,
                            compressionMethod,
                            compressionLevel);
                    }
                    else
                    {
                        WriteDataReaderToUnencryptedParquetFile(
                            reader,
                            schema,
                            $"{outputDirectory}/{fileNameWithoutExtension}_c{fileCount:000#}.parquet",
                            maxFileSize,
                            maxRowGroupSize,
                            compressionMethod,
                            compressionLevel);
                    }
                }

                return fileCount;
            }

            return 0;
        }

        private static readonly long _goodEnoughThresholdLength = 8192;

        public static void WriteDataReaderToUnencryptedParquetFile(
            IDataReader reader,
            Schema schema,
            string fileNamePath,
            long maxFileSize = long.MaxValue,
            long maxRowGroupSize = long.MaxValue,
            CompressionMethod compressionMethod = CompressionMethod.Snappy,
            int compressionLevel = -1)
        {
            using var fileStream = File.Open(fileNamePath, FileMode.OpenOrCreate);
            using var parquetWriter = new ParquetWriter(schema, fileStream)
            {
                CompressionMethod = compressionMethod,
                CompressionLevel = compressionLevel,
            };

            WriteFileLoop(
                reader,
                schema,
                parquetWriter,
                fileNamePath,
                maxFileSize,
                maxRowGroupSize);
        }

        public static void WriteDataReaderToEncryptedParquetFile(
            IDataReader reader,
            Schema schema,
            string fileNamePath,
            IStreamEncryptionProvider encryptionProvider,
            long maxFileSize = long.MaxValue,
            long maxRowGroupSize = long.MaxValue,
            CompressionMethod compressionMethod = CompressionMethod.Snappy,
            int compressionLevel = -1)
        {
            Guard.AgainstNull(encryptionProvider, nameof(encryptionProvider));

            using var fileStream = File.Open(fileNamePath, FileMode.OpenOrCreate);
            using var cryptoStream = encryptionProvider.GetEncryptStream(fileStream);
            using var parquetWriter = new ParquetWriter(schema, cryptoStream)
            {
                CompressionMethod = compressionMethod,
                CompressionLevel = compressionLevel,
            };

            WriteFileLoop(
                reader,
                schema,
                parquetWriter,
                fileNamePath,
                maxFileSize,
                maxRowGroupSize);

            cryptoStream.FlushFinalBlock();
        }

        public static void WriteDataReaderToComcryptedParquetFile(
            IDataReader reader,
            Schema schema,
            string fileNamePath,
            IStreamEncryptionProvider encryptionProvider,
            long maxFileSize = long.MaxValue,
            long maxRowGroupSize = long.MaxValue)
        {
            Guard.AgainstNull(encryptionProvider, nameof(encryptionProvider));

            using var fileStream = File.Open(fileNamePath, FileMode.OpenOrCreate);
            using var cryptoStream = encryptionProvider.GetEncryptStream(fileStream);
            using var gzipStream = new GZipStream(cryptoStream, CompressionLevel.Optimal);
            using var parquetWriter = new ParquetWriter(schema, gzipStream);

            WriteFileLoop(
                reader,
                schema,
                parquetWriter,
                fileNamePath,
                maxFileSize,
                maxRowGroupSize);

            cryptoStream.FlushFinalBlock();
        }

        private static void WriteFileLoop(
            IDataReader reader,
            Schema schema,
            ParquetWriter parquetWriter,
            string fileNamePath,
            long maxFileSize,
            long maxRowGroupSize)
        {
            var postWritePartitionSize = 0L;

            while (!reader.IsClosed)
            {
                var currentFileSize = new FileInfo(fileNamePath).Length;

                // This is the file size after the first RowGroup is
                // written to file (with compression included).
                if (currentFileSize > 0 && postWritePartitionSize == 0)
                { postWritePartitionSize = currentFileSize; }

                if (maxFileSize > currentFileSize)
                {
                    var remainingAllowedFileSize = maxFileSize - currentFileSize;

                    // File size is good enough check.
                    //
                    // If we proceed past this point, postCompressedRowGroupSize
                    // will be applied and write essentially up full RowGroup to file.
                    // If the RowGroup is sufficiently large enough, this could
                    // drastically alter the file size.
                    if (remainingAllowedFileSize < _goodEnoughThresholdLength)
                    { return; }

                    // Prevents excessive RowGroupPartitions at the end of the file
                    // by setting the maximum table size to the first row page group
                    // compressed size. This should allow us to create one
                    // final homogenous partition... in theory.
                    remainingAllowedFileSize = remainingAllowedFileSize > postWritePartitionSize
                        ? remainingAllowedFileSize
                        : postWritePartitionSize;

                    WritePartition(
                        reader,
                        schema,
                        parquetWriter,
                        maxRowGroupSize,
                        remainingAllowedFileSize);

                }
                // This file is at maximum size. Reader still open indicates more to write
                // but it's time for a new file.
                else
                {
                    return;
                }
            }
        }

        private static void WritePartition(
            IDataReader reader,
            Schema schema,
            ParquetWriter parquetWriter,
            long maxRowGroupSize,
            long remainingAllowedFileSize)
        {
            var table = BuildTable(reader, schema, remainingAllowedFileSize, maxRowGroupSize);

            using ParquetRowGroupWriter groupWriter = parquetWriter.CreateRowGroup();

            groupWriter.Write(table);
        }

        // BuildTable will build a maximum size table or run out of data.
        // If we are finished, DataReader is closed.
        public static Table BuildTable(
            IDataReader reader,
            Schema schema,
            long maxTableSizeInBytes = long.MaxValue,
            long maxRowGroupSize = long.MaxValue)
        {
            var table = new Table(schema);
            var currentTableSize = 0L;
            var currentRowCount = 0L;

            while (!reader.IsClosed && reader.Read())
            {
                var objects = new object[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.IsDBNull(i))
                    { objects[i] = null; }
                    else
                    {
                        var (value, length) = reader.GetValueForParquet(i);
                        objects[i] = value;
                        currentTableSize += length;
                    }
                }

                table.Add(new Row(objects));
                currentRowCount++;

                // Table is above the maximum allowed file size.
                // Only check size per ROW.
                // Side-effect: A really large single row will bypass Maximum File Size.
                if (currentTableSize >= maxTableSizeInBytes)
                {
                    return table;
                }

                // Table is at the maximum allowed rows.
                if (currentRowCount >= maxRowGroupSize)
                {
                    return table;
                }
            }

            // Everything has been read, indicate that all upstream work can stop.
            if (!reader.IsClosed)
            { reader.Close(); }

            return table;
        }

        private static readonly string InvalidValueErrorTemplate = "An unknown type ({0}) was returned from the database at runtime.";

        public static (object, long) GetValueForParquet(this IDataReader reader, int index)
        {
            var rawDataBaseType = reader.GetDataTypeName(index);
            var databaseType = NormalizeDatabaseTypeName(rawDataBaseType);

            if (_dbToParquetTypeMap.ContainsKey(databaseType))
            {
                switch (_dbToParquetTypeMap[databaseType])
                {
                    case DataType.Boolean:
                        {
                            var value = reader.GetBoolean(index);
                            return (value, value.GetByteCount());
                        }
                    case DataType.Byte:
                        {
                            var value = reader.GetByte(index);
                            return (value, value.GetByteCount());
                        }
                    case DataType.ByteArray:
                        {
                            var value = reader.GetValue(index);
                            return (value, value.GetByteCount());
                        }
                    case DataType.Int16:
                        {
                            var value = reader.GetInt16(index);
                            return (value, value.GetByteCount());
                        }
                    case DataType.Int32:
                        {
                            var value = reader.GetInt32(index);
                            return (value, value.GetByteCount());
                        }
                    case DataType.Int64:
                        {
                            var value = reader.GetInt64(index);
                            return (value, value.GetByteCount());
                        }
                    case DataType.Float:
                    case DataType.Double:
                        {
                            var value = reader.GetDouble(index);
                            return (value, value.GetByteCount());
                        }
                    case DataType.Decimal:
                        {
                            var value = reader.GetDecimal(index);
                            return (value, value.GetByteCount());
                        }
                    // Unknown/Date/Time/Strings
                    default:
                        {
                            var value = reader.GetComplexValueForParquet(index);
                            return (value, value.GetByteCount());
                        }
                }
            }
            throw new InvalidDataException(string.Format(InvalidValueErrorTemplate, rawDataBaseType));
        }

        private static readonly string _dateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ssK";
        private static bool _skipTypeBreaker;

        // Attempt to convert the value at the index to the datatypes we expect for Parquet.
        private static object GetComplexValueForParquet(this IDataReader reader, int index)
        {
            object value = null;

            // Allow the first one to try, then skip all other SqlGeography attempted mappings.
            if (_skipTypeBreaker && _skipTypes.Contains(reader.GetFieldType(index)))
            { return null; }

            try { value = reader.GetValue(index); }
            catch
            {
                // SqlGeography creates an InvalidCastException. The problem is that it slowed
                // the application down by a factor of 1000x (due to the # of exceptions).
                // Scenario A.) Microsoft.Data.SqlClient
                // Skip it when using modern SqlServer driver.
                if (_skipTypes.Contains(reader.GetFieldType(index)))
                {
                    _skipTypeBreaker = true;
                }
            }

            return value switch
            {
                string stringy => stringy,
                Guid guid => guid.ToString(),
                TimeSpan timespan => timespan.ToString(),
                DateTime dateTime => dateTime.ToString(_dateTimeFormat),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(),
                SqlHierarchyId sqlHierarchy => sqlHierarchy.ToString(),
                SqlGeography sqlGeography => sqlGeography.ToString(),
                _ => value,
            };
        }
    }
}
