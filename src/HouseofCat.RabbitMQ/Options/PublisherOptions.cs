using System.Threading.Channels;

namespace HouseofCat.RabbitMQ;

public class PublisherOptions
{
    public bool CreatePublishReceipts { get; set; }
    public int MessageQueueBufferSize { get; set; } = 10_000;
    public BoundedChannelFullMode BehaviorWhenFull { get; set; } = BoundedChannelFullMode.Wait;

    public bool Compress { get; set; }
    public bool Encrypt { get; set; }
    public bool WithHeaders { get; set; } = true;
    public int WaitForConfirmationTimeoutInMilliseconds { get; set; } = 500;
}
