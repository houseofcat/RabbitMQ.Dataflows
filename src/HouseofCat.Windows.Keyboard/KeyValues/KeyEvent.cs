using System;

namespace HouseofCat.Windows;

public class KeyEvent
{
    public int Action { get; set; }
    public string ActionString { get; set; }
    public long Timestamp { get; set; }
    public object Key { get; set; }
    public uint Flag { get; set; }
    public IntPtr KeyIntPtr { get; set; }
}
