using HouseofCat.Utilities.Errors;
using HouseofCat.Utilities.Helpers;
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
    public IModel Channel { get; }
    bool Closed { get; }
    bool FlowControlled { get; }
    bool UsedByConsumer { get; }

    string StartConsuming(IBasicConsumer internalConsumer, ConsumerOptions options);
    Task StopConsumingAsync();

    Task WaitUntilChannelIsReadyAsync(int sleepInterval, CancellationToken token = default);
    Task<bool> BuildRabbitMQChannelAsync(int autoRecoveryDelay = 1000, CancellationToken token = default);

    void Close();
    Task<bool> ChannelHealthyAsync();
    Task<bool> ConnectionHealthyAsync();
}

public class ChannelHost : IChannelHost, IDisposable
{
    private readonly ILogger<ChannelHost> _logger;
    public IModel Channel { get; private set; }
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
        _logger = LogHelpers.GetLogger<ChannelHost>();

        ChannelId = channelId;
        _connHost = connHost;
        Ackable = ackable;

        BuildRabbitMQChannelAsync().GetAwaiter().GetResult();
    }

    private static readonly string _sleepingUntilConnectionHealthy = "ChannelHost [Id: {0}] is sleeping until Connection [Id: {1}] is healthy...";

    public virtual async Task WaitUntilChannelIsReadyAsync(int sleepInterval, CancellationToken token = default)
    {
        var connectionHealthy = await _connHost.HealthyAsync();
        if (!connectionHealthy)
        {
            _logger.LogInformation(_sleepingUntilConnectionHealthy, ChannelId, _connHost.ConnectionId);
            while (!token.IsCancellationRequested && !connectionHealthy)
            {
                await Task.Delay(sleepInterval, token).ConfigureAwait(false);

                connectionHealthy = await _connHost.HealthyAsync();
            }
        }

        var success = false;
        while (!token.IsCancellationRequested && !success)
        {
            success = await BuildRabbitMQChannelAsync(sleepInterval, token).ConfigureAwait(false);
            if (!success)
            {
                await Task.Delay(sleepInterval, token).ConfigureAwait(false);
            }
        }
    }

    private static readonly string _makeChannelConnectionUnhealthyError = "Unable to create new inner channel for ChannelHost [Id: {0}]. Connection [Id: {1}] is still unhealthy. Try again later...";
    private static readonly string _makeChannelNotNeeded = "ChannelHost [Id: {0}] auto-recovered for Connection [Id: {1}]. New channel not needed.";
    private static readonly string _makeChannelSuccessful = "ChannelHost [Id: {0}] successfully created a new RabbitMQ channel on Connection [Id: {1}].";
    private static readonly string _makeChannelFailedError = "Making a channel failed. Error: {0}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<bool> BuildRabbitMQChannelAsync(int autoRecoveryDelay = 1000, CancellationToken token = default)
    {
        await _hostLock.WaitAsync(token).ConfigureAwait(false);

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

            if (Channel != null)
            {
                // One last check to see if the channel auto-recovered.
                var healthy = await ChannelHealthyAsync().ConfigureAwait(false);
                if (!healthy)
                {
                    await Task.Delay(autoRecoveryDelay, token);

                    healthy = await ChannelHealthyAsync().ConfigureAwait(false);
                }

                if (healthy)
                {
                    _logger.LogInformation(_makeChannelNotNeeded, ChannelId, _connHost.ConnectionId);
                    return true;
                }

                Channel.FlowControl -= FlowControl;
                Channel.ModelShutdown -= ChannelClose;
                Close();
                Channel = null;
            }

            Channel = _connHost.Connection.CreateModel();

            if (Ackable)
            {
                Channel.ConfirmSelect();
            }

            Channel.FlowControl += FlowControl;
            Channel.ModelShutdown += ChannelClose;

            _logger.LogDebug(_makeChannelSuccessful, ChannelId, _connHost.ConnectionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(_makeChannelFailedError, ex.Message);
            Channel = null;
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

        return connectionHealthy && (Channel?.IsOpen ?? false);
    }

    public async Task<bool> ConnectionHealthyAsync()
    {
        return await _connHost.HealthyAsync().ConfigureAwait(false);
    }

    private string _consumerTag = null;

    public string StartConsuming(IBasicConsumer internalConsumer, ConsumerOptions options)
    {
        Guard.AgainstNull(options, nameof(options));
        Guard.AgainstNullOrEmpty(options.QueueName, nameof(options.QueueName));
        Guard.AgainstNullOrEmpty(options.ConsumerName, nameof(options.ConsumerName));

        _consumerTag = Channel
            .BasicConsume(
                options.QueueName,
                options.AutoAck ?? false,
                options.ConsumerName,
                options.NoLocal ?? false,
                options.Exclusive ?? false,
                null,
                internalConsumer);

        _logger.LogDebug(LogMessages.ChannelHosts.ConsumerStartedConsumer, ChannelId, _consumerTag);

        UsedByConsumer = true;

        return _consumerTag;
    }

    public async Task StopConsumingAsync()
    {
        if (string.IsNullOrEmpty(_consumerTag) || !UsedByConsumer) return;

        try
        {
            var healthy = await ChannelHealthyAsync().ConfigureAwait(false);
            if (healthy)
            {
                _logger.LogInformation(LogMessages.ChannelHosts.ConsumerStopConsumer, ChannelId, _consumerTag);
                Channel.BasicCancel(_consumerTag);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.ChannelHosts.ConsumerStopConsumerError, ChannelId, _consumerTag);
        }
    }

    private const int CloseCode = 200;
    private const string CloseMessage = "HouseofCat.RabbitMQ manual close Channelhost [Id: {0} - CN: {1}] initiated.";

    public void Close()
    {
        try
        {
            _logger.LogInformation(CloseMessage, ChannelId, Channel.ChannelNumber);
            Channel.Close(
                CloseCode,
                string.Format(CloseMessage, ChannelId, Channel.ChannelNumber));
        }
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

            Channel = null;
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
