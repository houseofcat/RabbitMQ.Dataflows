using System;
using System.Collections.Generic;

namespace RabbitMQ.Core.Prototype
{
    public class MessageProperties
    {
        public string ContentType { get; set; }

        public string ContentEncoding { get; set; }

        public Dictionary<string, object> Headers { get; set; }

        public byte DeliveryMode { get; set; }

        public byte Priority { get; set; }

        public string CorrelationId { get; set; }

        public string ReplyTo { get; set; }

        public string Expiration { get; set; }

        public string MessageId { get; set; }

        public DateTime Timestamp { get; set; }

        public string Type { get; set; }

        public string UserId { get; set; }

        public string AppId { get; set; }
    }
}
