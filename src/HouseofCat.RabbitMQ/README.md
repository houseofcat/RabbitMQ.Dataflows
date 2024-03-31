<p align="center"><img src="https://s33.postimg.cc/g8pyewwm7/COOKEDRABBIT_1.jpg"></p>

# HouseofCat.RabbitMQ

Review tutorials and documentation under `./guides/rabbitmq`.

Sample Config File
```json
{
  "FactoryOptions": {
    "Uri": "amqp://guest:guest@localhost:5672/",
    "MaxChannelsPerConnection": 2000,
    "HeartbeatInterval": 6,
    "AutoRecovery": true,
    "TopologyRecovery": true,
    "NetRecoveryTimeout": 5,
    "ContinuationTimeout": 10,
    "EnableDispatchConsumersAsync": true
  },
  "PoolOptions": {
    "ServiceName": "HoC.RabbitMQ",
    "MaxConnections": 2,
    "MaxChannels": 10,
    "MaxAckableChannels": 0,
    "SleepOnErrorInterval": 5000,
    "TansientChannelStartRange": 10000,
    "UseTransientChannels": false
  },
  "PublisherOptions": {
    "MessageQueueBufferSize": 100,
    "BehaviorWhenFull": 0,
    "AutoPublisherSleepInterval": 1,
    "CreatePublishReceipts": true,
    "Compress": true,
    "Encrypt": true
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
      "GlobalSettings": "AggressiveSettings",
      "ConsumerName": "ConsumerFromConfig",
      "QueueName": "TestRabbitServiceQueue"
    }
  }
}
```
# [HouseofCat.io](https://houseofcat.io)
<p align="center"><img src="https://s33.postimg.cc/tt2hpn1of/COOKEDRABBIT_Readme_2.jpg"></p>
