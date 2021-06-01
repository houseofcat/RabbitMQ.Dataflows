namespace HouseofCat.RabbitMQ
{
    public struct PublishReceipt : IPublishReceipt
    {
        public bool IsError { get; set; }
        public ulong LetterId { get; set; }
        public IMessage OriginalLetter { get; set; }

        public ulong GetMessageId() => LetterId;
        public IMessage GetOriginalMessage() => OriginalLetter;
    }
}
