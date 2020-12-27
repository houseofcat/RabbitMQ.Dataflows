using HouseofCat.Utilities;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows
{
    public class NoLossBroadcastBlock<T> : IPropagatorBlock<T, T>
    {
        public Task Completion { get; }

        private readonly BroadcastBlock<T> _broadcastBlock;
        private readonly ITargetBlock<T> _targetBroadcastBlock; // reduces casting hits keeping a casted version cached

        public NoLossBroadcastBlock(Func<T, T> cloningFunction)
        {
            _broadcastBlock = new BroadcastBlock<T>(cloningFunction);
            _targetBroadcastBlock = _broadcastBlock;
            Completion = _broadcastBlock.Completion;
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
        {
            return _targetBroadcastBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        public void Complete()
        {
            _targetBroadcastBlock.Complete();
        }

        public void Fault(Exception exception)
        {
            _targetBroadcastBlock.Fault(exception);
        }

        public IDisposable LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions)
        {
            var bufferBlock = new BufferBlock<T>();
            var dispose1 = _broadcastBlock.LinkTo(bufferBlock, linkOptions);
            var dispose2 = bufferBlock.LinkTo(target, linkOptions);

            Completion.ContinueWith(_ => bufferBlock.Completion);
            return new MultiDispose(dispose1, dispose2);
        }

        public T ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target, out bool messageConsumed)
        {
            throw new NotImplementedException();
        }

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            throw new NotImplementedException();
        }

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            throw new NotImplementedException();
        }
    }
}
