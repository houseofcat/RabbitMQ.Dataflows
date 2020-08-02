using System;
using System.Collections.Generic;
using System.Text;

namespace HouseofCat.Windows
{
    public static class Helpers
    {
        public static (string action, uint flag) GetActivityFlags(int action)
        {
            switch (action)
            {
                case Constants.KeyPointers.WM_KEYUP:
                case Constants.KeyPointers.WM_SYSKEYUP:
                    return (Constants.UP, Constants.KeyPointers.KeyUpFlag);
                case Constants.KeyPointers.WM_KEYDOWN:
                case Constants.KeyPointers.WM_SYSKEYDOWN:
                default:
                    return (Constants.DOWN, Constants.KeyPointers.KeyDownFlag);
            }
        }
    }
}
