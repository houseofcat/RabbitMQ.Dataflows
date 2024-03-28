using HouseofCat.RabbitMQ.Dataflows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ.Subscriber;
public interface IQueueSubscriber<TQueueMessage> where TQueueMessage : Letter
{
    Task ConsumeAsync(TQueueMessage message);
    string QueueName { get; }
}

