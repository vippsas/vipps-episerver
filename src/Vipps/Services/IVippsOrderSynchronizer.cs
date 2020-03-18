using System;
using System.Threading;

namespace Vipps.Services
{
    public interface IVippsOrderSynchronizer : IDisposable
    {
        void Remove(string orderId);
        SemaphoreSlim Get(string orderId);
    }
}
