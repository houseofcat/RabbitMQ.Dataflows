using HouseofCat.Logger;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Pools;

public interface IChannelHost
{
    bool Ackable { get; }
    ulong ChannelId { get; }
    bool Closed { get; }
    bool FlowControlled { get; }
    bool UsedByConsumer { get; }

    IModel GetChannel();

    string StartConsuming(IBasicConsumer internalConsumer, ConsumerOptions options);
    void StopConsuming();

    Task WaitUntilChannelIsReadyAsync(int sleepInterval, CancellationToken token = default);
    Task<bool> BuildRabbitMQChannelAsync();

    void Close();
    Task<bool> ChannelHealthyAsync();
    Task<bool> ConnectionHealthyAsync();
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
    public bool UsedByConsumer { get; private set; }

    private readonly SemaphoreSlim _hostLock = new SemaphoreSlim(1, 1);
    private bool _disposedValue;

    public ChannelHost(ulong channelId, IConnectionHost connHost, bool ackable)
    {
        _logger = LogHelper.GetLogger<ChannelHost>();

        ChannelId = channelId;
        _connHost = connHost;
        Ackable = ackable;

        BuildRabbitMQChannelAsync().GetAwaiter().GetResult();
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

    public virtual async Task WaitUntilChannelIsReadyAsync(int sleepInterval, CancellationToken token = default)
    {
        var success = false;
        while (!token.IsCancellationRequested && !success)
        {
            success = await BuildRabbitMQChannelAsync().ConfigureAwait(false);
            if (!success)
            {
                try { await Task.Delay(sleepInterval, token).ConfigureAwait(false); }
                catch { /* SWALLOW */ }
            }
        }
    }

    private static readonly string _makeChannelConnectionUnhealthyError = "Unable to create new inner channel for ChannelHost (Id: {0}). Connection (Id: {0}) is still unhealthy. Try again later...";
    private static readonly string _makeChannelFailedError = "Making a channel failed. Error: {0}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<bool> BuildRabbitMQChannelAsync()
    {
        await _hostLock.WaitAsync().ConfigureAwait(false);

        try
        {
            var connectionHealthy = await _connHost
                .HealthyAsync()
                .ConfigureAwait(false);

            if (!connectionHealthy)
            {
                _logger.LogError(_makeChannelConnectionUnhealthyError, ChannelId, _connHost.ConnectionId);
                return false;
            }

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
            _logger.LogError(_makeChannelFailedError, ex.Message);
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
        { _logger.LogWarning(LogMessages.ChannelHosts.FlowControlled, ChannelId); }
        else
        { _logger.LogInformation(LogMessages.ChannelHosts.FlowControlFinished, ChannelId); }

        FlowControlled = e.Active;

        _hostLock.Release();
    }

    public async Task<bool> ChannelHealthyAsync()
    {
        var connectionHealthy = await _connHost.HealthyAsync().ConfigureAwait(false);

        return connectionHealthy && !FlowControlled && (_channel?.IsOpen ?? false);
    }

    public async Task<bool> ConnectionHealthyAsync()
    {
        return await _connHost.HealthyAsync().ConfigureAwait(false);
    }

    private const int CloseCode = 200;
    private const string CloseMessage = "HouseofCat.RabbitMQ manual close channel initiated.";

    private string _consumerTag = null;

    public string StartConsuming(IBasicConsumer internalConsumer, ConsumerOptions options)
    {
        Guard.AgainstNull(options, nameof(options));
        Guard.AgainstNullOrEmpty(options.QueueName, nameof(options.QueueName));
        Guard.AgainstNullOrEmpty(options.ConsumerName, nameof(options.ConsumerName));

        _consumerTag = GetChannel()
            .BasicConsume(
                options.QueueName,
                options.AutoAck ?? false,
                options.ConsumerName,
                options.NoLocal ?? false,
                options.Exclusive ?? false,
                null,
                internalConsumer);

        _logger.LogInformation(LogMessages.ChannelHosts.ConsumerStartedConsumer, ChannelId, _consumerTag);

        UsedByConsumer = true;

        return _consumerTag;
    }

    public void StopConsuming()
    {
        if (string.IsNullOrEmpty(_consumerTag) || !UsedByConsumer) return;

        _logger.LogInformation(LogMessages.ChannelHosts.ConsumerStopConsumer, ChannelId, _consumerTag);

        try
        {
            GetChannel().BasicCancel(_consumerTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.ChannelHosts.ConsumerStopConsumerError, ChannelId, _consumerTag);
        }
    }

    public void Close()
    {
        try
        { _channel.Close(CloseCode, CloseMessage); }
        catch { /* SWALLOW */ }
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
