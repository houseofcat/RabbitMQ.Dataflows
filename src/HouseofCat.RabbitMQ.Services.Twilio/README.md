# HouseofCat.RabbitMQ.Services.Twilio

RabbitMQ -> SMS Text Message with Piplines.

Set up you appsettings.json

```
  "HouseofCat": {
    "NotificationService": {
      "ConsumerName": "Text.Message.Consumer",
      "MaxDoP": 4,
      "From": "+13016835053",
      "Token": "TwilioToken",
      "Account": "TwilioAccount",
    }
  }
```

And a separate RabbitMQ Config
```
{
  "FactoryOptions": {
    "Uri": "amqp://guest:guest@localhost:5672/",
    "MaxChannelsPerConnection": 2000,
    "HeartbeatInterval": 6,
    "AutoRecovery": true,
    "TopologyRecovery": true,
    "NetRecoveryTimeout": 10,
    "ContinuationTimeout": 10,
    "EnableDispatchConsumersAsync": true,
    "SslOptions": {
      "EnableSsl": false,
      "CertServerName": "",
      "LocalCertPath": "",
      "LocalCertPassword": "",
      "ProtocolVersions": 3072
    }
  },
  "PoolOptions": {
    "ServiceName": "HoC.NotificationService",
    "MaxConnections": 5,
    "MaxChannels": 25,
    "SleepOnErrorInterval": 1000
  },
  "PublisherOptions": {
    "LetterQueueBufferSize": 100,
    "PriorityLetterQueueBufferSize": 100,
    "BehaviorWhenFull": 0,
    "AutoPublisherSleepInterval": 0,
    "CreatePublishReceipts": true,
    "Compress": false,
    "Encrypt": false
  },
  "GlobalConsumerOptions": {
    "AggressiveSettings": {
      "ErrorSuffix": "Error",
      "BatchSize": 128,
      "BehaviorWhenFull": 0,
      "SleepOnIdleInterval": 0,
      "UseTransientChannels": true,
      "AutoAck": false,
      "NoLocal": false,
      "Exclusive": false,
      "GlobalConsumerPipelineOptions": {
        "WaitForCompletion": false,
        "MaxDegreesOfParallelism": 64,
        "EnsureOrdered": false
      }
    },
    "ModerateSettings": {
      "ErrorSuffix": "Error",
      "BatchSize": 48,
      "BehaviorWhenFull": 0,
      "SleepOnIdleInterval": 100,
      "UseTransientChannels": true,
      "AutoAck": false,
      "NoLocal": false,
      "Exclusive": false,
      "GlobalConsumerPipelineOptions": {
        "WaitForCompletion": true,
        "MaxDegreesOfParallelism": 24,
        "EnsureOrdered": false
      }
    },
    "LightSettings": {
      "ErrorSuffix": "Error",
      "BatchSize": 8,
      "BehaviorWhenFull": 0,
      "SleepOnIdleInterval": 100,
      "UseTransientChannels": true,
      "AutoAck": false,
      "NoLocal": false,
      "Exclusive": false,
      "GlobalConsumerPipelineOptions": {
        "WaitForCompletion": true,
        "MaxDegreesOfParallelism": 4,
        "EnsureOrdered": false
      }
    },
    "SingleThreaded": {
      "ErrorSuffix": "Error",
      "BatchSize": 1,
      "BehaviorWhenFull": 0,
      "SleepOnIdleInterval": 0,
      "UseTransientChannels": true,
      "AutoAck": false,
      "NoLocal": false,
      "Exclusive": false,
      "GlobalConsumerPipelineOptions": {
        "WaitForCompletion": true,
        "MaxDegreesOfParallelism": 1,
        "EnsureOrdered": false
      }
    }
  },
  "ConsumerOptions": {
    "ConsumerFromConfig": {
      "Enabled": true,
      "GlobalSettings": "LightSettings",
      "ConsumerName": "Text.Message.Consumer",
      "QueueName": "Text.Messages"
    }
  }
}
```