using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static HouseofCat.Windows.InputStructs;

namespace HouseofCat.Windows
{
    public static class NativeMethods
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

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #region Helpers

        private const string DOWN = "KEYDOWN";
        private const string UP = "KEYUP";

        public static (string action, uint flag) GetActivityFlags(int action)
        {
            switch (action)
            {
                case WM_KEYUP:
                case WM_SYSKEYUP:
                    return (UP, KeyUpFlag);
                case WM_KEYDOWN:
                case WM_SYSKEYDOWN:
                default:
                    return (DOWN, KeyDownFlag);
            }
        }

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
}
