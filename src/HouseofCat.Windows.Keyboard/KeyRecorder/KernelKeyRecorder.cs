using HouseofCat.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace HouseofCat.Windows;

public class KernelKeyRecorder
{
    public Dictionary<long, List<KeyEvent>> KeyPlaybackBuffer { get; set; } = new Dictionary<long, List<KeyEvent>>();
    private Stopwatch SW;

    private const int KeyboardHookId = 13;
    private IntPtr KeyboardHook;

    private Channel<string> KeyChannel { get; }
    public ChannelReader<string> KeyChannelReader { get; }
    private ChannelWriter<string> KeyChannelWriter { get; }

    // Pinning to protect from/against the Garbage Collector;
    private readonly HouseofCat.Windows.NativeMethods.LowLevelKeyboardProc KeyboardHookProc;

    public bool IsStarted { get; private set; }

    public KernelKeyRecorder()
    {
        KeyChannel = Channel.CreateUnbounded<string>();
        KeyChannelReader = KeyChannel.Reader;
        KeyChannelWriter = KeyChannel.Writer;

        KeyboardHookProc = KeyboardProc;
    }

    public void Start()
    {
        if (IsStarted) throw new InvalidOperationException("recording already in progress");

        KeyPlaybackBuffer = new Dictionary<long, List<KeyEvent>>();
        SW = new Stopwatch();

        using (var process = Process.GetCurrentProcess())
        using (var mainModule = process.MainModule)
        {
            KeyboardHook = NativeMethods
                .SetWindowsHookEx(
                    KeyboardHookId,
                    KeyboardHookProc,
                    NativeMethods.GetModuleHandle(mainModule.ModuleName),
                    0);
        }

        SW.Start();
        IsStarted = true;
    }

    public void Clear()
    {
        if (IsStarted)
        {
            Stop();
        }

        KeyPlaybackBuffer = new Dictionary<long, List<KeyEvent>>();
        SW = new Stopwatch();
    }

    public void Stop()
    {
        if (IsStarted)
        {
            SW.Stop();
            NativeMethods.UnhookWindowsHookEx(KeyboardHook);
            IsStarted = false;
        }
    }

    private const string KeyEventTemplate = "{0}\r\nActionEvent: {1}\r\nTimestamp: {2} μs";
    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var time = SW.ElapsedMicroseconds();
            var key = Marshal.ReadInt32(lParam);
            var action = wParam.ToInt32();
            (var actionString, var flag) = Helpers.GetActivityFlags(action);

            // Send to buffer for Async writes to UI.
            KeyChannelWriter.TryWrite(string.Format(KeyEventTemplate, key, actionString, time));

            // Allows multiple keypresses on the same frame time.
            if (KeyPlaybackBuffer.ContainsKey(time))
            {
                KeyPlaybackBuffer[time].Add(
                    new KeyEvent
                    {
                        Action = action,
                        ActionString = actionString,
                        Flag = flag,
                        Timestamp = time,
                        Key = key,
                        KeyIntPtr = lParam
                    });

            }
            else
            {
                KeyPlaybackBuffer[time] = new List<KeyEvent>
                {
                    new KeyEvent
                    {
                        Action = action,
                        ActionString = actionString,
                        Flag = flag,
                        Timestamp = time,
                        Key = key,
                        KeyIntPtr = lParam
                    }
                };
            }
        }

        return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }
}
