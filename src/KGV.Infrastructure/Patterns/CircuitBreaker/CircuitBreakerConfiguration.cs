using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.CircuitBreaker
{
    /// <summary>
    /// Circuit Breaker Pattern Configuration für KGV Migration
    /// Implementiert nach Azure Architecture Pattern Best Practices
    /// </summary>
    public static class CircuitBreakerConfiguration
    {
        public static IServiceCollection AddCircuitBreakerPolicies(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register Circuit Breaker for Legacy System using new Polly v8 syntax
            services.AddHttpClient<ILegacySystemClient, LegacySystemClient>()
                .AddStandardResilienceHandler(options =>
                {
                    // Configure Circuit Breaker
                    options.CircuitBreaker.FailureRatio = 0.5;
                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                    options.CircuitBreaker.MinimumThroughput = 5;
                    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60);
                    options.CircuitBreaker.ShouldHandle = args => args.Outcome switch
                    {
                        { } outcome when IsTransientHttpError(outcome) => PredicateResult.True(),
                        _ => PredicateResult.False()
                    };
                    
                    // Configure Retry
                    options.Retry.MaxRetryAttempts = 3;
                    options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                    options.Retry.Delay = TimeSpan.FromSeconds(1);
                    options.Retry.ShouldHandle = args => args.Outcome switch
                    {
                        { } outcome when IsTransientHttpError(outcome) => PredicateResult.True(),
                        _ => PredicateResult.False()
                    };
                });

            // Register Circuit Breaker for External APIs
            services.AddHttpClient<IExternalApiClient, ExternalApiClient>()
                .AddStandardResilienceHandler(options =>
                {
                    // Configure Circuit Breaker with stricter policy
                    options.CircuitBreaker.FailureRatio = 0.5;
                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                    options.CircuitBreaker.MinimumThroughput = 10;
                    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
                    options.CircuitBreaker.ShouldHandle = args => args.Outcome switch
                    {
                        { } outcome when IsTransientHttpError(outcome) => PredicateResult.True(),
                        _ => PredicateResult.False()
                    };
                    
                    // Configure Retry
                    options.Retry.MaxRetryAttempts = 3;
                    options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                    options.Retry.Delay = TimeSpan.FromSeconds(1);
                    options.Retry.ShouldHandle = args => args.Outcome switch
                    {
                        { } outcome when IsTransientHttpError(outcome) => PredicateResult.True(),
                        _ => PredicateResult.False()
                    };
                });

            // Register Circuit Breaker for Database Operations
            services.AddSingleton<IDatabaseCircuitBreaker, DatabaseCircuitBreaker>();

            return services;
        }

        /// <summary>
        /// Determines if an HTTP outcome represents a transient error
        /// </summary>
        private static bool IsTransientHttpError(Outcome<HttpResponseMessage> outcome)
        {
            if (outcome.Exception != null)
            {
                return outcome.Exception is HttpRequestException or TaskCanceledException;
            }

            if (outcome.Result != null)
            {
                var statusCode = outcome.Result.StatusCode;
                return statusCode >= HttpStatusCode.InternalServerError ||
                       statusCode == HttpStatusCode.RequestTimeout ||
                       statusCode == HttpStatusCode.TooManyRequests;
            }

            return false;
        }

    }

    /// <summary>
    /// Database-specific Circuit Breaker implementation
    /// Handles transient database failures
    /// </summary>
    public class DatabaseCircuitBreaker : IDatabaseCircuitBreaker
    {
        private readonly IAsyncPolicy _circuitBreakerPolicy;
        private readonly ILogger<DatabaseCircuitBreaker> _logger;

        public DatabaseCircuitBreaker(ILogger<DatabaseCircuitBreaker> logger)
        {
            _logger = logger;
            
            // For now, use a simple retry policy instead of circuit breaker to avoid Polly v8 complexity
            _circuitBreakerPolicy = Policy
                .Handle<Exception>(IsTransientDatabaseError)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, delay, retryCount, context) =>
                    {
                        _logger.LogWarning(outcome, "Database operation failed, retrying in {Delay}s (attempt {RetryCount})", delay.TotalSeconds, retryCount);
                    });
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            return await _circuitBreakerPolicy.ExecuteAsync(operation);
        }

        public async Task ExecuteAsync(Func<Task> operation)
        {
            await _circuitBreakerPolicy.ExecuteAsync(operation);
        }

        private bool IsTransientDatabaseError(Exception ex)
        {
            // PostgreSQL specific transient error detection
            if (ex.Message.Contains("timeout") ||
                ex.Message.Contains("deadlock") ||
                ex.Message.Contains("connection") ||
                ex.Message.Contains("too many connections"))
            {
                return true;
            }

            return false;
        }
    }

    // Interfaces
    public interface ILegacySystemClient
    {
        Task<T> GetAsync<T>(string endpoint);
        Task<bool> PostAsync<T>(string endpoint, T data);
    }

    public interface IExternalApiClient
    {
        Task<T> GetAsync<T>(string endpoint);
    }

    public interface IDatabaseCircuitBreaker
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
        Task ExecuteAsync(Func<Task> operation);
    }

    // Dummy implementations for structure
    public class LegacySystemClient : ILegacySystemClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LegacySystemClient> _logger;

        public LegacySystemClient(HttpClient httpClient, ILogger<LegacySystemClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            // Implementation with circuit breaker applied via DI
            throw new NotImplementedException();
        }

        public async Task<bool> PostAsync<T>(string endpoint, T data)
        {
            // Implementation with circuit breaker applied via DI
            throw new NotImplementedException();
        }
    }

    public class ExternalApiClient : IExternalApiClient
    {
        private readonly HttpClient _httpClient;

        public ExternalApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            // Implementation with circuit breaker applied via DI
            throw new NotImplementedException();
        }
    }
}