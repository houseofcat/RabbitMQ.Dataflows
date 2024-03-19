using System.Collections.Generic;
using System.ComponentModel;

namespace HouseofCat.Data.Database;

public class SQLiteOptions
{
    [DefaultValue(true)]
    public bool Pooling { get; set; } = true;

    [DefaultValue(100)]
    public int MaxPoolSize { get; set; } = 100;
    public int MinPoolSize { get; set; }

    public bool ReadOnly { get; set; }
    public int Version { get; set; } = 3;
    public bool BinaryGuid { get; set; }

    public bool UTF16Encoding { get; set; }

    public int CacheSizeInBytes { get; set; }
    public int PageSizeInBytes { get; set; }
    public int MaxPageCount { get; set; }

    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
