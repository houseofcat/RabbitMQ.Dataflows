using System;

namespace HouseofCat.Windows
{
    public static class Constants
    {
        public const string DOWN = "KEYDOWN";
        public const string UP = "KEYUP";

        public static class KeyPointers
        {
            public const int KeyDownFlag = 0x0000;
            public const int KeyUpFlag = 0x0002;

            public const int WM_KEYDOWN = 0x0100;
            public const int WM_KEYUP = 0x0101;
            public const int WM_SYSKEYDOWN = 0x0104;
            public const int WM_SYSKEYUP = 0x0105;

            public static IntPtr WM_KEYDOWN_PTR = (IntPtr)WM_KEYDOWN;
            public static IntPtr WM_KEYUP_PTR = (IntPtr)WM_KEYUP;
            public static IntPtr WM_SYSKEYDOWN_PTR = (IntPtr)WM_SYSKEYDOWN;
            public static IntPtr WM_SYSKEYUP_PTR = (IntPtr)WM_SYSKEYUP;
        }
    }
}
