using System;
using System.Collections.Generic;
using System.Threading;
using EPiServer.ServiceLocation;

namespace Vipps.Services
{
    [ServiceConfiguration(typeof(IVippsOrderSynchronizer))]
    public class DefaultVippsOrderSynchronizer : IVippsOrderSynchronizer
    {   
        private readonly IDictionary<string, SemaphoreSlim> _index;
        private readonly object _lock;
        private readonly int _semaphoreMaxCount;
        private bool _disposed;

        public DefaultVippsOrderSynchronizer()
        {
            _semaphoreMaxCount = 1;
            _disposed = false;
            _lock = new object();
            _index = new Dictionary<string, SemaphoreSlim>();
        }

        ~DefaultVippsOrderSynchronizer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual SemaphoreSlim Get(string orderId)
        {
            lock (_lock)
            {
                if (_index.TryGetValue(orderId, out var result))
                {
                    return result;
                }

                return _index[orderId] = Create(orderId);
            }
        }

        public virtual void Remove(string key)
        {
            if (!_index.ContainsKey(key))
                return;

            lock (_lock)
            {
                if (!_index.ContainsKey(key))
                    return;

                var value = _index[key];

                Dispose(value);
                
                _index.Remove(key);
            }
        }

        protected virtual SemaphoreSlim Create(string key)
        {
            if (key == null) 
                throw new ArgumentNullException(nameof(key));

            return new SemaphoreSlim(_semaphoreMaxCount, _semaphoreMaxCount);
        }

        protected virtual bool Contains(string key)
        {
            if (key == null) 
                throw new ArgumentNullException(nameof(key));

            lock (_lock)
            {
                return _index.ContainsKey(key);
            }
        }
        
        protected virtual void Add(string key)
        {
            if (key == null) 
                throw new ArgumentNullException(nameof(key));

            lock (_lock)
            {
                _index.Add(key, Create(key));
            }
        }

        protected virtual void Dispose(bool disposeManaged)
        {
            if (_disposed) return;

            if (disposeManaged)
            {
                lock (_lock)
                {
                    foreach (var value in _index.Values)
                    {
                        Dispose(value);
                    }

                    _index.Clear();
                }
            }
            
            _disposed = true;
        }

        protected virtual void Dispose(SemaphoreSlim value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                value.Dispose();
            }
            catch (ObjectDisposedException)
            {
                if (_disposed) 
                    throw;
            }
        }
    }
}
