namespace HouseofCat.Windows
{
    public static class Enums
    {

        /// <summary>
        /// Allow identifying status of the Thread in the ThreadContainer for more complex operations.
        /// </summary>
        public enum ThreadStatus
        {
            /// <summary>
            /// NoThread means the ThreadContainer is empty.
            /// </summary>
            NoThread,
            /// <summary>
            /// Idle means the Thread in the ThreadContainer is doing nothing.
            /// </summary>
            Idle,
            /// <summary>
            /// Processings means the thread in the ThreadContainer is doing work.
            /// </summary>
            Processing
        }
    }
}
