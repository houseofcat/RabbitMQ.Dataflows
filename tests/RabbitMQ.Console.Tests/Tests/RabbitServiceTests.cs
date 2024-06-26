﻿using HouseofCat.RabbitMQ;
using HouseofCat.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace RabbitMQ.ConsoleTests;

public static class RabbitServiceTests
{
    // This test is about testing outages in both the Consumer and Publisher
    // with the shared ConnectionPool all stitched together by the helper RabbitService.
    public static async Task RunRabbitServicePingPongTestAsync(ILoggerFactory loggerFactory, string configFileNamePath)
    {
        var rabbitService = await Shared.SetupRabbitServiceAsync(loggerFactory, configFileNamePath);

        var consumer = rabbitService.GetConsumer(Shared.ConsumerName);
        await consumer.StartConsumerAsync();

        using var preProcessSpan = OpenTelemetryHelpers.StartActiveSpan("messaging.rabbitmq.publisher create message", SpanKind.Internal);
        var body = rabbitService.SerializationProvider.Serialize(new { Name = "TestName", Age = 42 });
        var message = new Message(
            exchange: Shared.ExchangeName,
            routingKey: Shared.RoutingKey,
            body: body,
            payloadId: Guid.NewGuid().ToString())
        {
            ParentSpanContext = preProcessSpan.Context
        };

        await rabbitService.Publisher.QueueMessageAsync(message);
        preProcessSpan?.End();

        // Ping pong the same message.
        await foreach (var receivedMessage in await consumer.ReadUntilStopAsync())
        {
            using var consumerSpan = OpenTelemetryHelpers.StartActiveSpan(
                "messaging.rabbitmq.consumer process",
                SpanKind.Consumer,
                parentContext: receivedMessage.ParentSpanContext ?? default);

            if (receivedMessage?.Message is null)
            {
                receivedMessage?.AckMessage();
                continue;
            }

            await rabbitService.DecomcryptAsync(receivedMessage.Message);

            receivedMessage.Message.Exchange = Shared.ExchangeName;
            receivedMessage.Message.RoutingKey = Shared.RoutingKey;
            receivedMessage.Message.Metadata.PayloadId = Guid.NewGuid().ToString();
            await rabbitService.Publisher.QueueMessageAsync(receivedMessage.Message);

            receivedMessage.AckMessage();
            consumerSpan?.End();
        }
    }

    /// <summary>
    /// Test for sync publish message queueing.
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="configFileNamePath"></param>
    /// <returns></returns>
    public static async Task RunRabbitServiceAltPingPongTestAsync(ILoggerFactory loggerFactory, string configFileNamePath)
    {
        var rabbitService = await Shared.SetupRabbitServiceAsync(loggerFactory, configFileNamePath);

        var consumer = rabbitService.GetConsumer(Shared.ConsumerName);
        await consumer.StartConsumerAsync();

        var body = rabbitService.SerializationProvider.Serialize(new { Name = "TestName", Age = 42 });
        var message = new Message(
            exchange: Shared.ExchangeName,
            routingKey: Shared.RoutingKey,
            body: body,
            payloadId: Guid.NewGuid().ToString());

        rabbitService.Publisher.QueueMessage(message);

        // Ping pong the same message.
        await foreach (var receivedMessage in await consumer.ReadUntilStopAsync())
        {
            if (receivedMessage?.Message is null)
            {
                receivedMessage?.AckMessage();
                continue;
            }

            await rabbitService.DecomcryptAsync(receivedMessage.Message);

            receivedMessage.Message.Exchange = Shared.ExchangeName;
            receivedMessage.Message.RoutingKey = Shared.RoutingKey;
            receivedMessage.Message.Metadata.PayloadId = Guid.NewGuid().ToString();
            rabbitService.Publisher.QueueMessage(receivedMessage.Message);
            receivedMessage.AckMessage();
        }
    }
}
