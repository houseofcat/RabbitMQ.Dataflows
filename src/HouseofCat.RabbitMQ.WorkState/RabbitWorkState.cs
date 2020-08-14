using HouseofCat.Workflows;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace HouseofCat.RabbitMQ.WorkState
{
    public interface IRabbitWorkState : IWorkState
    {
        // Inbound Data
        IReceivedData ReceivedData { get; set; }
        Letter SendLetter { get; set; }
        bool SendLetterSent { get; set; }
    }

    public abstract class RabbitWorkState : IRabbitWorkState
    {
        [IgnoreDataMember]
        public virtual IReceivedData ReceivedData { get; set; }
        public virtual byte[] SendData { get; set; }
        public virtual Letter SendLetter { get; set; }
        public virtual bool SendLetterSent { get; set; }

        public virtual IDictionary<string, object> Data { get; set; }

        public virtual IDictionary<string, bool> StepSuccess { get; set; }

        public virtual string StepIdentifier { get; set; }
        public bool IsFaulted { get; set; }
        public ExceptionDispatchInfo EDI { get; set; }
    }
}
