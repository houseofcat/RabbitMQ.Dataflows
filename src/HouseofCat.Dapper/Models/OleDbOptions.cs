using System.Collections.Generic;

namespace HouseofCat.Dapper
{
    public class OleDbOptions
    {
        public bool PersistSecurityInfo { get; set; }
        public int OleDbServices { get; set; }

        public string FileName { get; set; }
        public string Provider { get; set; }

        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
