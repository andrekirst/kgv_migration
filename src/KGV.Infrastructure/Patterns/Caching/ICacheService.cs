using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.Caching
{
    /// <summary>
    /// Cache service interface for Cache-Aside pattern implementation
    /// Provides unified caching abstraction with Redis backend
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get cached value by key
        /// </summary>
        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Set cache value with specified TTL
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get or set cache value using factory method
        /// </summary>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove cached value by key
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove multiple cached values by pattern
        /// </summary>
        Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if key exists in cache
        /// </summary>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get multiple cached values by keys
        /// </summary>
        Task<IDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Set multiple cached values
        /// </summary>
        Task SetManyAsync<T>(IDictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increment numeric value atomically
        /// </summary>
        Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get cache statistics
        /// </summary>
        Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Test cache connectivity
        /// </summary>
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Typed cache service for specific entity caching
    /// </summary>
    public interface ITypedCacheService<T>
    {
        Task<T> GetAsync(string key, CancellationToken cancellationToken = default);
        Task SetAsync(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task<T> GetOrSetAsync(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cache configuration settings for different entity types
    /// </summary>
    public class CacheSettings
    {
        public string KeyPrefix { get; set; }
        public TimeSpan DefaultExpiration { get; set; }
        public bool EnableCompression { get; set; }
        public CacheSerializationMethod SerializationMethod { get; set; }
        public Dictionary<string, TimeSpan> CustomExpirations { get; set; } = new();
    }

    public enum CacheSerializationMethod
    {
        Json,
        MessagePack,
        Protobuf
    }

    /// <summary>
    /// Cache statistics for monitoring
    /// </summary>
    public class CacheStatistics
    {
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public double HitRatio => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
        public long KeyCount { get; set; }
        public long MemoryUsage { get; set; }
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }

    /// <summary>
    /// Cache key builder for consistent key generation
    /// </summary>
    public interface ICacheKeyBuilder
    {
        string BuildKey<T>(string identifier, params object[] parameters);
        string BuildKey(string entityType, string identifier, params object[] parameters);
        string BuildPatternKey(string entityType, string pattern = "*");
        string BuildUserKey(string userId, string entityType, string identifier);
        string BuildListKey(string entityType, params object[] filters);
        string BuildStatsKey(string entityType, string statsType);
    }

    /// <summary>
    /// Cache invalidation strategy interface
    /// </summary>
    public interface ICacheInvalidationStrategy
    {
        Task InvalidateAsync(string entityType, string identifier, CancellationToken cancellationToken = default);
        Task InvalidateRelatedAsync(string entityType, string identifier, CancellationToken cancellationToken = default);
        Task InvalidateUserCacheAsync(string userId, CancellationToken cancellationToken = default);
        Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cache warming interface for proactive cache population
    /// </summary>
    public interface ICacheWarmupService
    {
        Task WarmupAsync(CancellationToken cancellationToken = default);
        Task WarmupEntityAsync<T>(IEnumerable<string> identifiers, CancellationToken cancellationToken = default);
        Task WarmupUserDataAsync(string userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cache events for monitoring and debugging
    /// </summary>
    public interface ICacheEvents
    {
        event Action<CacheHitEvent> CacheHit;
        event Action<CacheMissEvent> CacheMiss;
        event Action<CacheSetEvent> CacheSet;
        event Action<CacheRemovedEvent> CacheRemoved;
        event Action<CacheErrorEvent> CacheError;
    }

    public class CacheHitEvent
    {
        public string Key { get; set; }
        public string EntityType { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CacheMissEvent
    {
        public string Key { get; set; }
        public string EntityType { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CacheSetEvent
    {
        public string Key { get; set; }
        public string EntityType { get; set; }
        public TimeSpan? Expiration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CacheRemovedEvent
    {
        public string Key { get; set; }
        public string Pattern { get; set; }
        public int Count { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CacheErrorEvent
    {
        public string Operation { get; set; }
        public string Key { get; set; }
        public Exception Exception { get; set; }
        public DateTime Timestamp { get; set; }
    }
}