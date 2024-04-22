using System;
using System.Collections.Generic;
using System.Text;

namespace HouseofCat.Utilities.Extensions;

public static class ObjectExtensions
{
    private static readonly int _syncBlockSize = 4;
    private static readonly int _methodTableReferenceSize = 4;
    private static readonly int _lengthSize = 4;

    public static long GetByteCount(this object input)
    {
        if (input == null) return 0;
        var type = input.GetType();

        if (_primitiveTypeSizes.TryGetValue(type, out int sizeValue))
        {
            return sizeValue;
        }

        if (_primitiveArrayTypeMultiplier.TryGetValue(type, out int multiplierValue))
        {
            return (multiplierValue * ((Array)input).Length)
                + _syncBlockSize
                + _methodTableReferenceSize
                + _lengthSize;
        }

        if (input is string stringy)
        {
            return Encoding.Unicode.GetByteCount(stringy);
        }

        throw new InvalidOperationException("Can't perform byte count on this reference type.");
    }

    private static readonly Dictionary<Type, int> _primitiveArrayTypeMultiplier = new Dictionary<Type, int>
    {
        { typeof(sbyte[]),    sizeof(sbyte)     },
        { typeof(byte[]),     sizeof(byte)      },
        { typeof(bool[]),     sizeof(bool)      },
        { typeof(short[]),    sizeof(short)     },
        { typeof(ushort[]),   sizeof(ushort)    },
        { typeof(int[]),      sizeof(int)       },
        { typeof(uint[]),     sizeof(uint)      },
        { typeof(long[]),     sizeof(long)      },
        { typeof(ulong[]),    sizeof(ulong)     },
        { typeof(float[]),    sizeof(float)     },
        { typeof(double[]),   sizeof(double)    },
        { typeof(decimal[]),  sizeof(decimal)   }
    };

    private static readonly Dictionary<Type, int> _primitiveTypeSizes = new Dictionary<Type, int>
    {
        { typeof(sbyte),    sizeof(sbyte)   },
        { typeof(byte),     sizeof(byte)    },
        { typeof(bool),     sizeof(bool)    },
        { typeof(short),    sizeof(short)   },
        { typeof(ushort),   sizeof(ushort)  },
        { typeof(char),     sizeof(char)    },
        { typeof(int),      sizeof(int)     },
        { typeof(uint),     sizeof(uint)    },
        { typeof(long),     sizeof(long)    },
        { typeof(ulong),    sizeof(ulong)   },
        { typeof(float),    sizeof(float)   },
        { typeof(double),   sizeof(double)  },
        { typeof(decimal),  sizeof(decimal) }
    };
}
