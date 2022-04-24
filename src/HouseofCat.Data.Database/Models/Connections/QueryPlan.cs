using System.Collections.Generic;
using System.Text.Json.Serialization;
using static HouseofCat.Data.Database.Enums;

namespace HouseofCat.Data.Database
{
    public class QueryPlan
    {
        public string Name { get; set; }
        public List<Parameter> Parameters { get; set; }
        public string Query { get; set; }

        public string DeferQuery { get; set; }
        public bool AllowDeferToFail { get; set; }

        public int QueryTimeoutInterval { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UnitOfTime QueryTimeoutUnit { get; set; }

        public List<QueryPlan> DependentQueries { get; set; }
    }
}
