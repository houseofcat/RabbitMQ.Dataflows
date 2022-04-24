using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace HouseofCat.Data.Database.QueryBuilding
{
    public class Statement
    {
        public string Id { get; set; }

        [Required]
        public StatementType StatementType { get; set; }

        [Required]
        public Table Table { get; set; }

        [Required]
        public List<Field> Fields { get; set; }

        public List<Where> Wheres { get; set; }
        public List<Join> Joins { get; set; }

        public string[] SelectRawStatements { get; set; }
        public string[] GroupByFields { get; set; }
        public List<Order> OrderBys { get; set; }
        public List<Where> Havings { get; set; }

        public string QueryAlias { get; set; }

        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public bool? Distinct { get; set; }

        public Statement UnionStatement { get; set; } = null;
    }
}
