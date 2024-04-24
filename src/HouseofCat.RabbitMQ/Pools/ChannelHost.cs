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
    void StopConsuming();

    Task WaitUntilChannelIsReadyAsync(int sleepInterval, CancellationToken token = default);
    Task<bool> BuildRabbitMQChannelAsync(int autoRecoveryDelay = 1000, CancellationToken token = default);

    void Close();
    bool ChannelHealthy();
    bool ConnectionHealthy();
    string DisplayId();
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
        var connectionHealthy = _connHost.Healthy();
        if (!connectionHealthy)
        {
            _logger.LogInformation(_sleepingUntilConnectionHealthy, ChannelId, _connHost.ConnectionId);
            while (!token.IsCancellationRequested && !connectionHealthy)
            {
                await Task.Delay(sleepInterval, token).ConfigureAwait(false);

                connectionHealthy = _connHost.Healthy();
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
        try
        {
            if (!_connHost.Healthy())
            {
                _logger.LogError(_makeChannelConnectionUnhealthyError, ChannelId, _connHost.ConnectionId);
                return false;
            }

            if (Channel != null)
            {
                // One last check to see if the channel auto-recovered.
                var healthy = ChannelHealthy();
                if (!healthy)
                {
                    await Task.Delay(autoRecoveryDelay, token);

                    healthy = ChannelHealthy();
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
    }

    protected virtual void ChannelClose(object sender, ShutdownEventArgs e)
    {
        _logger.LogInformation(e.ReplyText);
        Closed = true;
    }

    private static readonly string _flowControlled = "ChannelHost [Id: {0}] - Flow control event has triggered.";
    private static readonly string _flowControlFinished = "ChannelHost [Id: {0}] - Flow control event has resolved itself.";

    protected virtual void FlowControl(object sender, FlowControlEventArgs e)
    {
        if (e.Active)
        { _logger.LogWarning(_flowControlled, ChannelId); }
        else
        { _logger.LogInformation(_flowControlFinished, ChannelId); }

        FlowControlled = e.Active;
    }

    public bool ChannelHealthy()
    {
        var connectionHealthy = _connHost.Healthy();

        return connectionHealthy && (Channel?.IsOpen ?? false);
    }

    public bool ConnectionHealthy()
    {
        return _connHost.Healthy();
    }

    private string _consumerTag;

    private static readonly string _consumerStartedConsumer = "ChannelHost [Id: {0}] - Starting consuming. ConsumerTag: [{1}]";

    public string StartConsuming(IBasicConsumer internalConsumer, ConsumerOptions options)
    {
        Guard.AgainstNull(options, nameof(options));
        Guard.AgainstNullOrEmpty(options.QueueName, nameof(options.QueueName));
        Guard.AgainstNullOrEmpty(options.ConsumerName, nameof(options.ConsumerName));

        _consumerTag = Channel
            .BasicConsume(
                options.QueueName,
                options.AutoAck,
                options.ConsumerName,
                options.NoLocal,
                options.Exclusive,
                null,
                internalConsumer);

        _logger.LogDebug(_consumerStartedConsumer, ChannelId, _consumerTag);

        UsedByConsumer = true;

        return _consumerTag;
    }

    private static readonly string _consumerStopConsumer = "ChannelHost [Id: {0}] - Stopping consuming using ConsumerTag: [{1}]";
    private static readonly string _consumerStopConsumerError = "ChannelHost [Id: {0}] - Error stopping consuming using ConsumerTag: [{1}]";

    public void StopConsuming()
    {
        if (string.IsNullOrEmpty(_consumerTag) || !UsedByConsumer) return;

        try
        {
            if (ChannelHealthy())
            {
                _logger.LogInformation(_consumerStopConsumer, ChannelId, _consumerTag);
                Channel.BasicCancel(_consumerTag);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _consumerStopConsumerError, ChannelId, _consumerTag);
        }
    }

    public string DisplayId()
    {
        return $"{_connHost.Connection.ClientProvidedName}:{Channel.ChannelNumber}";
    }

    private const int CloseCode = 200;
    private static readonly string _closeMessage = "HouseofCat.RabbitMQ manual close initiated for ChannelHost [Id: {0}] - [{1}].";

    public void Close()
    {
        try
        {
            Channel.Close(
                CloseCode,
                string.Format(_closeMessage, ChannelId, DisplayId()));
        }
        catch { /* SWALLOW */ }
    }

    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            Channel = null;
            _connHost = null;
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
