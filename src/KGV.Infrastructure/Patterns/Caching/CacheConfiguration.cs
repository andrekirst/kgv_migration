using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.Caching
{
    /// <summary>
    /// Configuration for Cache-Aside pattern with Redis
    /// Provides complete caching infrastructure setup for KGV migration system
    /// </summary>
    public static class CacheConfiguration
    {
        public static IServiceCollection AddCacheAsidePattern(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure Redis options
            services.Configure<RedisCacheOptions>(configuration.GetSection("Caching:Redis"));

            // Register Redis connection
            var connectionString = configuration.GetConnectionString("Redis") ?? 
                                 configuration.GetSection("Caching:Redis:ConnectionString").Value;

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Redis connection string is required for caching");
            }

            // Register Redis connection multiplexer as singleton
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<IConnectionMultiplexer>>();
                
                try
                {
                    var configuration = ConfigurationOptions.Parse(connectionString);
                    
                    // Configure connection options
                    configuration.AbortOnConnectFail = false;
                    configuration.ConnectRetry = 3;
                    configuration.ConnectTimeout = 5000;
                    configuration.SyncTimeout = 10000;
                    configuration.AsyncTimeout = 10000;
                    configuration.CommandMap = CommandMap.Create(new HashSet<string>
                    {
                        // Disable potentially dangerous commands in production
                        "FLUSHDB", "FLUSHALL", "KEYS", "CONFIG"
                    }, available: false);

                    var multiplexer = ConnectionMultiplexer.Connect(configuration);
                    
                    // Log connection events
                    multiplexer.ConnectionFailed += (sender, args) =>
                        logger.LogError("Redis connection failed: {EndPoint} - {FailType} - {Exception}", 
                            args.EndPoint, args.FailureType, args.Exception);
                    
                    multiplexer.ConnectionRestored += (sender, args) =>
                        logger.LogInformation("Redis connection restored: {EndPoint}", args.EndPoint);
                    
                    multiplexer.ErrorMessage += (sender, args) =>
                        logger.LogError("Redis error: {EndPoint} - {Message}", args.EndPoint, args.Message);

                    logger.LogInformation("Redis connection established successfully");
                    
                    return multiplexer;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to establish Redis connection: {ConnectionString}", 
                        MaskConnectionString(connectionString));
                    throw;
                }
            });

            // Register core caching services
            services.AddSingleton<ICacheKeyBuilder, CacheKeyBuilder>();
            services.AddSingleton<ICacheService, RedisCacheService>();
            services.AddSingleton<ICacheInvalidationStrategy, ApplicationCacheInvalidationStrategy>();

            // Register typed cache services
            services.AddSingleton<ApplicationCacheService>();
            services.AddSingleton<ITypedCacheService<AntiCorruption.ModernModels.Application>>(
                provider => provider.GetRequiredService<ApplicationCacheService>());

            // Register cache warmup service
            services.AddScoped<ICacheWarmupService, CacheWarmupService>();

            // Register cache health check
            services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                    "redis-cache",
                    provider => new RedisCacheHealthCheck(provider.GetRequiredService<ICacheService>()),
                    Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                    new[] { "cache", "redis", "ready" }));
            });

            // Register cache metrics collector
            services.AddSingleton<ICacheMetricsCollector, CacheMetricsCollector>();

            // Configure cache middleware if enabled
            if (configuration.GetValue<bool>("Caching:EnableMiddleware", false))
            {
                services.AddSingleton<CacheMiddleware>();
            }

            return services;
        }

        private static string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return connectionString;

            // Mask password in connection string for logging
            var parts = connectionString.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Trim().StartsWith("password=", StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = "password=***";
                }
            }
            return string.Join(",", parts);
        }
    }

    /// <summary>
    /// Cache warmup service for proactive cache population
    /// </summary>
    public class CacheWarmupService : ICacheWarmupService
    {
        private readonly ApplicationCacheService _applicationCache;
        private readonly ILogger<CacheWarmupService> _logger;

        public CacheWarmupService(
            ApplicationCacheService applicationCache,
            ILogger<CacheWarmupService> logger)
        {
            _applicationCache = applicationCache;
            _logger = logger;
        }

        public async Task WarmupAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting cache warmup process");

            try
            {
                // Warmup frequently accessed data
                await WarmupFrequentlyAccessedApplicationsAsync(cancellationToken);
                await WarmupApplicationStatisticsAsync(cancellationToken);
                await WarmupRecentApplicationsAsync(cancellationToken);

                _logger.LogInformation("Cache warmup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache warmup failed");
                throw;
            }
        }

        public async Task WarmupEntityAsync<T>(IEnumerable<string> identifiers, CancellationToken cancellationToken = default)
        {
            if (typeof(T) == typeof(AntiCorruption.ModernModels.Application))
            {
                var applicationIds = identifiers.Select(id => Guid.Parse(id));
                await _applicationCache.WarmUpCacheAsync(applicationIds, async id =>
                {
                    // This would normally fetch from repository
                    // For now, return null as placeholder
                    return null;
                }, cancellationToken);
            }
        }

        public async Task WarmupUserDataAsync(string userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Warming up cache for user {UserId}", userId);

            try
            {
                // Warmup user-specific data
                await _applicationCache.GetUserApplicationsAsync(userId, async () =>
                {
                    // This would normally fetch user applications from repository
                    return new List<CQRS.Queries.ApplicationSummaryDto>();
                }, cancellationToken);

                _logger.LogDebug("User cache warmup completed for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "User cache warmup failed for user {UserId}", userId);
            }
        }

        private async Task WarmupFrequentlyAccessedApplicationsAsync(CancellationToken cancellationToken)
        {
            // This would normally fetch frequently accessed application IDs from analytics or repository
            var frequentlyAccessedIds = new List<Guid>(); // Placeholder

            if (frequentlyAccessedIds.Any())
            {
                await _applicationCache.WarmUpCacheAsync(frequentlyAccessedIds, async id =>
                {
                    // Fetch from repository
                    return null; // Placeholder
                }, cancellationToken);
            }
        }

        private async Task WarmupApplicationStatisticsAsync(CancellationToken cancellationToken)
        {
            await _applicationCache.GetStatisticsAsync(null, null, null, async () =>
            {
                // This would normally fetch statistics from repository
                return new CQRS.Queries.ApplicationStatisticsDto
                {
                    TotalApplications = 0,
                    StatusCounts = new Dictionary<string, int>(),
                    ApplicationsByMonth = new Dictionary<string, int>(),
                    AverageProcessingDays = 0,
                    PendingApplications = 0,
                    GeneratedAt = DateTime.UtcNow
                };
            }, cancellationToken);
        }

        private async Task WarmupRecentApplicationsAsync(CancellationToken cancellationToken)
        {
            await _applicationCache.GetRecentApplicationsAsync(10, async () =>
            {
                // This would normally fetch recent applications from repository
                return new List<CQRS.Queries.ApplicationSummaryDto>();
            }, cancellationToken);
        }
    }

    /// <summary>
    /// Redis cache health check
    /// </summary>
    public class RedisCacheHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly ICacheService _cacheService;

        public RedisCacheHealthCheck(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isHealthy = await _cacheService.IsHealthyAsync(cancellationToken);
                
                if (isHealthy)
                {
                    var statistics = await _cacheService.GetStatisticsAsync(cancellationToken);
                    
                    var data = new Dictionary<string, object>
                    {
                        { "hit_ratio", statistics.HitRatio },
                        { "key_count", statistics.KeyCount },
                        { "memory_usage", statistics.MemoryUsage },
                        { "last_updated", statistics.LastUpdated }
                    };

                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                        "Redis cache is healthy", data);
                }
                else
                {
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                        "Redis cache health check failed");
                }
            }
            catch (Exception ex)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "Redis cache is unavailable", ex);
            }
        }
    }

    /// <summary>
    /// Cache metrics collector for monitoring
    /// </summary>
    public interface ICacheMetricsCollector
    {
        void RecordCacheHit(string entityType, string operation);
        void RecordCacheMiss(string entityType, string operation);
        void RecordCacheOperation(string operation, long durationMs, bool success);
        void RecordCacheSize(string entityType, long count);
    }

    public class CacheMetricsCollector : ICacheMetricsCollector
    {
        private readonly ILogger<CacheMetricsCollector> _logger;

        public CacheMetricsCollector(ILogger<CacheMetricsCollector> logger)
        {
            _logger = logger;
        }

        public void RecordCacheHit(string entityType, string operation)
        {
            _logger.LogDebug("Cache hit: {EntityType} {Operation}", entityType, operation);
            // TODO: Send to metrics backend (Prometheus, Application Insights, etc.)
        }

        public void RecordCacheMiss(string entityType, string operation)
        {
            _logger.LogDebug("Cache miss: {EntityType} {Operation}", entityType, operation);
            // TODO: Send to metrics backend
        }

        public void RecordCacheOperation(string operation, long durationMs, bool success)
        {
            _logger.LogDebug("Cache operation: {Operation} - {Duration}ms - Success: {Success}",
                operation, durationMs, success);
            // TODO: Send to metrics backend
        }

        public void RecordCacheSize(string entityType, long count)
        {
            _logger.LogDebug("Cache size: {EntityType} - {Count}", entityType, count);
            // TODO: Send to metrics backend
        }
    }

    /// <summary>
    /// Cache middleware for HTTP response caching
    /// </summary>
    public class CacheMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheMiddleware> _logger;

        public CacheMiddleware(
            RequestDelegate next,
            ICacheService cacheService,
            ILogger<CacheMiddleware> logger)
        {
            _next = next;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Implementation would depend on specific HTTP caching requirements
            // This is a placeholder for the middleware pattern
            await _next(context);
        }
    }
}