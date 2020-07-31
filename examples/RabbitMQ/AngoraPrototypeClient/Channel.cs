using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using static RabbitMQ.Core.Prototype.AmqpConstants;

namespace RabbitMQ.Core.Prototype
{
    public class Channel
    {
        public ushort ChannelNumber { get; }

        public bool IsOpen { get; private set; }

        public Exchange Exchange { get; }

        public Queue Queue { get; }

        public Basic Basic { get; }

        private readonly ChannelMethods methods;

        private readonly SemaphoreSlim pendingReply;
        private bool replyIsExpected;
        private uint expectedReplyMethod;
        private object replyTaskCompletionSource;
        private Action<object, ReadOnlySequence<byte>, Exception> replyHandler;

        private readonly Action<object, ReadOnlySequence<byte>, Exception> handle_OpenOk;
        private readonly Action<object, ReadOnlySequence<byte>, Exception> handle_CloseOk;

        internal Channel(Socket socket, uint maxContentBodySize, ushort channelNumber)
        {
            ChannelNumber = channelNumber;

            methods = new ChannelMethods(socket, channelNumber);

            pendingReply = new SemaphoreSlim(1, 1);

            Exchange = new Exchange(socket, channelNumber, SetExpectedReplyMethod, ThrowIfClosed);
            Queue = new Queue(socket, channelNumber, SetExpectedReplyMethod, ThrowIfClosed);
            Basic = new Basic(socket, channelNumber, maxContentBodySize, SetExpectedReplyMethod, ThrowIfClosed);

            handle_OpenOk = Handle_OpenOk;
            handle_CloseOk = Handle_CloseOk;
        }

        private async Task SetExpectedReplyMethod(uint method, object taskCompletionSource, Action<object, ReadOnlySequence<byte>, Exception> replyHandler)
        {
            await pendingReply.WaitAsync().ConfigureAwait(false);

            expectedReplyMethod = method;
            replyTaskCompletionSource = taskCompletionSource;
            this.replyHandler = replyHandler;

            replyIsExpected = true;
        }

        private void ThrowIfClosed()
        {
            if (!IsOpen)
            {
                throw new Exception("Channel is closed");
            }
        }

        internal async Task HandleIncomingMethod(uint method, ReadOnlySequence<byte> arguments)
        {
            switch (method)
            {
                case Method.Channel.Close:
                    await Handle_Close(arguments).ConfigureAwait(false);
                    break;
                case Method.Basic.Deliver:
                    await Basic.Handle_Deliver(arguments).ConfigureAwait(false);
                    break;
                default:
                    HandleReplyMethod(method, arguments);
                    break;
            }
        }

        private void HandleReplyMethod(uint method, ReadOnlySequence<byte> arguments)
        {
            if (!replyIsExpected)
            {
                throw new Exception("reply received when not expecting one");
                //TODO send channel exception
            }

            Exception exception = null;

            if (method != expectedReplyMethod)
            {
                exception = new Exception($"Expected reply method {expectedReplyMethod}. Received {method}.");
            }

            replyHandler(replyTaskCompletionSource, arguments, exception);

            replyIsExpected = false;
            pendingReply.Release();
        }

        internal Task HandleIncomingContent(byte frameType, ReadOnlySequence<byte> payload)
        {
            if(frameType == FrameType.ContentHeader)
            {
                return Basic.Handle_ContentHeader(payload);
            }

            if (frameType == FrameType.ContentBody)
            {
                return Basic.Handle_ContentBody(payload);
            }

            return Task.CompletedTask;
        }

        internal async Task Open()
        {
            var openOk = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await SetExpectedReplyMethod(Method.Channel.OpenOk, openOk, handle_OpenOk).ConfigureAwait(false);

            await methods.Send_Open().ConfigureAwait(false);

            await openOk.Task.ConfigureAwait(false);
        }

        private void Handle_OpenOk(object tcs, ReadOnlySequence<byte> arguments, Exception exception)
        {
            var openOk = (TaskCompletionSource<bool>)tcs;

            if (exception != null)
            {
                openOk.SetException(exception);
            }
            else
            {
                IsOpen = true;
                openOk.SetResult(true);
            }
        }

        public async Task Close(ushort replyCode = ChannelReplyCode.Success, string replyText = "Goodbye", ushort failingClass = 0, ushort failingMethod = 0)
        {
            ThrowIfClosed();

            var closeOk = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await SetExpectedReplyMethod(Method.Channel.CloseOk, closeOk, Handle_CloseOk).ConfigureAwait(false);

            await methods.Send_Close(replyCode, replyText, failingClass, failingMethod).ConfigureAwait(false);

            await closeOk.Task.ConfigureAwait(false);
        }

        private void Handle_CloseOk(object tcs, ReadOnlySequence<byte> arguments, Exception exception)
        {
            var closeOk = (TaskCompletionSource<bool>)tcs;

            if (exception != null)
            {
                closeOk.SetException(exception);
            }
            else
            {
                IsOpen = false;
                closeOk.SetResult(true);
            }
        }

        private async Task Handle_Close(ReadOnlySequence<byte> arguments)
        {
            IsOpen = false;

            await methods.Send_CloseOk().ConfigureAwait(false);

            if (replyIsExpected)
            {
                SendReply();
            }

            void SendReply()
            {
                var reader = new CustomBufferReader(arguments);

                var replyCode = reader.ReadUInt16();
                var replyText = reader.ReadShortString();

                var method = reader.ReadUInt32();

                var classId = method >> 16;
                var methodId = method << 16 >> 16;

                var exception = new Exception($"Channel Closed: {replyCode} {replyText}. ClassId: {classId} MethodId: {methodId}");

                replyHandler(replyTaskCompletionSource, default, exception);

                replyIsExpected = false;
                pendingReply.Release();
            }
        }

        internal void Handle_Connection_Close(ushort replyCode, string replyText, uint method)
        {
            IsOpen = false;

            if (replyIsExpected)
            {
                var classId = method >> 16;
                var methodId = method << 16 >> 16;

                var exception = new Exception($"Connection Closed: {replyCode} {replyText}. ClassId: {classId} MethodId: {methodId}");

                replyHandler(replyTaskCompletionSource, default, exception);

                replyIsExpected = false;
                pendingReply.Release();
            }
        }
    }
}
