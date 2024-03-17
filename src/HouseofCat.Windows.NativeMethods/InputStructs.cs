using System;
using System.Runtime.InteropServices;

namespace HouseofCat.Windows;

public static class InputStructs
{
    public struct MOUSEINPUT
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    public struct HARDWAREINPUT
    {
        public uint Msg;
        public ushort ParamL;
        public ushort ParamH;
    }

    public struct KEYBDINPUT
    {
        public ushort KeyCode;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MOUSEKEYBDHARDWAREINPUT
    {
        [FieldOffset(0)]
        public MOUSEINPUT Mouse;

        [FieldOffset(0)]
        public KEYBDINPUT Keyboard;

        [FieldOffset(0)]
        public HARDWAREINPUT Hardware;
    }

    public struct INPUT
    {
        public uint Type;
        public MOUSEKEYBDHARDWAREINPUT Data;
    }
}
