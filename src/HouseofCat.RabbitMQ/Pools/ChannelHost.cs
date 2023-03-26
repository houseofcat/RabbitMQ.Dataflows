using HouseofCat.Logger;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HouseofCat.Utilities;
using static HouseofCat.RabbitMQ.LogMessages;

namespace HouseofCat.RabbitMQ.Pools
{
    public interface IChannelHost
    {
        bool Ackable { get; }
        ulong ChannelId { get; }
        bool Closed { get; }
        bool FlowControlled { get; }
        string Id { get; }

        IModel GetChannel();
        Task<bool> MakeChannelAsync(Func<ValueTask<bool>> startConsumingAsync = null);
        void Close();
        Task<bool> HealthyAsync();
    }

    public class ChannelHost : IChannelHost, IDisposable
    {
        public bool Ackable { get; }
        public ulong ChannelId { get; }
        public bool Closed { get; private set; }
        public bool FlowControlled { get; private set; }
        public string Id { get; } = Guid.NewGuid().ConvertToBase64Url();

        private readonly ILogger<ChannelHost> _logger;
        private readonly SemaphoreSlim _hostLock = new SemaphoreSlim(1, 1);

        private IConnectionHost _connHost;
        private IModel _channel;
        private bool _disposedValue;

        public ChannelHost(ulong channelId, IConnectionHost connHost, bool ackable)
        {
            _logger = LogHelper.GetLogger<ChannelHost>();

            ChannelId = channelId;
            _connHost = connHost;
            Ackable = ackable;

            MakeChannelAsync().GetAwaiter().GetResult();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IModel GetChannel()
        {
            EnterLock();

            try
            {
                return _channel;
            }
            finally
            {
                ExitLock();
            }
        }

        private const string MakeChannelFailedError = "Making a channel failed. Error: {0}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<bool> MakeChannelAsync(Func<ValueTask<bool>> startConsumingAsync = null)
        {
            await EnterLockAsync().ConfigureAwait(false);

            try
            {
                if (_channel != null)
                {
                    RemoveEventHandlers(_channel, _connHost.Connection);
                    Close();
                    _channel = null;
                }

                _channel = _connHost.Connection.CreateModel();

                if (Ackable)
                {
                    _channel.ConfirmSelect();
                }

                AddEventHandlers(_channel, _connHost.Connection);
            }
            catch (Exception ex)
            {
                _logger.LogError(MakeChannelFailedError, ex.Message);
                _channel = null;
                return false;
            }
            finally
            {
                ExitLock();
            }

            return startConsumingAsync is null || await startConsumingAsync().ConfigureAwait(false);
        }

        protected virtual void AddEventHandlers(IModel channel, IConnection _)
        {
            channel.FlowControl += FlowControl;
            channel.ModelShutdown += ChannelClose;
        }

        protected virtual void RemoveEventHandlers(IModel channel, IConnection _)
        {
            channel.FlowControl -= FlowControl;
            channel.ModelShutdown -= ChannelClose;
        }

        protected void EnterLock() => _hostLock.Wait();
        protected Task EnterLockAsync() => _hostLock.WaitAsync();
        protected void ExitLock() => _hostLock.Release();

        protected virtual void ChannelClose(object sender, ShutdownEventArgs e)
        {
            EnterLock();
            _logger.LogDebug(e.ReplyText);
            Closed = true;
            ExitLock();
        }

        protected virtual void FlowControl(object sender, FlowControlEventArgs e)
        {
            EnterLock();
            if (e.Active)
            {
                _logger.LogWarning(ChannelHosts.FlowControlled, ChannelId);
            }
            else
            {
                _logger.LogInformation(ChannelHosts.FlowControlFinished, ChannelId);
            }
            FlowControlled = e.Active;
            ExitLock();
        }

        protected Task<bool> ConnectionHostHealthyAsync() => _connHost.HealthyAsync();

        public virtual async Task<bool> HealthyAsync() =>
            await ConnectionHostHealthyAsync().ConfigureAwait(false) && !FlowControlled && (_channel?.IsOpen ?? false);

        private const int CloseCode = 200;
        private const string CloseMessage = "HouseofCat.RabbitMQ manual close channel initiated.";

        public void Close()
        {
            try
            {
                if (!Closed || (_channel?.IsOpen ?? false))
                {
                    _channel.Close(CloseCode, CloseMessage);
                }
            }
            catch { /* SWALLOW */ }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                _hostLock.Dispose();
            }

            _channel = null;
            _connHost = null;
            _disposedValue = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
