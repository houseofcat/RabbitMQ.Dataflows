using HouseofCat.RabbitMQ.Dataflows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Subscriber;
public interface IQueueSubscriber<in TQueueMessage> where TQueueMessage : BaseSubscriberMessage
{
    Task ConsumeAsync(TQueueMessage message);
    string QueueName { get; }
}

