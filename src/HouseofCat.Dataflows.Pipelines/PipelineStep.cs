using System.Threading.Tasks.Dataflow;

namespace HouseofCat.Dataflows.Pipelines
{
    public class PipelineStep
    {
        public int StepIndex { get; set; }
        public IDataflowBlock Block { get; set; }
        public bool IsAsync { get; set; }
        public bool IsLastStep { get; set; }

        public bool IsFaulted
        {
            get { return Block?.Completion?.IsFaulted ?? false; }
        }
    }
}
