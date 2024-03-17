using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HouseofCat.Gremlins;

public static class ExceptionHelpers
{
    private static readonly Random _random = new Random();

    public static Task ThrowsRandomSystemExceptionAsync()
    {
        return _random.Next(0, 25) switch
        {
            0 => throw new Exception(),
            1 => throw new SystemException(),
            2 => throw new IndexOutOfRangeException(),
            3 => throw new NullReferenceException(),
            4 => throw new AccessViolationException(),
            5 => throw new InvalidOperationException(),
            6 => throw new ArgumentException(),
            7 => throw new ArgumentNullException(),
            8 => throw new ArgumentOutOfRangeException(),
            9 => throw new InvalidCastException(),
            10 => throw new InvalidProgramException(),
            11 => throw new InvalidTimeZoneException(),
            12 => throw new AggregateException(new Exception()),
            13 => throw new AggregateException(new Exception[] { new Exception(), new SystemException() }),
            14 => throw new AggregateException(),
            15 => throw new ExternalException(),
            16 => throw new COMException(),
            17 => throw new SEHException(),
            18 => throw new OutOfMemoryException(),
            19 => throw new BadImageFormatException(),
            20 => throw new DivideByZeroException(),
            21 => throw new DllNotFoundException(),
            22 => throw new DuplicateWaitObjectException(),
            23 => throw new ApplicationException(),
            24 => throw new ArithmeticException(),
            25 => throw new InvalidProgramException(),
            _ => Task.CompletedTask,
        };
    }

    public static Task ThrowsRandomNetworkExceptionAsync()
    {
        return _random.Next(0, 12) switch
        {
            0 => throw new Exception(),
            1 => throw new AccessViolationException(),
            2 => throw new InvalidOperationException(),
            3 => throw new IOException(),
            4 => throw new SocketException(),
            5 => throw new WebException(),
            6 => throw new PingException("Gremlins threw this exception."),
            7 => throw new HttpRequestException(),
            8 => throw new HttpListenerException(),
            10 => throw new TimeoutException(),
            12 => throw new COMException(),
            _ => Task.CompletedTask,
        };
    }
}
