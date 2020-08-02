using System.Threading.Channels;
using System.Threading.Tasks;

namespace HouseofCat.Sockets
{
    public interface IQuickWriter<TSend>
    {
        ChannelReader<TSend> MessageChannelReader { get; }
        ChannelWriter<TSend> MessageChannelWriter { get; }
        IQuickSocket QuickSocket { get; }
        bool Write { get; }

        Task QueueForWritingAsync(TSend data);
        Task StartWritingAsync();
        Task StopWriteAsync();
    }
}