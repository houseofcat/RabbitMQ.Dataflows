using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;

using static Angora.PrototypeClient.AmqpConstants;

namespace Angora.PrototypeClient
{
    public class Exchange
    {
        private readonly ExchangeMethods methods;
        private readonly Func<uint, object, Action<object, ReadOnlySequence<byte>, Exception>, Task> SetExpectedReplyMethod;
        private readonly Action ThrowIfClosed;

        private readonly Action<object, ReadOnlySequence<byte>, Exception> handle_DeclareOk;
        private readonly Action<object, ReadOnlySequence<byte>, Exception> handle_DeleteOk;
        private readonly Action<object, ReadOnlySequence<byte>, Exception> handle_BindOk;
        private readonly Action<object, ReadOnlySequence<byte>, Exception> handle_UnbindOk;

        internal Exchange(Socket socket, ushort channelNumber, Func<uint, object, Action<object, ReadOnlySequence<byte>, Exception>, Task> setExpectedReplyMethod, Action throwIfClosed)
        {
            methods = new ExchangeMethods(socket, channelNumber);
            SetExpectedReplyMethod = setExpectedReplyMethod;
            ThrowIfClosed = throwIfClosed;

            handle_DeclareOk = Handle_DeclareOk;
            handle_DeleteOk = Handle_DeleteOk;
            handle_BindOk = Handle_BindOk;
            handle_UnbindOk = Handle_UnbindOk;
        }

        public async Task Declare(string exchangeName, string type, bool passive, bool durable, bool autoDelete, bool @internal, Dictionary<string, object> arguments)
        {
            ThrowIfClosed();

            var declareOk = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await SetExpectedReplyMethod(Method.Exchange.DeclareOk, declareOk, handle_DeclareOk).ConfigureAwait(false);

            await methods.Send_Declare(exchangeName, type, passive, durable, autoDelete, @internal, arguments).ConfigureAwait(false);

            await declareOk.Task.ConfigureAwait(false);
        }

        private void Handle_DeclareOk(object tcs, ReadOnlySequence<byte> arguments, Exception exception)
        {
            var declareOk = (TaskCompletionSource<bool>)tcs;

            if (exception != null)
            {
                declareOk.SetException(exception);
            }
            else
            {
                declareOk.SetResult(true);
            }
        }

        public async Task Delete(string exchange, bool onlyIfUnused)
        {
            ThrowIfClosed();

            var deleteOk = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await SetExpectedReplyMethod(Method.Exchange.DeleteOk, deleteOk, handle_DeleteOk).ConfigureAwait(false);

            await methods.Send_Delete(exchange, onlyIfUnused).ConfigureAwait(false);

            await deleteOk.Task.ConfigureAwait(false);
        }

        private void Handle_DeleteOk(object tcs, ReadOnlySequence<byte> arguments, Exception exception)
        {
            var deleteOk = (TaskCompletionSource<bool>)tcs;

            if (exception != null)
            {
                deleteOk.SetException(exception);
            }
            else
            {
                deleteOk.SetResult(true);
            }
        }

        public async Task Bind(string source, string destination, string routingKey, Dictionary<string, object> arguments)
        {
            ThrowIfClosed();

            var bindOk = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await SetExpectedReplyMethod(Method.Exchange.BindOk, bindOk, handle_BindOk).ConfigureAwait(false);

            await methods.Send_Bind(source, destination, routingKey, arguments).ConfigureAwait(false);

            await bindOk.Task.ConfigureAwait(false);
        }

        private void Handle_BindOk(object tcs, ReadOnlySequence<byte> arguments, Exception exception)
        {
            var bindOk = (TaskCompletionSource<bool>)tcs;

            if (exception != null)
            {
                bindOk.SetException(exception);
            }
            else
            {
                bindOk.SetResult(true);
            }
        }

        public async Task Unbind(string source, string destination, string routingKey, Dictionary<string, object> arguments)
        {
            ThrowIfClosed();

            var unbindOk = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await SetExpectedReplyMethod(Method.Exchange.UnbindOk, unbindOk, handle_UnbindOk).ConfigureAwait(false);

            await methods.Send_Unbind(source, destination, routingKey, arguments).ConfigureAwait(false);

            await unbindOk.Task.ConfigureAwait(false);
        }

        private void Handle_UnbindOk(object tcs, ReadOnlySequence<byte> arguments, Exception exception)
        {
            var unbindOk = (TaskCompletionSource<bool>)tcs;

            if (exception != null)
            {
                unbindOk.SetException(exception);
            }
            else
            {
                unbindOk.SetResult(true);
            }
        }
    }
}
