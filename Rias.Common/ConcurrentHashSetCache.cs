using System.Collections.Concurrent;

namespace Rias.Common;

/// <summary>
/// A concurrent hash set with expiring items.
/// </summary>
public class ConcurrentHashSetCache<TKey> : IDisposable where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, ExpiryKey> _dictionary = new();
    private readonly Timer _cleanupTimer;

    public ConcurrentHashSetCache()
    {
        _cleanupTimer = new Timer(CleanupTimerCallback, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    private void CleanupTimerCallback(object? state)
    {
        lock (_cleanupTimer)
        {
            foreach (var (key, expiryKey) in _dictionary)
            {
                if (expiryKey.IsExpired())
                    _dictionary.TryRemove(key, out _);
            }
        }
    }

    public void AddOrUpdate(TKey key, TimeSpan expiry)
    {
        lock (_cleanupTimer)
        {
            var value = new ExpiryKey(expiry);
            _dictionary.AddOrUpdate(key, value, (_, _) => value);
        }
    }

    public bool Contains(TKey key)
    {
        lock (_cleanupTimer)
        {
            if (!_dictionary.TryGetValue(key, out var expiryKey))
                return false;

            if (expiryKey.IsExpired())
            {
                _dictionary.Remove(key, out _);
                return false;
            }

            return true;
        }
    }

    private class ExpiryKey
    {
        private readonly long _expiryTicks;

        public ExpiryKey(TimeSpan expiry)
        {
            _expiryTicks = Environment.TickCount64 + (long) expiry.TotalMilliseconds;
        }

        public bool IsExpired()
        {
            return Environment.TickCount64 > _expiryTicks;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            _cleanupTimer.Dispose();
    }
}