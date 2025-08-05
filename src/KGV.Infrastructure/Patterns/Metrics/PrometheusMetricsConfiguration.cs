using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace KGV.Infrastructure.Patterns.Metrics
{
    /// <summary>
    /// Prometheus metrics configuration for KGV migration system
    /// Provides comprehensive monitoring and observability
    /// </summary>
    public static class PrometheusMetricsConfiguration
    {
        public static IServiceCollection AddPrometheusMetrics(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register Prometheus metrics
            services.AddSingleton<KgvMetrics>();
            
            // Register metrics collectors
            services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();
            services.AddSingleton<ApplicationMetricsCollector>();
            services.AddSingleton<CacheMetricsCollector>();
            services.AddSingleton<MessagingMetricsCollector>();
            services.AddSingleton<HealthMetricsCollector>();

            // Register custom metrics exporters
            services.AddSingleton<CustomMetricsExporter>();

            return services;
        }

        public static IApplicationBuilder UsePrometheusMetrics(
            this IApplicationBuilder app,
            IConfiguration configuration)
        {
            // Add Prometheus HTTP metrics middleware
            app.UseHttpMetrics(options =>
            {
                options.AddCustomLabel("application", context => "kgv-migration");
                options.AddCustomLabel("environment", context => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown");
            });

            // Expose metrics endpoint
            var metricsPath = configuration.GetValue<string>("Prometheus:MetricsPath", "/metrics");
            app.UseMetricServer(metricsPath);

            return app;
        }
    }

    /// <summary>
    /// Core KGV metrics definitions
    /// </summary>
    public class KgvMetrics
    {
        // Application metrics
        public static readonly Counter ApplicationsCreated = Prometheus.Metrics
            .CreateCounter("kgv_applications_created_total", "Total number of applications created",
                labelNames: new[] { "status", "district" });

        public static readonly Counter ApplicationsUpdated = Prometheus.Metrics
            .CreateCounter("kgv_applications_updated_total", "Total number of applications updated",
                labelNames: new[] { "change_type", "district" });

        public static readonly Gauge ApplicationsActive = Prometheus.Metrics
            .CreateGauge("kgv_applications_active", "Number of active applications",
                labelNames: new[] { "status", "district" });

        public static readonly Histogram ApplicationProcessingDuration = Prometheus.Metrics
            .CreateHistogram("kgv_application_processing_duration_seconds", 
                "Time spent processing applications",
                labelNames: new[] { "operation", "status" });

        // Cache metrics
        public static readonly Counter CacheOperations = Prometheus.Metrics
            .CreateCounter("kgv_cache_operations_total", "Total cache operations",
                labelNames: new[] { "operation", "entity_type", "result" });

        public static readonly Histogram CacheOperationDuration = Prometheus.Metrics
            .CreateHistogram("kgv_cache_operation_duration_seconds", "Cache operation duration",
                labelNames: new[] { "operation", "entity_type" });

        public static readonly Gauge CacheHitRatio = Prometheus.Metrics
            .CreateGauge("kgv_cache_hit_ratio", "Cache hit ratio",
                labelNames: new[] { "entity_type" });

        public static readonly Gauge CacheSize = Prometheus.Metrics
            .CreateGauge("kgv_cache_size_bytes", "Cache size in bytes",
                labelNames: new[] { "entity_type" });

        // Message queue metrics
        public static readonly Counter MessagesProcessed = Prometheus.Metrics
            .CreateCounter("kgv_messages_processed_total", "Total messages processed",
                labelNames: new[] { "queue", "status", "consumer" });

        public static readonly Gauge QueueLength = Prometheus.Metrics
            .CreateGauge("kgv_queue_length", "Number of messages in queue",
                labelNames: new[] { "queue", "priority" });

        public static readonly Histogram MessageProcessingDuration = Prometheus.Metrics
            .CreateHistogram("kgv_message_processing_duration_seconds", "Message processing duration",
                labelNames: new[] { "queue", "consumer" });

        public static readonly Gauge DeadLetterQueueLength = Prometheus.Metrics
            .CreateGauge("kgv_dead_letter_queue_length", "Number of messages in dead letter queue",
                labelNames: new[] { "queue" });

        // Database metrics
        public static readonly Counter DatabaseOperations = Prometheus.Metrics
            .CreateCounter("kgv_database_operations_total", "Total database operations",
                labelNames: new[] { "operation", "table", "result" });

        public static readonly Histogram DatabaseOperationDuration = Prometheus.Metrics
            .CreateHistogram("kgv_database_operation_duration_seconds", "Database operation duration",
                labelNames: new[] { "operation", "table" });

        public static readonly Gauge DatabaseConnections = Prometheus.Metrics
            .CreateGauge("kgv_database_connections", "Number of database connections",
                labelNames: new[] { "database", "state" });

        // Migration metrics
        public static readonly Counter MigrationRecordsProcessed = Prometheus.Metrics
            .CreateCounter("kgv_migration_records_processed_total", "Total migration records processed",
                labelNames: new[] { "source", "target", "status" });

        public static readonly Gauge MigrationProgress = Prometheus.Metrics
            .CreateGauge("kgv_migration_progress_percent", "Migration progress percentage",
                labelNames: new[] { "migration_type" });

        public static readonly Histogram MigrationBatchDuration = Prometheus.Metrics
            .CreateHistogram("kgv_migration_batch_duration_seconds", "Migration batch processing duration",
                labelNames: new[] { "migration_type", "batch_size" });

        // Health check metrics
        public static readonly Gauge HealthCheckStatus = Prometheus.Metrics
            .CreateGauge("kgv_health_check_status", "Health check status (1=healthy, 0=unhealthy)",
                labelNames: new[] { "check_name" });

        public static readonly Histogram HealthCheckDuration = Prometheus.Metrics
            .CreateHistogram("kgv_health_check_duration_seconds", "Health check duration",
                labelNames: new[] { "check_name" });

        // Performance metrics
        public static readonly Histogram HttpRequestDuration = Prometheus.Metrics
            .CreateHistogram("kgv_http_request_duration_seconds", "HTTP request duration",
                labelNames: new[] { "method", "endpoint", "status_code" });

        public static readonly Counter HttpRequests = Prometheus.Metrics
            .CreateCounter("kgv_http_requests_total", "Total HTTP requests",
                labelNames: new[] { "method", "endpoint", "status_code" });

        public static readonly Gauge MemoryUsage = Prometheus.Metrics
            .CreateGauge("kgv_memory_usage_bytes", "Memory usage in bytes",
                labelNames: new[] { "type" });

        public static readonly Gauge CpuUsage = Prometheus.Metrics
            .CreateGauge("kgv_cpu_usage_percent", "CPU usage percentage");

        // Error metrics
        public static readonly Counter Errors = Prometheus.Metrics
            .CreateCounter("kgv_errors_total", "Total errors",
                labelNames: new[] { "component", "error_type", "severity" });

        public static readonly Gauge ErrorRate = Prometheus.Metrics
            .CreateGauge("kgv_error_rate", "Error rate per minute",
                labelNames: new[] { "component" });

        // Business metrics
        public static readonly Gauge ApplicationBacklog = Prometheus.Metrics
            .CreateGauge("kgv_application_backlog", "Number of applications waiting for processing",
                labelNames: new[] { "status", "priority" });

        public static readonly Histogram ApplicationLifecycle = Prometheus.Metrics
            .CreateHistogram("kgv_application_lifecycle_duration_days", "Application lifecycle duration",
                labelNames: new[] { "from_status", "to_status" });

        public static readonly Counter UserActions = Prometheus.Metrics
            .CreateCounter("kgv_user_actions_total", "Total user actions",
                labelNames: new[] { "action", "user_role" });
    }

    /// <summary>
    /// Generic metrics collector interface
    /// </summary>
    public interface IMetricsCollector
    {
        void RecordCounter(string name, double value, params string[] labels);
        void SetGauge(string name, double value, params string[] labels);
        void RecordHistogram(string name, double value, params string[] labels);
    }

    /// <summary>
    /// Prometheus metrics collector implementation
    /// </summary>
    public class PrometheusMetricsCollector : IMetricsCollector
    {
        public void RecordCounter(string name, double value, params string[] labels)
        {
            // Implementation would dynamically create or update counters
        }

        public void SetGauge(string name, double value, params string[] labels)
        {
            // Implementation would dynamically create or update gauges
        }

        public void RecordHistogram(string name, double value, params string[] labels)
        {
            // Implementation would dynamically create or update histograms
        }
    }

    /// <summary>
    /// Application-specific metrics collector
    /// </summary>
    public class ApplicationMetricsCollector
    {
        public void RecordApplicationCreated(string status, string district)
        {
            KgvMetrics.ApplicationsCreated
                .WithLabels(status, district ?? "unknown")
                .Inc();
        }

        public void RecordApplicationUpdated(string changeType, string district)
        {
            KgvMetrics.ApplicationsUpdated
                .WithLabels(changeType, district ?? "unknown")
                .Inc();
        }

        public void UpdateApplicationsActive(string status, string district, double count)
        {
            KgvMetrics.ApplicationsActive
                .WithLabels(status, district ?? "unknown")
                .Set(count);
        }

        public void RecordApplicationProcessingTime(string operation, string status, double durationSeconds)
        {
            KgvMetrics.ApplicationProcessingDuration
                .WithLabels(operation, status)
                .Observe(durationSeconds);
        }

        public void UpdateApplicationBacklog(string status, string priority, double count)
        {
            KgvMetrics.ApplicationBacklog
                .WithLabels(status, priority)
                .Set(count);
        }

        public void RecordApplicationLifecycle(string fromStatus, string toStatus, double durationDays)
        {
            KgvMetrics.ApplicationLifecycle
                .WithLabels(fromStatus, toStatus)
                .Observe(durationDays);
        }

        public void RecordUserAction(string action, string userRole)
        {
            KgvMetrics.UserActions
                .WithLabels(action, userRole)
                .Inc();
        }
    }

    /// <summary>
    /// Cache-specific metrics collector
    /// </summary>
    public class CacheMetricsCollector
    {
        public void RecordCacheOperation(string operation, string entityType, string result, double durationSeconds)
        {
            KgvMetrics.CacheOperations
                .WithLabels(operation, entityType, result)
                .Inc();

            KgvMetrics.CacheOperationDuration
                .WithLabels(operation, entityType)
                .Observe(durationSeconds);
        }

        public void UpdateCacheHitRatio(string entityType, double hitRatio)
        {
            KgvMetrics.CacheHitRatio
                .WithLabels(entityType)
                .Set(hitRatio);
        }

        public void UpdateCacheSize(string entityType, double sizeBytes)
        {
            KgvMetrics.CacheSize
                .WithLabels(entityType)
                .Set(sizeBytes);
        }
    }

    /// <summary>
    /// Messaging-specific metrics collector
    /// </summary>
    public class MessagingMetricsCollector
    {
        public void RecordMessageProcessed(string queue, string status, string consumer, double durationSeconds)
        {
            KgvMetrics.MessagesProcessed
                .WithLabels(queue, status, consumer)
                .Inc();

            KgvMetrics.MessageProcessingDuration
                .WithLabels(queue, consumer)
                .Observe(durationSeconds);
        }

        public void UpdateQueueLength(string queue, string priority, double length)
        {
            KgvMetrics.QueueLength
                .WithLabels(queue, priority)
                .Set(length);
        }

        public void UpdateDeadLetterQueueLength(string queue, double length)
        {
            KgvMetrics.DeadLetterQueueLength
                .WithLabels(queue)
                .Set(length);
        }
    }

    /// <summary>
    /// Health check metrics collector
    /// </summary>
    public class HealthMetricsCollector
    {
        public void RecordHealthCheck(string checkName, bool isHealthy, double durationSeconds)
        {
            KgvMetrics.HealthCheckStatus
                .WithLabels(checkName)
                .Set(isHealthy ? 1 : 0);

            KgvMetrics.HealthCheckDuration
                .WithLabels(checkName)
                .Observe(durationSeconds);
        }
    }

    /// <summary>
    /// Custom metrics exporter for advanced scenarios
    /// </summary>
    public class CustomMetricsExporter
    {
        private readonly System.Timers.Timer _exportTimer;
        private readonly List<ICustomMetricSource> _metricSources;

        public CustomMetricsExporter()
        {
            _metricSources = new List<ICustomMetricSource>();
            _exportTimer = new System.Timers.Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
            _exportTimer.Elapsed += ExportMetrics;
            _exportTimer.Start();
        }

        public void RegisterMetricSource(ICustomMetricSource source)
        {
            _metricSources.Add(source);
        }

        private async void ExportMetrics(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                foreach (var source in _metricSources)
                {
                    var metrics = await source.GetMetricsAsync();
                    
                    foreach (var metric in metrics)
                    {
                        switch (metric.Type)
                        {
                            case MetricType.Counter:
                                // Update counter metric
                                break;
                            case MetricType.Gauge:
                                // Update gauge metric
                                break;
                            case MetricType.Histogram:
                                // Update histogram metric
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log export error
                KgvMetrics.Errors
                    .WithLabels("metrics", "export_error", "error")
                    .Inc();
            }
        }

        public void Dispose()
        {
            _exportTimer?.Stop();
            _exportTimer?.Dispose();
        }
    }

    /// <summary>
    /// Interface for custom metric sources
    /// </summary>
    public interface ICustomMetricSource
    {
        Task<IEnumerable<CustomMetric>> GetMetricsAsync();
    }

    /// <summary>
    /// Custom metric definition
    /// </summary>
    public class CustomMetric
    {
        public string Name { get; set; }
        public MetricType Type { get; set; }
        public double Value { get; set; }
        public Dictionary<string, string> Labels { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public enum MetricType
    {
        Counter,
        Gauge,
        Histogram
    }

    /// <summary>
    /// System performance metrics source
    /// </summary>
    public class SystemPerformanceMetricSource : ICustomMetricSource
    {
        public async Task<IEnumerable<CustomMetric>> GetMetricsAsync()
        {
            var metrics = new List<CustomMetric>();

            try
            {
                // Memory metrics
                var process = System.Diagnostics.Process.GetCurrentProcess();
                metrics.Add(new CustomMetric
                {
                    Name = "kgv_memory_working_set_bytes",
                    Type = MetricType.Gauge,
                    Value = process.WorkingSet64
                });

                metrics.Add(new CustomMetric
                {
                    Name = "kgv_memory_private_bytes",
                    Type = MetricType.Gauge,
                    Value = process.PrivateMemorySize64
                });

                // GC metrics
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);
                var totalMemory = GC.GetTotalMemory(false);

                metrics.Add(new CustomMetric
                {
                    Name = "kgv_gc_collections_total",
                    Type = MetricType.Counter,
                    Value = gen0Collections,
                    Labels = new Dictionary<string, string> { { "generation", "0" } }
                });

                metrics.Add(new CustomMetric
                {
                    Name = "kgv_gc_collections_total",
                    Type = MetricType.Counter,
                    Value = gen1Collections,
                    Labels = new Dictionary<string, string> { { "generation", "1" } }
                });

                metrics.Add(new CustomMetric
                {
                    Name = "kgv_gc_collections_total",
                    Type = MetricType.Counter,
                    Value = gen2Collections,
                    Labels = new Dictionary<string, string> { { "generation", "2" } }
                });

                metrics.Add(new CustomMetric
                {
                    Name = "kgv_gc_memory_bytes",
                    Type = MetricType.Gauge,
                    Value = totalMemory
                });

                // Thread metrics
                metrics.Add(new CustomMetric
                {
                    Name = "kgv_threads_count",
                    Type = MetricType.Gauge,
                    Value = process.Threads.Count
                });

                // Handle metrics
                metrics.Add(new CustomMetric
                {
                    Name = "kgv_handles_count",
                    Type = MetricType.Gauge,
                    Value = process.HandleCount
                });
            }
            catch (Exception ex)
            {
                // Log error but don't fail metric collection
                metrics.Add(new CustomMetric
                {
                    Name = "kgv_metric_collection_errors_total",
                    Type = MetricType.Counter,
                    Value = 1,
                    Labels = new Dictionary<string, string> { { "source", "system_performance" } }
                });
            }

            return metrics;
        }
    }
}