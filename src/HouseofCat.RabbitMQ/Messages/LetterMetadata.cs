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

    public class LetterMetadata : IMetadata
    {
        /// <summary>
        /// An alternative Id field, intended to be user-supplied.
        /// </summary>
        public string Id { get; set; }
        public bool Encrypted { get; set; }
        public bool Compressed { get; set; }

        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();
    }
}
