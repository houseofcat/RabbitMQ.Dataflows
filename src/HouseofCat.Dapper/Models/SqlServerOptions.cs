using System.Collections.Generic;
using System.ComponentModel;

namespace HouseofCat.Dapper
{
    public class SqlServerOptions
    {
        public bool IntegratedSecurity { get; set; }

        [DefaultValue(true)]
        public bool Pooling { get; set; } = true;

        [DefaultValue(100)]
        public int MaxPoolSize { get; set; } = 100;
        public int MinPoolSize { get; set; }

        public bool MultipleActiveResultSets { get; set; }
        public bool MultiSubnetFailover { get; set; }
        public bool SwitchToTcp { get; set; }

        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
