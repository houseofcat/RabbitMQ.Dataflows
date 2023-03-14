using HouseofCat.Logger;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static HouseofCat.RabbitMQ.LogMessages;

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

        public ulong ChannelId { get; }
        public bool Ackable { get; }
        public bool Closed { get; private set; }
        public bool FlowControlled { get; private set; }

        private readonly SemaphoreSlim _hostLock = new SemaphoreSlim(1, 1);
        private IConnectionHost _connHost;

        protected IModel Channel { get; private set; }
        protected bool DisposedValue { get; private set; }

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
                return Channel;
            }
            finally
            {
                ExitLock();
            }
        }

        private const string MakeChannelFailedError = "Making a channel failed. Error: {Message}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<bool> MakeChannelAsync()
        {
            if (!await _connHost.HealthyAsync().ConfigureAwait(false))
            {
                return false;
            }

            await EnterLockAsync().ConfigureAwait(false);

            try
            {
                if (Channel != null)
                {
                    RemoveEventHandlers();
                    Close();
                    Channel = null;
                }

                Channel = _connHost.Connection.CreateModel();

                if (Ackable)
                {
                    Channel.ConfirmSelect();
                }

                AddEventHandlers();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(MakeChannelFailedError, ex.Message);
                Channel = null;
                return false;
            }
            finally
            {
                ExitLock();
            }
        }

        protected virtual void AddEventHandlers()
        {
            Channel.FlowControl += FlowControl;
            Channel.ModelShutdown += ChannelClose;
        }

        protected virtual void RemoveEventHandlers()
        {
            Channel.FlowControl -= FlowControl;
            Channel.ModelShutdown -= ChannelClose;
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

        public async Task<bool> HealthyAsync() =>
            await _connHost.HealthyAsync().ConfigureAwait(false) && !FlowControlled && (Channel?.IsOpen ?? false);

        private const int CloseCode = 200;
        private const string CloseMessage = "HouseofCat.RabbitMQ manual close channel initiated.";

        public void Close()
        {
            if (!Closed && Channel.IsOpen)
            {
                try
                {
                    Channel.Close(CloseCode, CloseMessage);
                }
                catch { /* SWALLOW */ }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (DisposedValue)
            {
                return;
            }

            if (disposing)
            {
                _hostLock.Dispose();
            }

            Channel = null;
            _connHost = null;
            DisposedValue = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
