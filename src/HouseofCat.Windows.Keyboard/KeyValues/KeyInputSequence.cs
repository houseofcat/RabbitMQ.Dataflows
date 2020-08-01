using static HouseofCat.Windows.InputStructs;

namespace HouseofCat.Windows
{
    public class KeyInputSequence
    {
        public long FrameTimestamp { get; set; }
        public INPUT[] InputSequence { get; set; }
        public string KeySequence { get; set; }
    }
}
