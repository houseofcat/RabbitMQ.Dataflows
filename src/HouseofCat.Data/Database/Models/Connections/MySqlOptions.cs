using System.Collections.Generic;
using System.ComponentModel;

namespace HouseofCat.Data.Database;

public class MySqlOptions
{
    public bool IntegratedSecurity { get; set; }

    public string Protocol { get; set; }
    public string SslMode { get; set; }

    [DefaultValue(true)]
    public bool Pooling { get; set; } = true;

    [DefaultValue(100)]
    public uint MaxPoolSize { get; set; } = 100;
    public uint MinPoolSize { get; set; }

    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
