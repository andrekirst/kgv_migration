using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.Caching
{
    /// <summary>
    /// Redis implementation of Cache-Aside pattern
    /// Provides high-performance distributed caching with monitoring and resilience
    /// </summary>
    public class RedisCacheService : ICacheService, ICacheEvents, IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly RedisCacheOptions _options;
        private readonly ICacheKeyBuilder _keyBuilder;
        private readonly SemaphoreSlim _connectionSemaphore;
        private readonly Dictionary<string, CacheConfiguration> _configurations;

        // Events
        public event Action<CacheHitEvent> CacheHit;
        public event Action<CacheMissEvent> CacheMiss;
        public event Action<CacheSetEvent> CacheSet;
        public event Action<CacheRemovedEvent> CacheRemoved;
        public event Action<CacheErrorEvent> CacheError;

        public RedisCacheService(
            IConnectionMultiplexer redis,
            ILogger<RedisCacheService> logger,
            IOptions<RedisCacheOptions> options,
            ICacheKeyBuilder keyBuilder)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _keyBuilder = keyBuilder ?? throw new ArgumentNullException(nameof(keyBuilder));

            _database = _redis.GetDatabase(_options.DatabaseNumber);
            _connectionSemaphore = new SemaphoreSlim(10, 10); // Limit concurrent operations
            _configurations = LoadCacheConfigurations();

            _logger.LogInformation("Redis cache service initialized for database {DatabaseNumber}", _options.DatabaseNumber);
        }

        public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                await _connectionSemaphore.WaitAsync(cancellationToken);

                var cachedValue = await _database.StringGetAsync(key);
                
                stopwatch.Stop();

                if (cachedValue.HasValue)
                {
                    var value = DeserializeValue<T>(cachedValue);
                    
                    _logger.LogDebug("Cache hit for key {Key} in {ElapsedMs}ms", key, stopwatch.ElapsedMilliseconds);
                    
                    CacheHit?.Invoke(new CacheHitEvent
                    {
                        Key = key,
                        EntityType = typeof(T).Name,
                        Duration = stopwatch.Elapsed,
                        Timestamp = DateTime.UtcNow
                    });

                    return value;
                }
                else
                {
                    _logger.LogDebug("Cache miss for key {Key} in {ElapsedMs}ms", key, stopwatch.ElapsedMilliseconds);
                    
                    CacheMiss?.Invoke(new CacheMissEvent
                    {
                        Key = key,
                        EntityType = typeof(T).Name,
                        Duration = stopwatch.Elapsed,
                        Timestamp = DateTime.UtcNow
                    });

                    return default(T);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(ex, "Cache get failed for key {Key}", key);
                
                CacheError?.Invoke(new CacheErrorEvent
                {
                    Operation = "Get",
                    Key = key,
                    Exception = ex,
                    Timestamp = DateTime.UtcNow
                });

                // Return default on cache failure (fail-safe behavior)
                return default(T);
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            if (value == null)
                return; // Don't cache null values

            try
            {
                await _connectionSemaphore.WaitAsync(cancellationToken);

                var serializedValue = SerializeValue(value);
                var effectiveExpiration = expiration ?? GetDefaultExpiration<T>();

                var success = await _database.StringSetAsync(key, serializedValue, effectiveExpiration);

                if (success)
                {
                    _logger.LogDebug("Cache set for key {Key} with expiration {Expiration}", key, effectiveExpiration);
                    
                    CacheSet?.Invoke(new CacheSetEvent
                    {
                        Key = key,
                        EntityType = typeof(T).Name,
                        Expiration = effectiveExpiration,
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to set cache for key {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache set failed for key {Key}", key);
                
                CacheError?.Invoke(new CacheErrorEvent
                {
                    Operation = "Set",
                    Key = key,
                    Exception = ex,
                    Timestamp = DateTime.UtcNow
                });

                // Don't throw on cache set failure (fail-safe behavior)
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            // First try to get from cache
            var cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue != null && !cachedValue.Equals(default(T)))
            {
                return cachedValue;
            }

            // Cache miss - get from factory
            try
            {
                var value = await factory();
                
                if (value != null && !value.Equals(default(T)))
                {
                    // Cache the result asynchronously (fire and forget)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await SetAsync(key, value, expiration, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to cache value for key {Key} in background", key);
                        }
                    }, CancellationToken.None);
                }

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Factory method failed for cache key {Key}", key);
                throw;
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                await _connectionSemaphore.WaitAsync(cancellationToken);

                var removed = await _database.KeyDeleteAsync(key);

                if (removed)
                {
                    _logger.LogDebug("Cache removed for key {Key}", key);
                    
                    CacheRemoved?.Invoke(new CacheRemovedEvent
                    {
                        Key = key,
                        Count = 1,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache remove failed for key {Key}", key);
                
                CacheError?.Invoke(new CacheErrorEvent
                {
                    Operation = "Remove",
                    Key = key,
                    Exception = ex,
                    Timestamp = DateTime.UtcNow
                });
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(pattern))
                return;

            try
            {
                await _connectionSemaphore.WaitAsync(cancellationToken);

                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(_database.Database, pattern).ToArray();

                if (keys.Length > 0)
                {
                    var deletedCount = await _database.KeyDeleteAsync(keys);
                    
                    _logger.LogDebug("Cache removed {Count} keys matching pattern {Pattern}", deletedCount, pattern);
                    
                    CacheRemoved?.Invoke(new CacheRemovedEvent
                    {
                        Pattern = pattern,
                        Count = deletedCount,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache remove by pattern failed for pattern {Pattern}", pattern);
                
                CacheError?.Invoke(new CacheErrorEvent
                {
                    Operation = "RemoveByPattern",
                    Key = pattern,
                    Exception = ex,
                    Timestamp = DateTime.UtcNow
                });
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            try
            {
                await _connectionSemaphore.WaitAsync(cancellationToken);
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache exists check failed for key {Key}", key);
                return false;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task<IDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            var keyArray = keys?.ToArray();
            if (keyArray == null || keyArray.Length == 0)
                return new Dictionary<string, T>();

            try
            {
                await _connectionSemaphore.WaitAsync(cancellationToken);

                var redisKeys = keyArray.Select(k => (RedisKey)k).ToArray();
                var values = await _database.StringGetAsync(redisKeys);

                var result = new Dictionary<string, T>();
                
                for (int i = 0; i < keyArray.Length; i++)
                {
                    if (values[i].HasValue)
                    {
                        result[keyArray[i]] = DeserializeValue<T>(values[i]);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache get many failed for {Count} keys", keyArray.Length);
                return new Dictionary<string, T>();
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task SetManyAsync<T>(IDictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            if (keyValuePairs == null || !keyValuePairs.Any())
                return;

            try
            {
                await _connectionSemaphore.WaitAsync(cancellationToken);

                var batch = _database.CreateBatch();
                var tasks = new List<Task>();

                var effectiveExpiration = expiration ?? GetDefaultExpiration<T>();

                foreach (var kvp in keyValuePairs)
                {
                    if (kvp.Value != null)
                    {
                        var serializedValue = SerializeValue(kvp.Value);
                        tasks.Add(batch.StringSetAsync(kvp.Key, serializedValue, effectiveExpiration));
                    }
                }

                batch.Execute();
                await Task.WhenAll(tasks);

                _logger.LogDebug("Cache set many completed for {Count} keys", keyValuePairs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache set many failed for {Count} keys", keyValuePairs.Count);
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            try
            {
                await _connectionSemaphore.WaitAsync(cancellationToken);

                var result = await _database.StringIncrementAsync(key, value);

                if (expiration.HasValue)
                {
                    await _database.KeyExpireAsync(key, expiration.Value);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache increment failed for key {Key}", key);
                throw;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _connectionSemaphore.WaitAsync(cancellationToken);

                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var info = await server.InfoAsync();

                var stats = new CacheStatistics
                {
                    LastUpdated = DateTime.UtcNow
                };

                // Parse Redis INFO response
                foreach (var section in info)
                {
                    foreach (var item in section)
                    {
                        switch (item.Key.ToLower())
                        {
                            case "keyspace_hits":
                                if (long.TryParse(item.Value, out var hits))
                                    stats.HitCount = hits;
                                break;
                            case "keyspace_misses":
                                if (long.TryParse(item.Value, out var misses))
                                    stats.MissCount = misses;
                                break;
                            case "used_memory":
                                if (long.TryParse(item.Value, out var memory))
                                    stats.MemoryUsage = memory;
                                break;
                        }
                    }
                }

                // Get key count for current database
                var dbInfo = await server.InfoAsync("keyspace");
                var dbSection = dbInfo.FirstOrDefault();
                if (dbSection != null)
                {
                    var dbKey = $"db{_database.Database}";
                    var dbItem = dbSection.FirstOrDefault(x => x.Key == dbKey);
                    if (!string.IsNullOrEmpty(dbItem.Value))
                    {
                        // Parse "keys=123,expires=45,avg_ttl=678"
                        var keysPart = dbItem.Value.Split(',').FirstOrDefault(x => x.StartsWith("keys="));
                        if (keysPart != null && long.TryParse(keysPart.Substring(5), out var keyCount))
                        {
                            stats.KeyCount = keyCount;
                        }
                    }
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache statistics");
                return new CacheStatistics { LastUpdated = DateTime.UtcNow };
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _connectionSemaphore.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

                var testKey = $"health_check_{Guid.NewGuid()}";
                var testValue = DateTime.UtcNow.ToString();

                // Test set and get
                await _database.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(10));
                var retrieved = await _database.StringGetAsync(testKey);
                await _database.KeyDeleteAsync(testKey);

                return retrieved.HasValue && retrieved == testValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache health check failed");
                return false;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        private T DeserializeValue<T>(RedisValue value)
        {
            if (!value.HasValue)
                return default(T);

            try
            {
                var stringValue = value.ToString();
                
                if (typeof(T) == typeof(string))
                    return (T)(object)stringValue;

                return JsonSerializer.Deserialize<T>(stringValue, _options.JsonSerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize cached value for type {Type}", typeof(T).Name);
                return default(T);
            }
        }

        private RedisValue SerializeValue<T>(T value)
        {
            if (value == null)
                return RedisValue.Null;

            try
            {
                if (typeof(T) == typeof(string))
                    return value.ToString();

                return JsonSerializer.Serialize(value, _options.JsonSerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize value for type {Type}", typeof(T).Name);
                throw;
            }
        }

        private TimeSpan GetDefaultExpiration<T>()
        {
            var typeName = typeof(T).Name.ToLower();
            
            if (_configurations.ContainsKey(typeName))
                return _configurations[typeName].DefaultExpiration;

            return _options.DefaultExpiration;
        }

        private Dictionary<string, CacheConfiguration> LoadCacheConfigurations()
        {
            var configurations = new Dictionary<string, CacheConfiguration>();

            // Configure entity-specific cache settings
            configurations["application"] = new CacheConfiguration
            {
                KeyPrefix = "app:",
                DefaultExpiration = TimeSpan.FromMinutes(30),
                EnableCompression = false,
                SerializationMethod = CacheSerializationMethod.Json
            };

            configurations["person"] = new CacheConfiguration
            {
                KeyPrefix = "person:",
                DefaultExpiration = TimeSpan.FromHours(1),
                EnableCompression = false,
                SerializationMethod = CacheSerializationMethod.Json
            };

            configurations["district"] = new CacheConfiguration
            {
                KeyPrefix = "district:",
                DefaultExpiration = TimeSpan.FromHours(24),
                EnableCompression = false,
                SerializationMethod = CacheSerializationMethod.Json
            };

            configurations["statistics"] = new CacheConfiguration
            {
                KeyPrefix = "stats:",
                DefaultExpiration = TimeSpan.FromMinutes(5),
                EnableCompression = true,
                SerializationMethod = CacheSerializationMethod.Json
            };

            return configurations;
        }

        public void Dispose()
        {
            _connectionSemaphore?.Dispose();
            _logger.LogInformation("Redis cache service disposed");
        }
    }

    /// <summary>
    /// Redis cache configuration options
    /// </summary>
    public class RedisCacheOptions
    {
        public string ConnectionString { get; set; }
        public int DatabaseNumber { get; set; } = 0;
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(20);
        public string KeyPrefix { get; set; } = "kgv:";
        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        public int MaxConcurrentOperations { get; set; } = 10;
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}