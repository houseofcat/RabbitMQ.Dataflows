using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HouseofCat.RabbitMQ.Workflows
{
    public interface IWorkState
    {
        IReceivedData ReceivedData { get; set; }
        IDictionary<string, object> Data { get; set; }
        IDictionary<string, bool> StepSuccess { get; set; }
        string StepIdentifier { get; set; }
    }

    public abstract class WorkState : IWorkState
    {
        [IgnoreDataMember]
        public virtual IReceivedData ReceivedData { get; set; }

        public virtual IDictionary<string, object> Data { get; set; }

        public virtual IDictionary<string, bool> StepSuccess { get; set; }

        public virtual string StepIdentifier { get; set; }
    }
}
