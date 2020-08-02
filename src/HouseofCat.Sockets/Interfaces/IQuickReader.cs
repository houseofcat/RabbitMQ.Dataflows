using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;

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
