using System;
using System.Collections.Generic;
using System.Threading;
using EPiServer.ServiceLocation;
using Vipps.Models;

namespace Vipps.Services
{
    [ServiceConfiguration(typeof(IVippsOrderSynchronizer))]
    public class DefaultVippsOrderSynchronizer : IVippsOrderSynchronizer
    {
        private readonly object _lock;
        private readonly IDictionary<string, SynchronizerData> _index;
        
        private readonly int _semaphoreMaxCount;
        private bool _disposed;

        public DefaultVippsOrderSynchronizer()
        {
            _semaphoreMaxCount = 1;
            _disposed = false;
            _lock = new object();
            _index = new Dictionary<string, SynchronizerData>();
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

        public virtual SemaphoreSlim Get(string key)
        {
            lock (_lock)
            {
                if (_index.TryGetValue(key, out var result))
                {
                    result.Requests++;
                    return result.Semaphore;
                }

                result = Create(key);

                _index.Add(key, result);

                return result.Semaphore;
            }
        }

        public int Requests
        {
            get 
            { 
                var requests = 0;

                foreach (var item in _index)
                {
                    requests += item.Value.Requests;
                }

                return requests;
            }
        }

        public virtual void Remove(string key)
        {
            Remove(key, true);
        }

        public virtual void Remove(string key, bool dispose)
        {
            if (!_index.ContainsKey(key))
                return;

            lock (_lock)
            {
                if (!_index.ContainsKey(key))
                    return;

                var value = _index[key];

                if (dispose)
                    Dispose(value);
                
                _index.Remove(key);
            }
        }

        public bool TryRelease(string key)
        {
            if (!_index.ContainsKey(key))
                return false;

            lock (_lock)
            {
                if (!_index.TryGetValue(key, out var result))
                    return false;

                var disposed = false;

                try
                {
                    result.Semaphore.Release();
                    result.Requests--;
                }
                catch (ObjectDisposedException)
                {
                    result.Requests = 0;
                    disposed = true;
                }

                if (result.Requests > 0)
                    return true;

                _index.Remove(key);

                if (!disposed)
                    Dispose(result);

                return true;
            }
        }

        protected virtual SynchronizerData Create(string key)
        {
            if (key == null) 
                throw new ArgumentNullException(nameof(key));

            return new SynchronizerData
            {
                Semaphore = new SemaphoreSlim(_semaphoreMaxCount, _semaphoreMaxCount),
                Requests = 1
            };
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

        protected virtual void Dispose(SynchronizerData value)
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

        public virtual string GetInstanceId()
        {
            return Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
        }
    }
}
