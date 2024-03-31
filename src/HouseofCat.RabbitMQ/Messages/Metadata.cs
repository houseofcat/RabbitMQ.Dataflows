using System.Collections.Generic;

namespace HouseofCat.RabbitMQ;

public interface IMetadata
{
    string PayloadId { get; }
    bool Encrypted { get; set; }
    bool Compressed { get; set; }

    Dictionary<string, object> Fields { get; set; }
}

public sealed class Metadata : IMetadata
{
    /// <summary>
    /// PayloadId is a user supplied identifier that allows identifying the Body without needing to deserialize.
    /// </summary>
    public string PayloadId { get; set; }
    public bool Encrypted { get; set; }
    public bool Compressed { get; set; }

    public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();
}
