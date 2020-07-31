<p align="center"><img src="https://s33.postimg.cc/g8pyewwm7/COOKEDRABBIT_1.jpg"></p>

## CookedRabbit.Core

### This is the migrated CookedRabbit.Core development.

Original Repo is located at:  
https://github.com/houseofcat/CookedRabbit/tree/develop/NetCore/CookedRabbit.Core

### Development Details

 * RabbitMQ Server v3.8.5
 * Erlang 23.0
 * NetCore 3.1 (Primarily) / NET 5.0 (preview 6)
 * C# 8.0-9.0 w/ Visual Studio 2019 Enterprise
 
New Global behaviors are now addable via config. See PipelineClient in examples folder for full details. This will help reduce config clutter with consumers all sharing the same behavior/settings.  
```javascript
   "GlobalConsumerSettings": {
    "AggressiveSettings": {
      "ErrorSuffix": "Error",
      "BatchSize": 128,
      "BehaviorWhenFull": 0,
      "SleepOnIdleInterval": 0,
      "UseTransientChannels": true,
      "AutoAck": false,
      "NoLocal": false,
      "Exclusive": false,
      "GlobalConsumerPipelineSettings": {
        "WaitForCompletion": false,
        "MaxDegreesOfParallelism": 64
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
      "GlobalConsumerPipelineSettings": {
        "WaitForCompletion": true,
        "MaxDegreesOfParallelism": 1
      }
    }
  },
  "ConsumerSettings": {
    "ConsumerFromConfig": {
      "Enabled": true,
      "GlobalSettings": "AggressiveSettings",
      "ConsumerName": "ConsumerFromConfig",
      "QueueName": "TestRabbitServiceQueue"
    }
  }
```

# [HouseofCat.io](https://houseofcat.io)
<p align="center"><img src="https://s33.postimg.cc/tt2hpn1of/COOKEDRABBIT_Readme_2.jpg"></p>
