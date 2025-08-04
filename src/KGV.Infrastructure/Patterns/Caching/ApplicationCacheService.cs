using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KGV.Infrastructure.Patterns.AntiCorruption.ModernModels;
using KGV.Infrastructure.Patterns.CQRS.Queries;

namespace KGV.Infrastructure.Patterns.Caching
{
    /// <summary>
    /// Application-specific cache service implementing Cache-Aside pattern
    /// Provides high-level caching operations for KGV Application entities
    /// </summary>
    public class ApplicationCacheService : ITypedCacheService<Application>
    {
        private readonly ICacheService _cacheService;
        private readonly ICacheKeyBuilder _keyBuilder;
        private readonly ILogger<ApplicationCacheService> _logger;
        private readonly ICacheInvalidationStrategy _invalidationStrategy;

        public ApplicationCacheService(
            ICacheService cacheService,
            ICacheKeyBuilder keyBuilder,
            ILogger<ApplicationCacheService> logger,
            ICacheInvalidationStrategy invalidationStrategy)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _keyBuilder = keyBuilder ?? throw new ArgumentNullException(nameof(keyBuilder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _invalidationStrategy = invalidationStrategy ?? throw new ArgumentNullException(nameof(invalidationStrategy));
        }

        public async Task<Application> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            var cacheKey = _keyBuilder.BuildKey<Application>(key);
            return await _cacheService.GetAsync<Application>(cacheKey, cancellationToken);
        }

        public async Task SetAsync(string key, Application value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            var cacheKey = _keyBuilder.BuildKey<Application>(key);
            await _cacheService.SetAsync(cacheKey, value, expiration, cancellationToken);
        }

        public async Task<Application> GetOrSetAsync(string key, Func<Task<Application>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            var cacheKey = _keyBuilder.BuildKey<Application>(key);
            return await _cacheService.GetOrSetAsync(cacheKey, factory, expiration, cancellationToken);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            var cacheKey = _keyBuilder.BuildKey<Application>(key);
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);
        }

        public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            var cachePattern = _keyBuilder.BuildPatternKey(KgvCacheKeys.APPLICATION, pattern);
            await _cacheService.RemoveByPatternAsync(cachePattern, cancellationToken);
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            var cacheKey = _keyBuilder.BuildKey<Application>(key);
            return await _cacheService.ExistsAsync(cacheKey, cancellationToken);
        }

        // KGV-specific caching methods

        /// <summary>
        /// Cache application by ID with optimized expiration
        /// </summary>
        public async Task<Application> GetApplicationByIdAsync(Guid applicationId, Func<Task<Application>> factory = null, CancellationToken cancellationToken = default)
        {
            var key = applicationId.ToString();
            
            if (factory != null)
            {
                return await GetOrSetAsync(key, factory, TimeSpan.FromMinutes(30), cancellationToken);
            }
            
            return await GetAsync(key, cancellationToken);
        }

        /// <summary>
        /// Cache application by file reference
        /// </summary>
        public async Task<Application> GetApplicationByFileReferenceAsync(string fileReference, Func<Task<Application>> factory = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileReference))
                return null;

            var key = _keyBuilder.BuildKey(KgvCacheKeys.APPLICATION, "file_ref", fileReference);
            
            if (factory != null)
            {
                return await _cacheService.GetOrSetAsync(key, factory, TimeSpan.FromMinutes(30), cancellationToken);
            }
            
            return await _cacheService.GetAsync<Application>(key, cancellationToken);
        }

        /// <summary>
        /// Cache applications by status with pagination
        /// </summary>
        public async Task<PagedResultDto<ApplicationSummaryDto>> GetApplicationsByStatusAsync(
            ApplicationStatus status,
            int page,
            int pageSize,
            Func<Task<PagedResultDto<ApplicationSummaryDto>>> factory = null,
            CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildListKey(KgvCacheKeys.APPLICATION, "status", status.ToString(), "page", page, "size", pageSize);
            
            if (factory != null)
            {
                return await _cacheService.GetOrSetAsync(key, factory, TimeSpan.FromMinutes(10), cancellationToken);
            }
            
            return await _cacheService.GetAsync<PagedResultDto<ApplicationSummaryDto>>(key, cancellationToken);
        }

        /// <summary>
        /// Cache application search results
        /// </summary>
        public async Task<PagedResultDto<ApplicationSummaryDto>> GetSearchResultsAsync(
            SearchApplicationsQuery query,
            Func<Task<PagedResultDto<ApplicationSummaryDto>>> factory = null,
            CancellationToken cancellationToken = default)
        {
            // Create cache key based on search parameters
            var searchHash = CreateSearchHash(query);
            var key = _keyBuilder.BuildListKey(KgvCacheKeys.APPLICATION, "search", searchHash);
            
            if (factory != null)
            {
                // Search results have shorter TTL due to potential data changes
                return await _cacheService.GetOrSetAsync(key, factory, TimeSpan.FromMinutes(5), cancellationToken);
            }
            
            return await _cacheService.GetAsync<PagedResultDto<ApplicationSummaryDto>>(key, cancellationToken);
        }

        /// <summary>
        /// Cache application statistics
        /// </summary>
        public async Task<ApplicationStatisticsDto> GetStatisticsAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string district,
            Func<Task<ApplicationStatisticsDto>> factory = null,
            CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildStatsKey(KgvCacheKeys.APPLICATION, "overview");
            
            // Add date range and district to key if specified
            if (fromDate.HasValue || toDate.HasValue || !string.IsNullOrEmpty(district))
            {
                var parameters = new List<object>();
                if (fromDate.HasValue) parameters.Add($"from_{fromDate.Value:yyyyMMdd}");
                if (toDate.HasValue) parameters.Add($"to_{toDate.Value:yyyyMMdd}");
                if (!string.IsNullOrEmpty(district)) parameters.Add($"district_{district}");
                
                key = _keyBuilder.BuildKey(KgvCacheKeys.STATISTICS, "application_overview", parameters.ToArray());
            }
            
            if (factory != null)
            {
                // Statistics cached for shorter time due to frequent updates
                return await _cacheService.GetOrSetAsync(key, factory, TimeSpan.FromMinutes(5), cancellationToken);
            }
            
            return await _cacheService.GetAsync<ApplicationStatisticsDto>(key, cancellationToken);
        }

        /// <summary>
        /// Cache recent applications for dashboard
        /// </summary>
        public async Task<IEnumerable<ApplicationSummaryDto>> GetRecentApplicationsAsync(
            int count,
            Func<Task<IEnumerable<ApplicationSummaryDto>>> factory = null,
            CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildListKey(KgvCacheKeys.APPLICATION, KgvCacheKeys.Lists.RECENT_APPLICATIONS, count);
            
            if (factory != null)
            {
                // Recent applications cached for short time
                return await _cacheService.GetOrSetAsync(key, factory, TimeSpan.FromMinutes(2), cancellationToken);
            }
            
            return await _cacheService.GetAsync<IEnumerable<ApplicationSummaryDto>>(key, cancellationToken);
        }

        /// <summary>
        /// Cache applications by user (for user-specific views)
        /// </summary>
        public async Task<IEnumerable<ApplicationSummaryDto>> GetUserApplicationsAsync(
            string userId,
            Func<Task<IEnumerable<ApplicationSummaryDto>>> factory = null,
            CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildUserKey(userId, KgvCacheKeys.APPLICATION, "list");
            
            if (factory != null)
            {
                return await _cacheService.GetOrSetAsync(key, factory, TimeSpan.FromMinutes(15), cancellationToken);
            }
            
            return await _cacheService.GetAsync<IEnumerable<ApplicationSummaryDto>>(key, cancellationToken);
        }

        /// <summary>
        /// Invalidate application cache when data changes
        /// </summary>
        public async Task InvalidateApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Invalidating cache for application {ApplicationId}", applicationId);

            await _invalidationStrategy.InvalidateAsync(KgvCacheKeys.APPLICATION, applicationId.ToString(), cancellationToken);
            await _invalidationStrategy.InvalidateRelatedAsync(KgvCacheKeys.APPLICATION, applicationId.ToString(), cancellationToken);
        }

        /// <summary>
        /// Invalidate all application caches (use sparingly)
        /// </summary>
        public async Task InvalidateAllApplicationCacheAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Invalidating ALL application cache entries");

            await RemoveByPatternAsync("*", cancellationToken);
            
            // Also invalidate related caches
            await _cacheService.RemoveByPatternAsync(
                _keyBuilder.BuildPatternKey(KgvCacheKeys.STATISTICS, "*"), 
                cancellationToken);
        }

        /// <summary>
        /// Warm up cache with frequently accessed applications
        /// </summary>
        public async Task WarmUpCacheAsync(IEnumerable<Guid> applicationIds, Func<Guid, Task<Application>> factory, CancellationToken cancellationToken = default)
        {
            if (factory == null || applicationIds == null)
                return;

            _logger.LogInformation("Warming up application cache for {Count} applications", applicationIds.Count());

            var tasks = applicationIds.Select(async id =>
            {
                try
                {
                    await GetApplicationByIdAsync(id, () => factory(id), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm up cache for application {ApplicationId}", id);
                }
            });

            await Task.WhenAll(tasks);

            _logger.LogInformation("Application cache warm-up completed");
        }

        private string CreateSearchHash(SearchApplicationsQuery query)
        {
            // Create a consistent hash for search parameters
            var parameters = new[]
            {
                query.SearchTerm ?? "",
                query.Status ?? "",
                query.District ?? "",
                query.CreatedFrom?.ToString("yyyyMMdd") ?? "",
                query.CreatedTo?.ToString("yyyyMMdd") ?? "",
                query.UpdatedFrom?.ToString("yyyyMMdd") ?? "",
                query.UpdatedTo?.ToString("yyyyMMdd") ?? "",
                query.Page.ToString(),
                query.PageSize.ToString(),
                query.SortBy ?? "",
                query.SortDescending.ToString()
            };

            var combined = string.Join("|", parameters);
            
            // Create a short hash for the cache key
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(hashBytes).Replace("+", "-").Replace("/", "_").Replace("=", "").Substring(0, 16);
        }
    }

    /// <summary>
    /// Cache invalidation strategy for applications
    /// </summary>
    public class ApplicationCacheInvalidationStrategy : ICacheInvalidationStrategy
    {
        private readonly ICacheService _cacheService;
        private readonly ICacheKeyBuilder _keyBuilder;
        private readonly ILogger<ApplicationCacheInvalidationStrategy> _logger;

        public ApplicationCacheInvalidationStrategy(
            ICacheService cacheService,
            ICacheKeyBuilder keyBuilder,
            ILogger<ApplicationCacheInvalidationStrategy> logger)
        {
            _cacheService = cacheService;
            _keyBuilder = keyBuilder;
            _logger = logger;
        }

        public async Task InvalidateAsync(string entityType, string identifier, CancellationToken cancellationToken = default)
        {
            var key = _keyBuilder.BuildKey(entityType, identifier);
            await _cacheService.RemoveAsync(key, cancellationToken);
            
            _logger.LogDebug("Invalidated cache for {EntityType} {Identifier}", entityType, identifier);
        }

        public async Task InvalidateRelatedAsync(string entityType, string identifier, CancellationToken cancellationToken = default)
        {
            if (entityType == KgvCacheKeys.APPLICATION)
            {
                // Invalidate related caches when an application changes
                var tasks = new[]
                {
                    // Invalidate list caches
                    _cacheService.RemoveByPatternAsync(_keyBuilder.BuildPatternKey(entityType, "list:*"), cancellationToken),
                    
                    // Invalidate search caches
                    _cacheService.RemoveByPatternAsync(_keyBuilder.BuildPatternKey(entityType, "search:*"), cancellationToken),
                    
                    // Invalidate statistics
                    _cacheService.RemoveByPatternAsync(_keyBuilder.BuildPatternKey(KgvCacheKeys.STATISTICS, "*"), cancellationToken),
                    
                    // Invalidate recent applications
                    _cacheService.RemoveByPatternAsync(_keyBuilder.BuildListKey(entityType, KgvCacheKeys.Lists.RECENT_APPLICATIONS, "*"), cancellationToken)
                };

                await Task.WhenAll(tasks);
                
                _logger.LogDebug("Invalidated related caches for {EntityType} {Identifier}", entityType, identifier);
            }
        }

        public async Task InvalidateUserCacheAsync(string userId, CancellationToken cancellationToken = default)
        {
            var pattern = _keyBuilder.BuildPatternKey("*", $"*:user:{userId}");
            await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
            
            _logger.LogDebug("Invalidated user cache for user {UserId}", userId);
        }

        public async Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
            
            _logger.LogDebug("Invalidated cache pattern {Pattern}", pattern);
        }
    }
}