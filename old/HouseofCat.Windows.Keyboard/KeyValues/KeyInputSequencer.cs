using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static HouseofCat.Windows.InputStructs;

namespace HouseofCat.Windows;

public static class KeyInputSequencer
{
    private const string KeySequenceSegmentFormat = "Key: {0}\r\nActionEvent: {1}\r\nOriginal Time: {2} μs";
    public static List<KeyInputSequence> BuildSequence(Dictionary<long, List<KeyEvent>> keyPlaybackBuffer)
    {
        var sequences = new List<KeyInputSequence>();

        foreach (var kvp in keyPlaybackBuffer)
        {
            var sequence = new KeyInputSequence();
            var inputs = new List<INPUT>();

            sequence.FrameTimestamp = kvp.Key;

            // Get each action for this Frame/Timestamp (most of the time it will be a single action - but could be multiples).
            foreach (var key in kvp.Value)
            {
                inputs.Add(ConvertKeyToInput(key.Key, key.Flag));
                sequence.KeySequence += string.Format(KeySequenceSegmentFormat, key.Key, key.Action, key.Timestamp);
            }

            sequence.InputSequence = inputs.ToArray();
            sequences.Add(sequence);
        }

        return sequences;
    }

    private const int KEYBOARDEVENT = 1;
    private const int SCANVALUE = 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static INPUT ConvertKeyToInput(object key, uint flags)
    {
        return new INPUT
        {
            Type = KEYBOARDEVENT,
            Data =
            {
                Keyboard = new KEYBDINPUT
                {
                    KeyCode = Convert.ToUInt16(key),
                    Scan = SCANVALUE,
                    Flags = flags,
                    Time = 0,
                    ExtraInfo = IntPtr.Zero
                }
            }
        };
    }
}
