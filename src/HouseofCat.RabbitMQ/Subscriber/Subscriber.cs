using HouseofCat.RabbitMQ.Pools;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using HouseofCat.RabbitMQ.Services;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;

namespace HouseofCat.RabbitMQ.Subscriber;

public class Subscriber<TMessageConsumer, TQueueMessage> : IHostedService
    where TMessageConsumer : IQueueSubscriber<TQueueMessage>
    where TQueueMessage : Letter
{
    private readonly ILogger<Subscriber<TMessageConsumer, TQueueMessage>> _logger;
    private readonly IQueueSubscriber<Letter> _messageConsumer;
    private readonly IRabbitService _rabbitService;
    private readonly SemaphoreSlim _conLock = new SemaphoreSlim(1, 1);
    private bool _shutdown;
    private bool autoAck = false;
    private int sleepInterval = 5000;


    private static readonly ActivitySource _activitySource =
        new ActivitySource(Assembly.GetEntryAssembly().GetName().Name ?? "HouseofCat.RabbitMQ");

    private static readonly TextMapPropagator Propagator =
        Propagators.DefaultTextMapPropagator;


    public Subscriber(
        ILogger<Subscriber<TMessageConsumer, TQueueMessage>> logger,
        IRabbitService rabbitService,
        IQueueSubscriber<Letter> messageConsumer)
    {
        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));

        _messageConsumer = messageConsumer ??
            throw new ArgumentNullException(nameof(messageConsumer));

        _rabbitService = rabbitService ??
            throw new ArgumentNullException(nameof(rabbitService));
    }

    private IChannelHost _channelHost { get; set; }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        // Messaging is async so we don't need to wait for it to complete. On top of this
        // the APIs are blocking, so we need to run this on a background thread.
        _ = Task.Factory.StartNew(() =>
        {
            StartProcessing(stoppingToken);
        }, TaskCreationOptions.LongRunning);

        return Task.CompletedTask;
    }

    private void StartProcessing(CancellationToken stoppingToken)
    {
        try
        {
            _channelHost = _rabbitService.ChannelPool.GetChannelAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            StartInternal();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting RabbitMQ connection");

            Task.Delay(sleepInterval, stoppingToken).ConfigureAwait(false);
            StartProcessing(stoppingToken);
        }
    }

    private void StartInternal()
    {
        _logger.LogInformation("Starting RabbitMQ connection on a background thread");

        var consumerChannel = _channelHost.GetChannel() ?? throw new InvalidOperationException("RabbitMQ connection is not open");

        var consumer = new AsyncEventingBasicConsumer(consumerChannel);

        _logger.LogTrace("Starting RabbitMQ basic consume");

        consumerChannel.CallbackException += (sender, ea) =>
        {
            _logger.LogWarning(ea.Exception, "Error with RabbitMQ consumer channel");
        };

        consumer.Received += OnMessageReceived;
        consumer.Shutdown += ConsumerShutdownAsync;

        consumerChannel.BasicConsume(
            queue: _messageConsumer.QueueName,
            autoAck: autoAck,
            consumer: consumer);
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        // Extract the PropagationContext of the upstream parent from the message headers.
        var parentContext = Propagator.Extract(default, eventArgs.BasicProperties, this.ExtractTraceContextFromBasicProperties);
        Baggage.Current = parentContext.Baggage;

        // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name
        var activityName = $"{eventArgs.RoutingKey} receive";

        using var activity = _activitySource.StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext);


        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);


        try
        {
            await ProcessEvent(activity, eventName, new ReceivedData(_channelHost.GetChannel(), eventArgs, !autoAck));
            _channelHost.GetChannel().BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error accepting the message \"{Message}\"", message);
        }
    }

    private async Task ProcessEvent(Activity activity, string eventName, ReceivedData receivedData)
    {
        _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);
        try
        {
            await _rabbitService.DecomcryptAsync(receivedData.Letter);
            var message = Encoding.UTF8.GetString(receivedData.Letter.Body.Span);

            activity?.SetTag("message", message);

            AddMessagingTags(activity, receivedData.Letter);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorrer an error during encript/decompress phase");
        }

        try
        {
            await _messageConsumer.ConsumeAsync(receivedData.Letter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has been raise the consumer ");
            // Reject the message
            _channelHost.GetChannel().BasicReject(receivedData.DeliveryTag, requeue: false);
        }

    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!await _conLock.WaitAsync(0).ConfigureAwait(false)) return;

        _shutdown = true;

        try
        {
            await _rabbitService.ChannelPool.ShutdownAsync().ConfigureAwait(false);
        }
        finally { _conLock.Release(); }
    }

    private async Task ConsumerShutdownAsync(object sender, ShutdownEventArgs e)
    {
        if (await _conLock.WaitAsync(0))
        {
            try
            { await HandleUnexpectedShutdownAsync(e).ConfigureAwait(false); }
            finally
            { _conLock.Release(); }
        }
    }

    private async Task HandleUnexpectedShutdownAsync(ShutdownEventArgs e)
    {
        if (!_shutdown)
        {
            await Task.Yield();
            bool success;
            do
            {
                success = await _channelHost.BuildRabbitMQChannelAsync().ConfigureAwait(false);

                if (success)
                {
                    _logger.LogWarning(
                        "Consumer ({0}) shutdown event has occurred. Reason: {1}. Attempting to restart consuming...",
                        nameof(TMessageConsumer),
                        e.ReplyText);

                    StartInternal();
                    success = true;
                }
            }
            while (!_shutdown && !success);
        }
    }

    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault ? new DefaultJsonTypeInfoResolver() : JsonTypeInfoResolver.Combine()
    };

    private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
    {
        try
        {
            if (props.Headers.TryGetValue(key, out var value))
            {
                var bytes = value as byte[];
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract trace context.");
        }

        return Enumerable.Empty<string>();
    }

    public static void AddMessagingTags(Activity activity, IMessage message)
    {
        // See:
        //   * https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#messaging-attributes
        //   * https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/rabbitmq.md
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination_kind", "queue");
        activity?.SetTag("messaging.destination", message.Envelope.Exchange);
        activity?.SetTag("messaging.rabbitmq.routing_key", message.Envelope.RoutingKey);
        activity?.SetTag("messaging.message.id", message.MessageId);
        activity?.SetTag("messaging.operation", "subscribe");
    }
}