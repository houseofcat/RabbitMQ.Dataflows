using System;

namespace HouseofCat.Workflows
{
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
                            _disposables[i].Dispose();
                        }
                        catch { /* Swallow */}
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
