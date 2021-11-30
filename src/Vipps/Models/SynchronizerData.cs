using System;
using System.Threading;

namespace Vipps.Models
{
    public class SynchronizerData : IDisposable
    {
        private bool _disposed;

        public int Requests { get; set; }
        public SemaphoreSlim Semaphore { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Requests = 0;
                    Semaphore.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
