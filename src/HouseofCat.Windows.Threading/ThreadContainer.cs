using System;
using System.Threading;
using System.Threading.Tasks;
using static HouseofCat.Windows.Enums;

namespace HouseofCat.Windows
{
    /// <summary>
    /// ThreadContainer holds a thread and the CPU information inteded for it to be assigned a specific
    /// CPU core or LogicalProcessor.
    /// </summary>
    public class ThreadContainer
    {
        /// <summary>
        /// The Thread that will be assigned to a Core/Logical Processor.
        /// </summary>
        public Thread Thread { get; set; } = null;

        /// <summary>
        /// The CPU this Thread is assigned to.
        /// </summary>
        public int CpuNumber { get; set; } = 0;

        /// <summary>
        /// The physical Core/Logical Processor this Thread is to be assigned to.
        /// </summary>
        public int CpuCoreNumber { get; set; } = 0;

        /// <summary>
        /// The physical Core/Logical Processor this Thread is to be assigned to.
        /// </summary>
        public int CpuLogicalProcessorNumber { get; set; } = 0;

        /// <summary>
        /// The physical Core count per CPU.
        /// </summary>
        public int CoresPerCpu { get; set; } = 0;

        /// <summary>
        /// The Logical Processors per CPU as seen by the OS.
        /// </summary>
        public int LogicalProcessorsPerCpu { get; set; } = 0;

        /// <summary>
        /// Allows the thread to see when it needs to stop doing work.
        /// </summary>
        public bool TerminateSelf { get; set; } = false;

        /// <summary>
        /// Tells calling methods to use waits and by how much (ms).
        /// </summary>
        public int ThrottleTime { get; set; } = 0;

        /// <summary>
        /// The ThreadStatus helps quickly identify what work state is for the Thread stored here.
        /// </summary>
        public ThreadStatus ThreadStatus { get; set; } = ThreadStatus.NoThread;

        private Func<object, Task> _asyncFuncWork;

        /// <summary>
        /// Allows for custom workloads to be assigned to Threads.
        /// </summary>
        public Func<object, Task> AsyncFuncWork
        {
            get { return _asyncFuncWork; }
            set
            {
                if (Monitor.TryEnter(FuncLock, TimeSpan.FromMilliseconds(100)))
                {
                    _asyncFuncWork = value;

                    Monitor.Exit(FuncLock);
                }
            }
        }

        /// <summary>
        /// Used for preventing AsyncFuncWork being accessed while in use.
        /// </summary>
        public object FuncLock = new object();
    }
}
