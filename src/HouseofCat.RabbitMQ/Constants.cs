namespace HouseofCat.RabbitMQ;

public static class Constants
{
    public const string HeaderValueForContentTypeBinary = "application/octet-stream";
    public const string HeaderValueForContentTypePlainText = "text/plain";
    public const string HeaderValueForContentTypeJson = "application/json";
    public const string HeaderValueForContentTypeMessagePack = "application/msgpack";

    public static string HeaderPrefix { get; set; } = "X-";
    public static string HeaderForContentType { get; set; } = "ContentType";

    public static string HeaderForObjectType { get; set; } = "X-RD-OBJECTTYPE";
    public static string HeaderValueForMessageObjectType { get; set; } = "IMESSAGE";

    public static string HeaderValueForUnknownObjectType { get; set; } = "UNK";
    public static string HeaderForEncrypted { get; set; } = "X-RD-ENCRYPTED";
    public static string HeaderForEncryption { get; set; } = "X-RD-ENCRYPTION";
    public static string HeaderForEncryptDate { get; set; } = "X-RD-ENCRYPTDATE";
    public static string HeaderForCompressed { get; set; } = "X-RD-COMPRESSED";
    public static string HeaderForCompression { get; set; } = "X-RD-COMPRESSION";

    public static string HeaderForTraceParent { get; set; } = "traceparent";

    public static string MessagingSystemKey { get; set; } = "messaging.system";
    public static string MessagingSystemValue { get; set; } = "rabbitmq";

    public static string MessagingOperationKey { get; set; } = "messaging.operation";
    public static string MessagingOperationPublishValue { get; set; } = "publish";
    public static string MessagingOperationConsumeValue { get; set; } = "receive";
    public static string MessagingOperationProcessValue { get; set; } = "process";

    public static string MessagingDestinationNameKey { get; set; } = "messaging.destination.name";
    public static string MessagingConsumerNameKey { get; set; } = "messaging.rabbitmq.consumer.name";
    public static string MessagingMessageMessageIdKey { get; set; } = "messaging.message.id";

    public static string MessagingMessageBodySizeKey { get; set; } = "messaging.rabbitmq.message.body.size";
    public static string MessagingMessageEnvelopeSizeKey { get; set; } = "messaging.rabbitmq.message.envelope.size";

    public static string MessagingMessageRoutingKeyKey { get; set; } = "messaging.rabbitmq.message.routing_key";
    public static string MessagingMessageDeliveryTagIdKey { get; set; } = "messaging.rabbitmq.message.delivery_tag";
    public static string MessagingMessageContentTypeKey { get; set; } = "messaging.rabbitmq.message.content_type";

    public static string MessagingBatchProcessValue { get; set; } = "messaging.batch.message_count";

    public static string MessagingMessagePayloadIdKey { get; set; } = "messaging.rabbitmq.message.payload_id";
    public static string MessagingMessageEncryptedKey { get; set; } = "messaging.rabbitmq.message.encrypted";
    public static string MessagingMessageEncryptedDateKey { get; set; } = "messaging.rabbitmq.message.encrypted_date";
    public static string MessagingMessageEncryptionKey { get; set; } = "messaging.rabbitmq.message.encryption";
    public static string MessagingMessageCompressedKey { get; set; } = "messaging.rabbitmq.message.compressed";
    public static string MessagingMessageCompressionKey { get; set; } = "messaging.rabbitmq.message.compression";
}
