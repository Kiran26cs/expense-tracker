namespace ExpensesBackend.API.Services.Interfaces;

public interface ICacheService
{
    /// <summary>Gets a cached value by key. Returns null if not found or expired.</summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>Stores a value in cache with an optional TTL. Defaults to 5 minutes.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;

    /// <summary>Removes a specific key from the cache.</summary>
    Task RemoveAsync(string key);

    /// <summary>Checks whether a key exists in the cache.</summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Cache-aside pattern: returns cached value if present,
    /// otherwise calls factory, caches the result, and returns it.
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class;
}
