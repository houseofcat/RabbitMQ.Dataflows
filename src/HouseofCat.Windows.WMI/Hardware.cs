using System;
using System.Management;

namespace HouseofCat.Windows.WMI;

public static class Hardware
{
    private static readonly string Wmi_Root = "root\\CIMV2";
    private static readonly string Wmi_NumberOfProcessors = "SELECT NumberOfProcessors FROM Win32_ComputerSystem";
    private static readonly string Wmi_NumberOfCores = "SELECT NumberOfCores FROM Win32_Processor";

    /// <summary>
    /// Gets the number of CPUs installed on the system.
    /// </summary>
    /// <returns></returns>
    public static int GetCpuCount()
    {
        var processorCount = 0;

        try
        {
            var mos = new ManagementObjectSearcher(Wmi_Root, Wmi_NumberOfProcessors);

            foreach (var mo in mos.Get())
            {
                if (mo["NumberOfProcessors"] != null)
                { processorCount += int.Parse(mo["NumberOfProcessors"].ToString()); }
            }
        }
        catch { processorCount = 0; }

        return processorCount;
    }

    /// <summary>
    /// Gets the number of physical Cores on each CPU.
    /// </summary>
    /// <returns></returns>
    public static int GetCoreCount()
    {
        var coreCount = 0;

        try
        {
            var mos = new ManagementObjectSearcher(Wmi_Root, Wmi_NumberOfCores);

            foreach (var mo in mos.Get())
            {
                if (mo["NumberOfCores"] != null)
                { coreCount += int.Parse(mo["NumberOfCores"].ToString()); }
            }
        }
        catch { coreCount = 0; }

        return coreCount;
    }

    /// <summary>
    /// Gets the number of Logical Processors the OS has access to.
    /// </summary>
    /// <returns></returns>
    public static int GetTotalLogicalProcessorCount()
    {
        return Environment.ProcessorCount;
    }
}
