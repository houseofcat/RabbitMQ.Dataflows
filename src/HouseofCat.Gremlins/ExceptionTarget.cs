namespace HouseofCat.Gremlins
{
    public class ExceptionTarget
    {
        public bool AllowConsecutiveFailure = false;
        public int FailureCount { get; set; } = 0;
        public int FailureCountMax { get; set; } = 0;
        public bool GuaranteeFailure { get; set; } = false;
        public bool LastIterationFailed { get; set; } = false;
    }
}
