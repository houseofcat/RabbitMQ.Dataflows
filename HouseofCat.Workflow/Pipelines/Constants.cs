namespace HouseofCat.Workflows.Pipelines
{
    public static class Constants
    {
        public readonly static string NotFinalized = "Pipeline is not ready for receiving work as it has not been finalized yet.";
        public readonly static string AlreadyFinalized = "Pipeline is already finalized and ready for use.";
        public readonly static string CantFinalize = "Pipeline can't finalize as no steps have been added.";
        public readonly static string InvalidAddError = "Pipeline is already finalized and you can no longer add steps.";
        public readonly static string InvalidStepFound = "Pipeline can't chain the last step to this new step. Unexpected type found on the previous step.";

        public readonly static string Healthy = "Pipeline ({0}) appears healthy.";
        public readonly static string Faulted = "Pipeline ({0}) has faulted. Replace/rebuild Pipeline or restart Application...";
        public readonly static string AwaitsCompletion = "Pipeline ({0}) awaits completion.";
        public readonly static string Queued = "Pipeline ({0}) queued item for execution.";
    }
}
