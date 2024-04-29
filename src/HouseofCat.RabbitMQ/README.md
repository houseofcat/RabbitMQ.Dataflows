<p align="center"><img src="https://s33.postimg.cc/g8pyewwm7/COOKEDRABBIT_1.jpg"></p>

# HouseofCat.RabbitMQ

Review tutorials and documentation under `./guides/rabbitmq`.

Sample Config File
```json
{
  "PoolOptions": {
    "Uri": "amqp://guest:guest@localhost:5672/",
    "MaxChannelsPerConnection": 2000,
    "HeartbeatInterval": 6,
    "AutoRecovery": true,
    "TopologyRecovery": true,
    "NetRecoveryTimeout": 5,
    "ContinuationTimeout": 10,
    "EnableDispatchConsumersAsync": true
    "ServiceName": "HoC.RabbitMQ",
    "Connections": 2,
    "Channels": 10,
    "AckableChannels": 0,
    "SleepOnErrorInterval": 5000,
    "TansientChannelStartRange": 10000,
    "UseTransientChannels": false
  },
  "PublisherOptions": {
    "MessageQueueBufferSize": 100,
    "BehaviorWhenFull": 0,
    "CreatePublishReceipts": true,
    "Compress": false,
    "Encrypt": false,
    "WaitForConfirmationTimeoutInMilliseconds": 500
  },
  "ConsumerOptions": {
    "TestConsumer": {
      "Enabled": true,
      "ConsumerName": "TestConsumer",
      "BatchSize": 5,
      "BehaviorWhenFull": 0,
      "UseTransientChannels": true,
      "AutoAck": false,
      "NoLocal": false,
      "Exclusive": false,
      "QueueName": "TestQueue",
      "QueueArguments": null,
      "SendQueueName": "TestTargetQueue",
      "SendQueueArgs": null,
      "ErrorQueueName": "TestQueue.Error",
      "ErrorQueueArgs": null,
      "BuildQueues": true,
      "BuildQueueDurable": true,
      "BuildQueueExclusive": false,
      "BuildQueueAutoDelete": false,
      "WorkflowName": "TestConsumerWorkflow",
      "WorkflowMaxDegreesOfParallelism": 1,
      "WorkflowConsumerCount": 1,
      "WorkflowBatchSize": 5,
      "WorkflowEnsureOrdered": false,
      "WorkflowWaitForCompletion": false,
      "WorkflowSendCompressed": false,
      "WorkflowSendEncrypted": false
    }
  }
}
```
# [HouseofCat.io](https://houseofcat.io)
<p align="center"><img src="https://s33.postimg.cc/tt2hpn1of/COOKEDRABBIT_Readme_2.jpg"></p>
