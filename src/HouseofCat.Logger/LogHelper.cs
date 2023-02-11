using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HouseofCat.Logger;

public static class LogHelper
{
    private readonly static object _syncObj = new object();
    private static ILoggerFactory _factory;

    public static ILoggerFactory LoggerFactory
    {
        get
        {
            if (_factory == null)
            {
                lock (_syncObj)
                {
                    _factory ??= new NullLoggerFactory();
                }
            }
            return _factory;
        }
        set { _factory = value ?? new NullLoggerFactory(); }
    }

    public static ILogger<TCategoryName> GetLogger<TCategoryName>()
    {
        return LoggerFactory.CreateLogger<TCategoryName>();
    }

    public static void AddProvider(ILoggerProvider provider)
    {
        LoggerFactory.AddProvider(provider);
    }
}
