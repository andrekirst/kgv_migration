using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.HealthChecks
{
    /// <summary>
    /// Prometheus metrics publisher for health check results
    /// Exposes health check metrics in Prometheus format
    /// </summary>
    public class PrometheusHealthCheckPublisher : IHealthCheckPublisher
    {
        private readonly ILogger<PrometheusHealthCheckPublisher> _logger;
        private readonly Dictionary<string, HealthCheckMetric> _metrics;

        public PrometheusHealthCheckPublisher(ILogger<PrometheusHealthCheckPublisher> logger)
        {
            _logger = logger;
            _metrics = new Dictionary<string, HealthCheckMetric>();
        }

        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            try
            {
                // Update metrics for each health check
                foreach (var (name, entry) in report.Entries)
                {
                    var metric = GetOrCreateMetric(name);
                    
                    metric.Status = entry.Status;
                    metric.Duration = entry.Duration;
                    metric.LastUpdated = DateTime.UtcNow;
                    metric.Data = entry.Data;
                    metric.Description = entry.Description;
                    metric.Exception = entry.Exception?.Message;

                    // Log metric update
                    _logger.LogDebug("Updated health check metric: {Name} = {Status} ({Duration}ms)",
                        name, entry.Status, entry.Duration.TotalMilliseconds);
                }

                // Update overall health status
                var overallMetric = GetOrCreateMetric("overall_health");
                overallMetric.Status = report.Status;
                overallMetric.Duration = report.TotalDuration;
                overallMetric.LastUpdated = DateTime.UtcNow;

                // Log overall status
                _logger.LogInformation("Health check report published: {Status} ({Duration}ms, {Count} checks)",
                    report.Status, report.TotalDuration.TotalMilliseconds, report.Entries.Count);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish health check metrics");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Get current health metrics in Prometheus format
        /// </summary>
        public string GetPrometheusMetrics()
        {
            var sb = new StringBuilder();

            try
            {
                // Health check status metric (0=Unhealthy, 1=Degraded, 2=Healthy)
                sb.AppendLine("# HELP kgv_health_check_status Health check status (0=Unhealthy, 1=Degraded, 2=Healthy)");
                sb.AppendLine("# TYPE kgv_health_check_status gauge");

                foreach (var (name, metric) in _metrics)
                {
                    var statusValue = metric.Status switch
                    {
                        HealthStatus.Healthy => 2,
                        HealthStatus.Degraded => 1,
                        HealthStatus.Unhealthy => 0,
                        _ => 0
                    };

                    var labels = $"check=\"{name}\"";
                    sb.AppendLine($"kgv_health_check_status{{{labels}}} {statusValue}");
                }

                // Health check duration metric
                sb.AppendLine();
                sb.AppendLine("# HELP kgv_health_check_duration_seconds Health check duration in seconds");
                sb.AppendLine("# TYPE kgv_health_check_duration_seconds gauge");

                foreach (var (name, metric) in _metrics)
                {
                    var labels = $"check=\"{name}\"";
                    var duration = metric.Duration.TotalSeconds;
                    sb.AppendLine($"kgv_health_check_duration_seconds{{{labels}}} {duration:F3}");
                }

                // Last updated timestamp
                sb.AppendLine();
                sb.AppendLine("# HELP kgv_health_check_last_updated_timestamp_seconds Last health check update timestamp");
                sb.AppendLine("# TYPE kgv_health_check_last_updated_timestamp_seconds gauge");

                foreach (var (name, metric) in _metrics)
                {
                    var labels = $"check=\"{name}\"";
                    var timestamp = ((DateTimeOffset)metric.LastUpdated).ToUnixTimeSeconds();
                    sb.AppendLine($"kgv_health_check_last_updated_timestamp_seconds{{{labels}}} {timestamp}");
                }

                // Application info metric
                sb.AppendLine();
                sb.AppendLine("# HELP kgv_application_info Application information");
                sb.AppendLine("# TYPE kgv_application_info gauge");
                
                var appLabels = $"version=\"1.0.0\",environment=\"{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown"}\",machine=\"{Environment.MachineName}\"";
                sb.AppendLine($"kgv_application_info{{{appLabels}}} 1");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Prometheus metrics");
                return "# Error generating metrics\n";
            }
        }

        private HealthCheckMetric GetOrCreateMetric(string name)
        {
            if (!_metrics.ContainsKey(name))
            {
                _metrics[name] = new HealthCheckMetric { Name = name };
            }
            return _metrics[name];
        }
    }

    /// <summary>
    /// Logging publisher for health check results
    /// Provides detailed logging of health check status
    /// </summary>
    public class LoggingHealthCheckPublisher : IHealthCheckPublisher
    {
        private readonly ILogger<LoggingHealthCheckPublisher> _logger;

        public LoggingHealthCheckPublisher(ILogger<LoggingHealthCheckPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            try
            {
                var level = report.Status switch
                {
                    HealthStatus.Healthy => LogLevel.Debug,
                    HealthStatus.Degraded => LogLevel.Warning,
                    HealthStatus.Unhealthy => LogLevel.Error,
                    _ => LogLevel.Information
                };

                // Log overall status
                _logger.Log(level, "Health Check Report: {Status} ({Duration}ms)",
                    report.Status, report.TotalDuration.TotalMilliseconds);

                // Log individual check results
                foreach (var (name, entry) in report.Entries)
                {
                    var entryLevel = entry.Status switch
                    {
                        HealthStatus.Healthy => LogLevel.Debug,
                        HealthStatus.Degraded => LogLevel.Warning,
                        HealthStatus.Unhealthy => LogLevel.Error,
                        _ => LogLevel.Information
                    };

                    var message = $"Health Check '{name}': {entry.Status}";
                    if (!string.IsNullOrEmpty(entry.Description))
                    {
                        message += $" - {entry.Description}";
                    }

                    if (entry.Exception != null)
                    {
                        _logger.Log(entryLevel, entry.Exception, message);
                    }
                    else
                    {
                        _logger.Log(entryLevel, message);
                    }

                    // Log additional data if available and in debug mode
                    if (_logger.IsEnabled(LogLevel.Debug) && entry.Data.Any())
                    {
                        var dataJson = JsonSerializer.Serialize(entry.Data, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        _logger.LogDebug("Health Check '{Name}' Data: {Data}", name, dataJson);
                    }
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish health check logs");
                return Task.CompletedTask;
            }
        }
    }

    /// <summary>
    /// Webhook publisher for health check results
    /// Sends health check results to external monitoring systems
    /// </summary>
    public class WebhookHealthCheckPublisher : IHealthCheckPublisher
    {
        private readonly ILogger<WebhookHealthCheckPublisher> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _webhookUrl;
        private readonly string _apiKey;

        public WebhookHealthCheckPublisher(
            ILogger<WebhookHealthCheckPublisher> logger,
            HttpClient httpClient,
            string webhookUrl,
            string apiKey = null)
        {
            _logger = logger;
            _httpClient = httpClient;
            _webhookUrl = webhookUrl;
            _apiKey = apiKey;
        }

        public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_webhookUrl))
            {
                return;
            }

            try
            {
                var payload = CreateWebhookPayload(report);
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var request = new HttpRequestMessage(HttpMethod.Post, _webhookUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrEmpty(_apiKey))
                {
                    request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                }

                request.Headers.Add("User-Agent", "KGV-HealthCheck/1.0");

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Health check webhook sent successfully to {Url}", _webhookUrl);
                }
                else
                {
                    _logger.LogWarning("Health check webhook failed: {Status} {Reason}",
                        response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send health check webhook to {Url}", _webhookUrl);
            }
        }

        private object CreateWebhookPayload(HealthReport report)
        {
            return new
            {
                timestamp = DateTime.UtcNow,
                application = "KGV-Migration",
                status = report.Status.ToString(),
                duration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration.TotalMilliseconds,
                    description = entry.Value.Description,
                    data = entry.Value.Data,
                    exception = entry.Value.Exception?.Message,
                    tags = entry.Value.Tags
                }).ToArray()
            };
        }
    }

    /// <summary>
    /// File-based publisher for health check results
    /// Writes health check status to file for container orchestration
    /// </summary>
    public class FileHealthCheckPublisher : IHealthCheckPublisher
    {
        private readonly ILogger<FileHealthCheckPublisher> _logger;
        private readonly string _healthFilePath;
        private readonly string _readinessFilePath;

        public FileHealthCheckPublisher(
            ILogger<FileHealthCheckPublisher> logger,
            string healthFilePath = "/tmp/health",
            string readinessFilePath = "/tmp/ready")
        {
            _logger = logger;
            _healthFilePath = healthFilePath;
            _readinessFilePath = readinessFilePath;
        }

        public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            try
            {
                // Write overall health status
                await WriteStatusFile(_healthFilePath, report.Status, report);

                // Write readiness status (based on "ready" tagged checks)
                var readyChecks = report.Entries.Where(e => e.Value.Tags.Contains("ready"));
                var readyStatus = readyChecks.All(e => e.Value.Status == HealthStatus.Healthy)
                    ? HealthStatus.Healthy
                    : HealthStatus.Unhealthy;

                await WriteStatusFile(_readinessFilePath, readyStatus, report);

                _logger.LogDebug("Health check status written to files: {Health} = {HealthStatus}, {Ready} = {ReadyStatus}",
                    _healthFilePath, report.Status, _readinessFilePath, readyStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write health check status files");
            }
        }

        private async Task WriteStatusFile(string filePath, HealthStatus status, HealthReport report)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var content = new
                {
                    status = status.ToString(),
                    timestamp = DateTime.UtcNow.ToString("O"),
                    duration = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Count
                };

                var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write status file {FilePath}", filePath);
            }
        }
    }

    /// <summary>
    /// Health check metric data structure
    /// </summary>
    public class HealthCheckMetric
    {
        public string Name { get; set; }
        public HealthStatus Status { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime LastUpdated { get; set; }
        public IReadOnlyDictionary<string, object> Data { get; set; }
        public string Description { get; set; }
        public string Exception { get; set; }
    }
}