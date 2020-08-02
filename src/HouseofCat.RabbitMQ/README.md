<p align="center"><img src="https://s33.postimg.cc/g8pyewwm7/COOKEDRABBIT_1.jpg"></p>

# HouseofCat.RabbitMQ

### This is the migrated CookedRabbit.Core development.

Original Repos are located at:  
https://github.com/houseofcat/CookedRabbit/tree/develop/NetCore/CookedRabbit.Core

And  
https://github.com/houseofcat/RabbitMQ.Core/tree/master/CookedRabbit.Core  

### Development Details

 * RabbitMQ Server v3.8.5
 * Erlang 23.0
 * NetCore 3.1 (Primarily) / NET 5.0 (preview 7)
 * C# 8.0-9.0 w/ Visual Studio 2019 Enterprise
 
New Global behaviors are now addable via config. See ConsumerPipelineMicroservice in examples folder for full details. This will help reduce config clutter with consumers all sharing the same behavior/settings.  

Sample Config File
```javascript
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
    "ServiceName": "HoC.RabbitMQ",
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
      "GlobalSettings": "AggressiveSettings",
      "ConsumerName": "ConsumerFromConfig",
      "QueueName": "TestRabbitServiceQueue"
    }
  }
}
```
# [HouseofCat.io](https://houseofcat.io)
<p align="center"><img src="https://s33.postimg.cc/tt2hpn1of/COOKEDRABBIT_Readme_2.jpg"></p>
