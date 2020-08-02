namespace HouseofCat.Gremlins
{
    /// <summary>
    /// This class contains variables related to the System such as hardware info.
    /// </summary>
    public class SystemVariables
    {
        /// <summary>
        /// Number of CPUs installed.
        /// </summary>
        public int CpuCount { get; set; } = 1;

        /// <summary>
        /// Number of physical Cores per CPU.
        /// </summary>
        public int CoreCount { get; set; } = 1;

        /// <summary>
        /// The logical processors as seen by the OS.
        /// </summary>
        public int LogicalProcessorCount { get; set; } = 1;

        /// <summary>
        /// Calculation of the physical Cores per CPU.
        /// </summary>
        public int CoresPerCpu => CoreCount / CpuCount;

        /// <summary>
        /// Calculation of the Logical Processors per CPU.
        /// </summary>
        public int LogicalProcessorsPerCpu => LogicalProcessorCount / CpuCount;
    }
}
