using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;

namespace KGV.Infrastructure.Patterns.Messaging
{
    /// <summary>
    /// Configuration for Queue-Based Load Leveling pattern with Redis
    /// Provides complete messaging infrastructure setup for KGV migration system
    /// </summary>
    public static class MessagingConfiguration
    {
        public static IServiceCollection AddQueueBasedLoadLeveling(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure messaging options
            services.Configure<RedisQueueOptions>(configuration.GetSection("Messaging:Redis"));
            services.Configure<LoadLevelingOptions>(configuration.GetSection("Messaging:LoadLeveling"));
            services.Configure<MessageProcessorOptions>(configuration.GetSection("Messaging:Processor"));

            // Register Redis connection (reuse from caching if available)
            var connectionString = configuration.GetConnectionString("Redis") ?? 
                                 configuration.GetSection("Messaging:Redis:ConnectionString").Value;

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Redis connection string is required for messaging");
            }

            // Register core messaging services
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
            services.AddSingleton<ILoadLevelingStrategy, LoadLevelingService>();
            services.AddSingleton<IQueueCircuitBreaker, QueueCircuitBreaker>();
            services.AddSingleton<IMessagePublisher, MessagePublisher>();

            // Register queue factory
            services.AddSingleton<IQueueFactory, RedisQueueFactory>();

            // Register specific message queues for KGV domain
            RegisterKgvMessageQueues(services, configuration);

            // Register message consumers
            RegisterKgvMessageConsumers(services);

            // Register message processors as hosted services
            RegisterKgvMessageProcessors(services, configuration);

            // Register queue monitoring
            services.AddSingleton<IQueueMonitor, QueueMonitor>();

            // Register health checks for messaging
            services.AddHealthChecks()
                .AddCheck<MessagingHealthCheck>("messaging-queues", 
                    Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                    new[] { "messaging", "redis", "ready" });

            return services;
        }

        private static void RegisterKgvMessageQueues(IServiceCollection services, IConfiguration configuration)
        {
            // Register message queues for KGV domain events
            services.AddSingleton<IMessageQueue<ApplicationCreatedEvent>>(provider =>
                CreateMessageQueue<ApplicationCreatedEvent>(provider, "kgv.application.created"));

            services.AddSingleton<IMessageQueue<ApplicationUpdatedEvent>>(provider =>
                CreateMessageQueue<ApplicationUpdatedEvent>(provider, "kgv.application.updated"));

            services.AddSingleton<IMessageQueue<ApplicationStatusChangedEvent>>(provider =>
                CreateMessageQueue<ApplicationStatusChangedEvent>(provider, "kgv.application.status_changed"));

            services.AddSingleton<IMessageQueue<DataMigrationEvent>>(provider =>
                CreateMessageQueue<DataMigrationEvent>(provider, "kgv.migration.data"));

            services.AddSingleton<IMessageQueue<NotificationEvent>>(provider =>
                CreateMessageQueue<NotificationEvent>(provider, "kgv.notification"));

            services.AddSingleton<IMessageQueue<AuditEvent>>(provider =>
                CreateMessageQueue<AuditEvent>(provider, "kgv.audit"));
        }

        private static void RegisterKgvMessageConsumers(IServiceCollection services)
        {
            // Register message consumers for KGV domain
            services.AddScoped<IMessageConsumer<ApplicationCreatedEvent>, ApplicationCreatedEventConsumer>();
            services.AddScoped<IMessageConsumer<ApplicationUpdatedEvent>, ApplicationUpdatedEventConsumer>();
            services.AddScoped<IMessageConsumer<ApplicationStatusChangedEvent>, ApplicationStatusChangedEventConsumer>();
            services.AddScoped<IMessageConsumer<DataMigrationEvent>, DataMigrationEventConsumer>();
            services.AddScoped<IMessageConsumer<NotificationEvent>, NotificationEventConsumer>();
            services.AddScoped<IMessageConsumer<AuditEvent>, AuditEventConsumer>();
        }

        private static void RegisterKgvMessageProcessors(IServiceCollection services, IConfiguration configuration)
        {
            // Register message processors as hosted services
            if (configuration.GetValue<bool>("Messaging:EnableApplicationEventProcessor", true))
            {
                services.AddSingleton<IHostedService, MessageProcessor<ApplicationCreatedEvent>>();
                services.AddSingleton<IHostedService, MessageProcessor<ApplicationUpdatedEvent>>();
                services.AddSingleton<IHostedService, MessageProcessor<ApplicationStatusChangedEvent>>();
            }

            if (configuration.GetValue<bool>("Messaging:EnableMigrationEventProcessor", true))
            {
                services.AddSingleton<IHostedService, MessageProcessor<DataMigrationEvent>>();
            }

            if (configuration.GetValue<bool>("Messaging:EnableNotificationProcessor", true))
            {
                services.AddSingleton<IHostedService, MessageProcessor<NotificationEvent>>();
            }

            if (configuration.GetValue<bool>("Messaging:EnableAuditProcessor", true))
            {
                services.AddSingleton<IHostedService, MessageProcessor<AuditEvent>>();
            }
        }

        private static IMessageQueue<T> CreateMessageQueue<T>(IServiceProvider provider, string queueName) where T : class
        {
            var redis = provider.GetRequiredService<IConnectionMultiplexer>();
            var logger = provider.GetRequiredService<ILogger<RedisMessageQueue<T>>>();
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisQueueOptions>>();
            var serializer = provider.GetRequiredService<IMessageSerializer>();

            return new RedisMessageQueue<T>(redis, logger, options, serializer, queueName);
        }
    }

    /// <summary>
    /// Queue factory for creating message queues dynamically
    /// </summary>
    public interface IQueueFactory
    {
        IMessageQueue<T> CreateQueue<T>(string queueName) where T : class;
    }

    public class RedisQueueFactory : IQueueFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public RedisQueueFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IMessageQueue<T> CreateQueue<T>(string queueName) where T : class
        {
            var redis = _serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            var logger = _serviceProvider.GetRequiredService<ILogger<RedisMessageQueue<T>>>();
            var options = _serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisQueueOptions>>();
            var serializer = _serviceProvider.GetRequiredService<IMessageSerializer>();

            return new RedisMessageQueue<T>(redis, logger, options, serializer, queueName);
        }
    }

    /// <summary>
    /// Circuit breaker implementation for queue operations
    /// </summary>
    public class QueueCircuitBreaker : IQueueCircuitBreaker
    {
        private readonly ILogger<QueueCircuitBreaker> _logger;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CircuitBreakerState> _states;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CircuitBreakerMetrics> _metrics;

        public QueueCircuitBreaker(ILogger<QueueCircuitBreaker> logger)
        {
            _logger = logger;
            _states = new System.Collections.Concurrent.ConcurrentDictionary<string, CircuitBreakerState>();
            _metrics = new System.Collections.Concurrent.ConcurrentDictionary<string, CircuitBreakerMetrics>();
        }

        public async Task<bool> CanExecuteAsync(string queueName, CancellationToken cancellationToken = default)
        {
            var state = _states.GetOrAdd(queueName, _ => CircuitBreakerState.Closed);
            var metrics = GetOrCreateMetrics(queueName);

            switch (state)
            {
                case CircuitBreakerState.Closed:
                    return true;

                case CircuitBreakerState.Open:
                    // Check if we should transition to half-open
                    if (DateTime.UtcNow > metrics.NextAttemptTime)
                    {
                        _states.TryUpdate(queueName, CircuitBreakerState.HalfOpen, CircuitBreakerState.Open);
                        _logger.LogInformation("Circuit breaker for queue {QueueName} moved to half-open", queueName);
                        return true;
                    }
                    return false;

                case CircuitBreakerState.HalfOpen:
                    return true;

                default:
                    return false;
            }
        }

        public async Task RecordSuccessAsync(string queueName, CancellationToken cancellationToken = default)
        {
            var metrics = GetOrCreateMetrics(queueName);
            System.Threading.Interlocked.Increment(ref metrics.SuccessCount);

            var state = _states.GetOrAdd(queueName, _ => CircuitBreakerState.Closed);
            if (state == CircuitBreakerState.HalfOpen)
            {
                // Move to closed state on success in half-open
                _states.TryUpdate(queueName, CircuitBreakerState.Closed, CircuitBreakerState.HalfOpen);
                metrics.FailureCount = 0; // Reset failure count
                _logger.LogInformation("Circuit breaker for queue {QueueName} moved to closed", queueName);
            }
        }

        public async Task RecordFailureAsync(string queueName, Exception exception, CancellationToken cancellationToken = default)
        {
            var metrics = GetOrCreateMetrics(queueName);
            System.Threading.Interlocked.Increment(ref metrics.FailureCount);

            var state = _states.GetOrAdd(queueName, _ => CircuitBreakerState.Closed);
            
            if (state == CircuitBreakerState.HalfOpen)
            {
                // Move back to open on failure in half-open
                _states.TryUpdate(queueName, CircuitBreakerState.Open, CircuitBreakerState.HalfOpen);
                metrics.NextAttemptTime = DateTime.UtcNow.AddMinutes(1); // Wait 1 minute before next attempt
                _logger.LogWarning("Circuit breaker for queue {QueueName} moved to open due to failure in half-open", queueName);
            }
            else if (metrics.FailureCount >= 5) // Configurable threshold
            {
                // Move to open state on too many failures
                _states.TryUpdate(queueName, CircuitBreakerState.Open, CircuitBreakerState.Closed);
                metrics.NextAttemptTime = DateTime.UtcNow.AddMinutes(1);
                _logger.LogWarning("Circuit breaker for queue {QueueName} opened due to {FailureCount} failures", 
                    queueName, metrics.FailureCount);
            }
        }

        public async Task<CircuitBreakerState> GetStateAsync(string queueName, CancellationToken cancellationToken = default)
        {
            return _states.GetOrAdd(queueName, _ => CircuitBreakerState.Closed);
        }

        private CircuitBreakerMetrics GetOrCreateMetrics(string queueName)
        {
            return _metrics.GetOrAdd(queueName, _ => new CircuitBreakerMetrics());
        }
    }

    internal class CircuitBreakerMetrics
    {
        public long SuccessCount;
        public long FailureCount;
        public DateTime NextAttemptTime;
    }

    /// <summary>
    /// Queue monitoring implementation
    /// </summary>
    public class QueueMonitor : IQueueMonitor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<QueueMonitor> _logger;

        public event Action<QueueAlert> AlertRaised;

        public QueueMonitor(IServiceProvider serviceProvider, ILogger<QueueMonitor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<QueueHealth> CheckHealthAsync(string queueName, CancellationToken cancellationToken = default)
        {
            // Implementation would check specific queue health
            // This is a placeholder
            return new QueueHealth
            {
                IsHealthy = true,
                Status = "Healthy",
                MessageBacklog = 0,
                ProcessingRate = 0,
                LastMessageProcessed = DateTime.UtcNow
            };
        }

        public async Task<QueueStatistics> GetStatisticsAsync(string queueName, CancellationToken cancellationToken = default)
        {
            // Implementation would get statistics from specific queue
            // This is a placeholder
            return new QueueStatistics
            {
                QueueName = queueName,
                ActiveMessageCount = 0,
                DeadLetterMessageCount = 0,
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<QueueAlert>> GetAlertsAsync(CancellationToken cancellationToken = default)
        {
            // Implementation would return current alerts
            return new List<QueueAlert>();
        }
    }

    /// <summary>
    /// Messaging health check
    /// </summary>
    public class MessagingHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly IQueueMonitor _queueMonitor;

        public MessagingHealthCheck(IQueueMonitor queueMonitor)
        {
            _queueMonitor = queueMonitor;
        }

        public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check health of critical queues
                var queueNames = new[] 
                { 
                    "kgv.application.created", 
                    "kgv.application.updated", 
                    "kgv.migration.data" 
                };

                var healthTasks = queueNames.Select(name => _queueMonitor.CheckHealthAsync(name, cancellationToken));
                var healthResults = await Task.WhenAll(healthTasks);

                var unhealthyQueues = healthResults.Where(h => !h.IsHealthy).ToList();

                var data = new Dictionary<string, object>
                {
                    { "total_queues", queueNames.Length },
                    { "healthy_queues", healthResults.Count(h => h.IsHealthy) },
                    { "unhealthy_queues", unhealthyQueues.Count }
                };

                if (unhealthyQueues.Any())
                {
                    var unhealthyNames = string.Join(", ", unhealthyQueues.Select(h => h.Status));
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                        $"Unhealthy queues: {unhealthyNames}", data: data);
                }

                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                    "All messaging queues are healthy", data);
            }
            catch (Exception ex)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "Messaging health check failed", ex);
            }
        }
    }

    #region KGV Domain Events (Placeholders)

    // These would typically be in a separate domain events assembly
    public class ApplicationCreatedEvent
    {
        public Guid ApplicationId { get; set; }
        public string FileReference { get; set; }
        public string PrimaryContactName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }

    public class ApplicationUpdatedEvent
    {
        public Guid ApplicationId { get; set; }
        public string FileReference { get; set; }
        public Dictionary<string, object> Changes { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class ApplicationStatusChangedEvent
    {
        public Guid ApplicationId { get; set; }
        public string FileReference { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public string Reason { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; }
    }

    public class DataMigrationEvent
    {
        public string MigrationType { get; set; }
        public int RecordsProcessed { get; set; }
        public int RecordsSuccessful { get; set; }
        public int RecordsFailed { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public List<string> Errors { get; set; }
    }

    public class NotificationEvent
    {
        public string Recipient { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    public class AuditEvent
    {
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string Action { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Changes { get; set; }
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
    }

    #endregion

    #region Message Consumer Implementations (Placeholders)

    public class ApplicationCreatedEventConsumer : IMessageConsumer<ApplicationCreatedEvent>
    {
        private readonly ILogger<ApplicationCreatedEventConsumer> _logger;

        public ApplicationCreatedEventConsumer(ILogger<ApplicationCreatedEventConsumer> logger)
        {
            _logger = logger;
        }

        public async Task<bool> HandleAsync(QueueMessage<ApplicationCreatedEvent> message, CancellationToken cancellationToken = default)
        {
            try
            {
                var evt = message.Body;
                _logger.LogInformation("Processing ApplicationCreatedEvent for {ApplicationId}", evt.ApplicationId);

                // Implementation would handle the event (e.g., send notifications, update search index, etc.)
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process ApplicationCreatedEvent");
                return false;
            }
        }
    }

    public class ApplicationUpdatedEventConsumer : IMessageConsumer<ApplicationUpdatedEvent>
    {
        private readonly ILogger<ApplicationUpdatedEventConsumer> _logger;

        public ApplicationUpdatedEventConsumer(ILogger<ApplicationUpdatedEventConsumer> logger)
        {
            _logger = logger;
        }

        public async Task<bool> HandleAsync(QueueMessage<ApplicationUpdatedEvent> message, CancellationToken cancellationToken = default)
        {
            try
            {
                var evt = message.Body;
                _logger.LogInformation("Processing ApplicationUpdatedEvent for {ApplicationId}", evt.ApplicationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process ApplicationUpdatedEvent");
                return false;
            }
        }
    }

    public class ApplicationStatusChangedEventConsumer : IMessageConsumer<ApplicationStatusChangedEvent>
    {
        private readonly ILogger<ApplicationStatusChangedEventConsumer> _logger;

        public ApplicationStatusChangedEventConsumer(ILogger<ApplicationStatusChangedEventConsumer> logger)
        {
            _logger = logger;
        }

        public async Task<bool> HandleAsync(QueueMessage<ApplicationStatusChangedEvent> message, CancellationToken cancellationToken = default)
        {
            try
            {
                var evt = message.Body;
                _logger.LogInformation("Processing status change for {ApplicationId}: {OldStatus} -> {NewStatus}", 
                    evt.ApplicationId, evt.OldStatus, evt.NewStatus);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process ApplicationStatusChangedEvent");
                return false;
            }
        }
    }

    public class DataMigrationEventConsumer : IMessageConsumer<DataMigrationEvent>
    {
        private readonly ILogger<DataMigrationEventConsumer> _logger;

        public DataMigrationEventConsumer(ILogger<DataMigrationEventConsumer> logger)
        {
            _logger = logger;
        }

        public async Task<bool> HandleAsync(QueueMessage<DataMigrationEvent> message, CancellationToken cancellationToken = default)
        {
            try
            {
                var evt = message.Body;
                _logger.LogInformation("Processing migration event: {Type}, {Processed} records", 
                    evt.MigrationType, evt.RecordsProcessed);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process DataMigrationEvent");
                return false;
            }
        }
    }

    public class NotificationEventConsumer : IMessageConsumer<NotificationEvent>
    {
        private readonly ILogger<NotificationEventConsumer> _logger;

        public NotificationEventConsumer(ILogger<NotificationEventConsumer> logger)
        {
            _logger = logger;
        }

        public async Task<bool> HandleAsync(QueueMessage<NotificationEvent> message, CancellationToken cancellationToken = default)
        {
            try
            {
                var evt = message.Body;
                _logger.LogInformation("Processing notification for {Recipient}: {Subject}", 
                    evt.Recipient, evt.Subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process NotificationEvent");
                return false;
            }
        }
    }

    public class AuditEventConsumer : IMessageConsumer<AuditEvent>
    {
        private readonly ILogger<AuditEventConsumer> _logger;

        public AuditEventConsumer(ILogger<AuditEventConsumer> logger)
        {
            _logger = logger;
        }

        public async Task<bool> HandleAsync(QueueMessage<AuditEvent> message, CancellationToken cancellationToken = default)
        {
            try
            {
                var evt = message.Body;
                _logger.LogInformation("Processing audit event: {Action} on {EntityType} {EntityId} by {UserId}",
                    evt.Action, evt.EntityType, evt.EntityId, evt.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process AuditEvent");
                return false;
            }
        }
    }

    #endregion
}