using System;
using System.Threading;

namespace Vipps.Services
{
    public interface IVippsOrderSynchronizer : IDisposable
    {
        void Remove(string orderId);
        void Remove(string orderId, bool dispose);
        bool TryRelease(string orderId);

        SemaphoreSlim Get(string orderId);
        string GetInstanceId();
    }
}
