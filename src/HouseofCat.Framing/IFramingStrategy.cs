using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HouseofCat.Framing
{
    public interface IFramingStrategy
    {
        Task CreateFrameAndSendAsync(byte[] bytes, NetworkStream netStream);
        bool TryReadSequence(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> sequence);
    }
}