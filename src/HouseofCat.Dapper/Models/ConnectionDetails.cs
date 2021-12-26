using System.Text.Json.Serialization;
using static HouseofCat.Dapper.Enums;

namespace HouseofCat.Dapper
{
    public class ConnectionDetails
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Database Database { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DatabaseName { get; set; }

        public Metadata Metadata { get; set; } = new Metadata();
    }
}
