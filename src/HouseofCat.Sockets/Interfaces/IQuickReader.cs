using System.Threading.Channels;
using System.Threading.Tasks;

namespace HouseofCat.Sockets
{
    public interface IQuickReader<TOut>
    {
        ChannelReader<TOut> MessageChannelReader { get; }
        IQuickListeningSocket QuickListeningSocket { get; }
        bool Receive { get; }

        Task StartReceiveAsync();
        Task StopReceiveAsync();
    }
}
