using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using System;
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
            // Register Circuit Breaker for Legacy System
            services.AddHttpClient<ILegacySystemClient, LegacySystemClient>()
                .AddPolicyHandler(GetLegacySystemCircuitBreakerPolicy())
                .AddPolicyHandler(GetRetryPolicy());

            // Register Circuit Breaker for External APIs
            services.AddHttpClient<IExternalApiClient, ExternalApiClient>()
                .AddPolicyHandler(GetExternalApiCircuitBreakerPolicy())
                .AddPolicyHandler(GetRetryPolicy());

            // Register Circuit Breaker for Database Operations
            services.AddSingleton<IDatabaseCircuitBreaker, DatabaseCircuitBreaker>();

            return services;
        }

        /// <summary>
        /// Circuit Breaker Policy für Legacy System
        /// Höhere Toleranz wegen bekannter Instabilität
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetLegacySystemCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => !msg.IsSuccessStatusCode)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(60),
                    onBreak: (result, duration, context) =>
                    {
                        var logger = context.Values.ContainsKey("logger") 
                            ? context.Values["logger"] as ILogger 
                            : null;
                        
                        logger?.LogWarning(
                            "Legacy System Circuit Breaker opened for {Duration}s. " +
                            "Reason: {Reason}",
                            duration.TotalSeconds,
                            result?.Result?.StatusCode ?? result?.Exception?.Message);
                    },
                    onReset: (context) =>
                    {
                        var logger = context.Values.ContainsKey("logger") 
                            ? context.Values["logger"] as ILogger 
                            : null;
                        
                        logger?.LogInformation("Legacy System Circuit Breaker reset. Connection restored.");
                    },
                    onHalfOpen: () =>
                    {
                        Console.WriteLine("Legacy System Circuit Breaker is half-open. Testing connection...");
                    });
        }

        /// <summary>
        /// Circuit Breaker Policy für externe APIs
        /// Striktere Policy für bessere Performance
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetExternalApiCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: 0.5,              // 50% failure rate
                    samplingDuration: TimeSpan.FromSeconds(30),
                    minimumThroughput: 10,              // Minimum 10 requests in sampling period
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (result, state, duration, context) =>
                    {
                        LogCircuitBreakerEvent(
                            "External API Circuit Breaker opened",
                            state,
                            duration,
                            context);
                    },
                    onReset: (context) =>
                    {
                        LogCircuitBreakerEvent(
                            "External API Circuit Breaker reset",
                            CircuitState.Closed,
                            TimeSpan.Zero,
                            context);
                    });
        }

        /// <summary>
        /// Retry Policy mit Exponential Backoff
        /// Kombiniert mit Circuit Breaker für optimale Resilience
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            var jitterer = new Random();
            
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => 
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) +
                        TimeSpan.FromMilliseconds(jitterer.Next(0, 100)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var logger = context.Values.ContainsKey("logger") 
                            ? context.Values["logger"] as ILogger 
                            : null;
                        
                        logger?.LogDebug(
                            "Retry {RetryCount} after {Delay}ms",
                            retryCount,
                            timespan.TotalMilliseconds);
                    });
        }

        private static void LogCircuitBreakerEvent(
            string message,
            CircuitState state,
            TimeSpan duration,
            Context context)
        {
            var logger = context.Values.ContainsKey("logger") 
                ? context.Values["logger"] as ILogger 
                : null;
            
            logger?.LogWarning(
                "{Message}. State: {State}, Duration: {Duration}s",
                message,
                state,
                duration.TotalSeconds);

            // Send metrics to Azure Monitor
            SendTelemetry(message, state, duration);
        }

        private static void SendTelemetry(string message, CircuitState state, TimeSpan duration)
        {
            // TODO: Implement Azure Application Insights telemetry
            // This would send custom metrics for monitoring dashboard
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
            
            _circuitBreakerPolicy = Policy
                .Handle<Exception>(ex => IsTransientDatabaseError(ex))
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, duration) =>
                    {
                        _logger.LogError(
                            exception,
                            "Database Circuit Breaker opened for {Duration}s",
                            duration.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Database Circuit Breaker reset");
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