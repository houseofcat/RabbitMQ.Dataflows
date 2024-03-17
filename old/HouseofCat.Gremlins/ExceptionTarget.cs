namespace HouseofCat.Gremlins;

public class ExceptionTarget
{
    public bool AllowConsecutiveFailure { get; set; }
    public int FailureCount { get; set; }
    public int FailureCountMax { get; set; }
    public bool GuaranteeFailure { get; set; }
    public bool LastIterationFailed { get; set; }
}
