using Microsoft.IO;
using System;
using System.IO;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace HouseofCat.Recyclable;

public static class RecyclableManager
{
    private static RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

    public static void ConfigureNewStaticManager(Options options = null)
    {
        if (options is null)
        {
            _manager = new RecyclableMemoryStreamManager();
        }
        else
        {
            _manager = new RecyclableMemoryStreamManager(options);
        }
    }

    public static RecyclableMemoryStream GetStream()
    {
        return _manager.GetStream();
    }

    public static RecyclableMemoryStream GetStream(string tag)
    {
        return _manager.GetStream(tag);
    }

    public static RecyclableMemoryStream GetStream(string tag, int desiredSize)
    {
        return _manager.GetStream(tag, desiredSize);
    }

    public static RecyclableMemoryStream GetStream(ReadOnlySpan<byte> buffer)
    {
        return _manager.GetStream(buffer);
    }

    public static RecyclableMemoryStream GetStream(string tag, ReadOnlySpan<byte> buffer)
    {
        return _manager.GetStream(tag, buffer);
    }

    public static RecyclableMemoryStream GetStream(string tag, ReadOnlySpan<byte> buffer, int start, int length)
    {
        return _manager.GetStream(tag, buffer.Slice(start, length));
    }

    public static void ReturnStream(MemoryStream stream)
    {
        stream.Dispose();
    }
}
