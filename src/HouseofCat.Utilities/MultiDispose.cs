using System;

namespace HouseofCat.Utilities;

public class MultiDispose : IDisposable
{
    private bool disposedValue;

    private readonly IDisposable[] _disposables;

    public MultiDispose(params IDisposable[] disposables)
    {
        _disposables = disposables;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                for (int i = 0; i < _disposables.Length; i++)
                {
                    try
                    {
                        _disposables[i]?.Dispose();
                    }
                    catch { /* Swallow */ }
                }
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
