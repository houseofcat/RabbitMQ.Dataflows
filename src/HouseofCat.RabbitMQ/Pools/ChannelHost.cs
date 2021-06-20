using HouseofCat.Logger;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Pools
{
    public interface IChannelHost
    {
        bool Ackable { get; }
        ulong ChannelId { get; }
        bool Closed { get; }
        bool FlowControlled { get; }

        IModel GetChannel();
        Task<bool> MakeChannelAsync();
        void Close();
        Task<bool> HealthyAsync();
    }

    public class ChannelHost : IChannelHost, IDisposable
    {
        private readonly ILogger<ChannelHost> _logger;
        private IModel _channel { get; set; }
        private IConnectionHost _connHost { get; set; }

        public ulong ChannelId { get; set; }

        public bool Ackable { get; }

        public bool Closed { get; private set; }
        public bool FlowControlled { get; private set; }

        private readonly SemaphoreSlim _hostLock = new SemaphoreSlim(1, 1);
        private bool _disposedValue;

        public ChannelHost(ulong channelId, IConnectionHost connHost, bool ackable)
        {
            _logger = LogHelper.GetLogger<ChannelHost>();

            ChannelId = channelId;
            _connHost = connHost;
            Ackable = ackable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IModel GetChannel()
        {
            _hostLock.Wait();

            try
            { return _channel; }
            finally
            { _hostLock.Release(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<bool> MakeChannelAsync()
        {
            await _hostLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_channel != null)
                {
                    _channel.FlowControl -= FlowControl;
                    _channel.ModelShutdown -= ChannelClose;
                    Close();
                    _channel = null;
                }

                _channel = _connHost.Connection.CreateModel();

                if (Ackable)
                {
                    _channel.ConfirmSelect();
                }

                _channel.FlowControl += FlowControl;
                _channel.ModelShutdown += ChannelClose;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Making a channel failed. Error: {0}", ex.Message);
                _channel = null;
                return false;
            }
            finally
            { _hostLock.Release(); }
        }

        protected virtual void ChannelClose(object sender, ShutdownEventArgs e)
        {
            _hostLock.Wait();
            _logger.LogDebug(e.ReplyText);
            Closed = true;
            _hostLock.Release();
        }

        protected virtual void FlowControl(object sender, FlowControlEventArgs e)
        {
            _hostLock.Wait();

            if (e.Active)
            { _logger.LogWarning(LogMessages.ChannelHosts.FlowControlled); }
            else
            { _logger.LogInformation(LogMessages.ChannelHosts.FlowControlFinished); }

            FlowControlled = e.Active;

            _hostLock.Release();
        }

        public async Task<bool> HealthyAsync()
        {
            var connectionHealthy = await _connHost.HealthyAsync().ConfigureAwait(false);

            return connectionHealthy && !FlowControlled && (_channel?.IsOpen ?? false);
        }

        private const int CloseCode = 200;
        private const string CloseMessage = "HouseofCat.RabbitMQ manual close channel initiated.";

        public void Close()
        {
            if (!Closed || !_channel.IsOpen)
            {
                try
                { _channel.Close(CloseCode, CloseMessage); }
                catch { }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _hostLock.Dispose();
                }

                _channel = null;
                _connHost = null;
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
