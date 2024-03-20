using System.Collections.Generic;
using System.ComponentModel;

namespace HouseofCat.Data.Database;

public class NpgsqlOptions
{
    public string SslMode { get; set; }

    [DefaultValue(true)]
    public bool Pooling { get; set; } = true;

    [DefaultValue(100)]
    public int MaxPoolSize { get; set; } = 100;
    public int MinPoolSize { get; set; }

    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
