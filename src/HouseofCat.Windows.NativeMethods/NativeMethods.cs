using System;
using System.Runtime.InteropServices;
using static HouseofCat.Windows.InputStructs;

namespace HouseofCat.Windows;

public static class NativeMethods
{
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    #region Threading Section

    /// <summary>
    /// Gets PID using Kerne32.dll.
    /// </summary>
    /// <returns>ProcessId</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int GetCurrentProcessId();

    /// <summary>
    /// Get the amount of RAM (in KB) physically installed.
    /// </summary>
    /// <param name="TotalMemoryInKilobytes"></param>
    /// <returns></returns>
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

    /// <summary>
    /// Gets the current Thread.
    /// </summary>
    /// <returns></returns>
    [DllImport("Kernel32.dll")]
    public static extern IntPtr GetCurrentThread();

    /// <summary>
    /// Sets the Thread affinity to a logical processor.
    /// </summary>
    /// <param name="hThread"></param>
    /// <param name="dwThreadAffinityMask"></param>
    /// <returns></returns>
    [DllImport("Kernel32.dll")]
    public static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

    #endregion

    #region Reading Section

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    #endregion

    #region Input Section

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();

    #endregion
}
