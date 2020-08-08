using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace HouseofCat.RabbitMQ.Pipelines
{
    public interface IWorkState
    {
        // Inbound Data
        IReceivedData ReceivedData { get; set; }
        IDictionary<string, object> Data { get; set; }

        // Routing Logic
        IDictionary<string, bool> StepSuccess { get; set; }
        string StepIdentifier { get; set; }

        // Error Handling
        bool IsFaulted { get; set; }
        ExceptionDispatchInfo EDI { get; set; }

        // Outbound
        byte[] SendData { get; set; }
        Letter SendLetter { get; set; }
        bool SendLetterSent { get; set; }
    }

    public abstract class WorkState : IWorkState
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
