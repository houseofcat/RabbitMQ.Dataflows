namespace HouseofCat.RabbitMQ
{
    public interface IPublishReceipt
    {
        bool IsError { get; set; }

        ulong GetMessageId();
        IMessage GetOriginalMessage();
    }

    public struct PublishReceipt : IPublishReceipt
    {
        public bool IsError { get; set; }
        public ulong LetterId { get; set; }
        public IMessage OriginalLetter { get; set; }

        public ulong GetMessageId() => LetterId;
        public IMessage GetOriginalMessage() => OriginalLetter;
    }
}
