namespace HouseofCat.RabbitMQ
{
    public interface IPublishReceipt
    {
        bool IsError { get; set; }

        string GetMessageId();
        IMessage GetOriginalMessage();
    }

    public struct PublishReceipt : IPublishReceipt
    {
        public bool IsError { get; set; }
        public string LetterId { get; set; }
        public IMessage OriginalLetter { get; set; }

        public string GetMessageId() => LetterId;
        public IMessage GetOriginalMessage() => OriginalLetter;
    }
}
