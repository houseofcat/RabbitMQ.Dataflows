using System.Data;
using System.Text.Json.Serialization;

namespace HouseofCat.Dapper
{
    public class Parameter
    {
        public string Name { get; set; }
        public object Value { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DbType? DbType { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ParameterDirection? Direction { get; set; }
        public int? Size { get; set; }
        public byte? Precision { get; set; }
        public byte? Scale { get; set; }
    }
}
