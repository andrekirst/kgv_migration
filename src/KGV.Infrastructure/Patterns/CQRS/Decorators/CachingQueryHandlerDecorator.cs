using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace KGV.Infrastructure.Patterns.CQRS.Decorators
{
    /// <summary>
    /// Caching decorator for query handlers
    /// Implements read-through caching for query results
    /// </summary>
    /// <typeparam name="TQuery">The type of query to handle</typeparam>
    /// <typeparam name="TResult">The type of result returned</typeparam>
    public class CachingQueryHandlerDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly IQueryHandler<TQuery, TResult> _inner;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingQueryHandlerDecorator<TQuery, TResult>> _logger;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5); // Default cache duration

        public CachingQueryHandlerDecorator(
            IQueryHandler<TQuery, TResult> inner,
            IMemoryCache cache,
            ILogger<CachingQueryHandlerDecorator<TQuery, TResult>> logger)
        {
            _inner = inner;
            _cache = cache;
            _logger = logger;
        }

        public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            var cacheKey = GenerateCacheKey(query);

            if (_cache.TryGetValue(cacheKey, out TResult cachedResult))
            {
                _logger.LogDebug("Cache hit for query {QueryType} with key {CacheKey}", 
                    typeof(TQuery).Name, cacheKey);
                return cachedResult;
            }

            _logger.LogDebug("Cache miss for query {QueryType} with key {CacheKey}", 
                typeof(TQuery).Name, cacheKey);

            var result = await _inner.HandleAsync(query, cancellationToken);

            // Cache the result
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheDuration,
                SlidingExpiration = TimeSpan.FromMinutes(2)
            };

            _cache.Set(cacheKey, result, cacheEntryOptions);

            _logger.LogDebug("Cached result for query {QueryType} with key {CacheKey}", 
                typeof(TQuery).Name, cacheKey);

            return result;
        }

        private string GenerateCacheKey(TQuery query)
        {
            // Generate a cache key based on query type and correlation ID
            // In a real implementation, you might want to use a more sophisticated hashing mechanism
            var queryType = typeof(TQuery).Name;
            var correlationId = query.CorrelationId;
            
            return $"Query_{queryType}_{correlationId}_{query.GetHashCode()}";
        }
    }
}