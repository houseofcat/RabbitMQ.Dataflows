using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public interface IMetadata
{
    string PayloadId { get; }
    bool Encrypted { get; set; }
    bool Compressed { get; set; }

    Dictionary<string, object> CustomFields { get; set; }
}

public sealed class Metadata : IMetadata
{
    /// <summary>
    /// PayloadId is a user supplied Id for tracking purposes. Allows identifying the inner payload without needing to deserialize first.
    /// </summary>
    public string PayloadId { get; set; }
    public bool Encrypted { get; set; }
    public bool Compressed { get; set; }

    public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();
}
