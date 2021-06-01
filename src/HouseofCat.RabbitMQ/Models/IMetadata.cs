using System.Collections.Generic;

namespace HouseofCat.RabbitMQ
{
    public interface IMetadata
    {
        string Id { get; }
        bool Encrypted { get; set; }
        bool Compressed { get; set; }

        Dictionary<string, object> CustomFields { get; set; }
    }
}