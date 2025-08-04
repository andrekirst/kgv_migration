using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.Messaging
{
    /// <summary>
    /// Load leveling service implementing Queue-Based Load Leveling pattern
    /// Manages processing load and prevents system overload through adaptive throttling
    /// </summary>
    public class LoadLevelingService : ILoadLevelingStrategy
    {
        private readonly ILogger<LoadLevelingService> _logger;
        private readonly LoadLevelingOptions _options;
        private readonly ConcurrentDictionary<string, QueueMetrics> _queueMetrics;
        private readonly ConcurrentDictionary<string, AdaptiveThrottling> _throttling;
        private readonly Timer _metricsTimer;

        public LoadLevelingService(
            ILogger<LoadLevelingService> logger,
            IOptions<LoadLevelingOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            _queueMetrics = new ConcurrentDictionary<string, QueueMetrics>();
            _throttling = new ConcurrentDictionary<string, AdaptiveThrottling>();
            
            // Start metrics cleanup timer
            _metricsTimer = new Timer(CleanupOldMetrics, null, 
                _options.MetricsCleanupInterval, _options.MetricsCleanupInterval);

            _logger.LogInformation("Load leveling service initialized with max concurrent load: {MaxLoad}",
                _options.MaxConcurrentLoad);
        }

        public async Task<bool> ShouldProcessAsync(string queueName, int currentLoad, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(queueName))
                throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));

            var metrics = GetOrCreateQueueMetrics(queueName);
            var throttling = GetOrCreateThrottling(queueName);

            // Update current load
            metrics.CurrentLoad = currentLoad;
            metrics.LastActivity = DateTime.UtcNow;

            // Check global load limit
            if (currentLoad >= _options.MaxConcurrentLoad)
            {
                _logger.LogDebug("Processing rejected for queue {QueueName}: Global load limit reached ({Current}/{Max})",
                    queueName, currentLoad, _options.MaxConcurrentLoad);
                return false;
            }

            // Check queue-specific throttling
            if (throttling.IsThrottled)
            {
                if (DateTime.UtcNow < throttling.ThrottleUntil)
                {
                    _logger.LogDebug("Processing rejected for queue {QueueName}: Queue is throttled until {ThrottleUntil}",
                        queueName, throttling.ThrottleUntil);
                    return false;
                }
                else
                {
                    // Throttling period expired
                    throttling.IsThrottled = false;
                    throttling.ThrottleUntil = null;
                    _logger.LogInformation("Throttling expired for queue {QueueName}", queueName);
                }
            }

            // Check error rate threshold
            var errorRate = CalculateErrorRate(metrics);
            if (errorRate > _options.MaxErrorRateThreshold)
            {
                await ApplyThrottlingAsync(queueName, throttling, $"High error rate: {errorRate:P2}");
                return false;
            }

            // Check average processing time threshold
            var avgProcessingTime = CalculateAverageProcessingTime(metrics);
            if (avgProcessingTime > _options.MaxAverageProcessingTime)
            {
                await ApplyThrottlingAsync(queueName, throttling, 
                    $"High average processing time: {avgProcessingTime.TotalMilliseconds}ms");
                return false;
            }

            // Check queue backlog threshold
            if (metrics.QueueBacklog > _options.MaxQueueBacklog)
            {
                await ApplyThrottlingAsync(queueName, throttling, 
                    $"High queue backlog: {metrics.QueueBacklog}");
                return false;
            }

            // Apply adaptive rate limiting
            var shouldProcess = await ApplyAdaptiveRateLimitingAsync(queueName, throttling);
            if (!shouldProcess)
            {
                _logger.LogDebug("Processing rejected for queue {QueueName}: Adaptive rate limiting",
                    queueName);
                return false;
            }

            return true;
        }

        public async Task RecordProcessingTimeAsync(string queueName, TimeSpan processingTime, bool success, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(queueName))
                throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));

            var metrics = GetOrCreateQueueMetrics(queueName);
            var throttling = GetOrCreateThrottling(queueName);

            // Record processing time
            metrics.ProcessingTimes.Add(new ProcessingTimeRecord
            {
                Duration = processingTime,
                Success = success,
                Timestamp = DateTime.UtcNow
            });

            // Update counters
            if (success)
            {
                Interlocked.Increment(ref metrics.SuccessCount);
                
                // Reduce throttling if consistently successful
                if (throttling.ConsecutiveSuccesses >= _options.SuccessThresholdForRecovery)
                {
                    await ReduceThrottlingAsync(queueName, throttling);
                }
                else
                {
                    Interlocked.Increment(ref throttling.ConsecutiveSuccesses);
                }
                
                Interlocked.Exchange(ref throttling.ConsecutiveFailures, 0);
            }
            else
            {
                Interlocked.Increment(ref metrics.ErrorCount);
                Interlocked.Increment(ref throttling.ConsecutiveFailures);
                Interlocked.Exchange(ref throttling.ConsecutiveSuccesses, 0);

                // Apply progressive throttling on failures
                if (throttling.ConsecutiveFailures >= _options.FailureThresholdForThrottling)
                {
                    await ApplyThrottlingAsync(queueName, throttling, 
                        $"Consecutive failures: {throttling.ConsecutiveFailures}");
                }
            }

            metrics.LastActivity = DateTime.UtcNow;

            _logger.LogDebug("Recorded processing time for queue {QueueName}: {Duration}ms, Success: {Success}",
                queueName, processingTime.TotalMilliseconds, success);
        }

        public async Task<int> GetOptimalBatchSizeAsync(string queueName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(queueName))
                throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));

            var metrics = GetOrCreateQueueMetrics(queueName);
            var throttling = GetOrCreateThrottling(queueName);

            // Start with default batch size
            var batchSize = _options.DefaultBatchSize;

            // Adjust based on current load
            var loadFactor = (double)metrics.CurrentLoad / _options.MaxConcurrentLoad;
            if (loadFactor > 0.8)
            {
                batchSize = Math.Max(1, batchSize / 2); // Reduce batch size under high load
            }
            else if (loadFactor < 0.3)
            {
                batchSize = Math.Min(_options.MaxBatchSize, batchSize * 2); // Increase batch size under low load
            }

            // Adjust based on error rate
            var errorRate = CalculateErrorRate(metrics);
            if (errorRate > _options.MaxErrorRateThreshold / 2)
            {
                batchSize = Math.Max(1, batchSize / 2);
            }

            // Adjust based on processing time
            var avgProcessingTime = CalculateAverageProcessingTime(metrics);
            if (avgProcessingTime > _options.MaxAverageProcessingTime / 2)
            {
                batchSize = Math.Max(1, batchSize / 2);
            }

            // Apply throttling adjustment
            if (throttling.IsThrottled)
            {
                batchSize = 1; // Minimal batch size when throttled
            }

            _logger.LogDebug("Optimal batch size for queue {QueueName}: {BatchSize} (load: {LoadFactor:P1}, error rate: {ErrorRate:P2})",
                queueName, batchSize, loadFactor, errorRate);

            return batchSize;
        }

        public async Task<TimeSpan> GetOptimalDelayAsync(string queueName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(queueName))
                throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));

            var metrics = GetOrCreateQueueMetrics(queueName);
            var throttling = GetOrCreateThrottling(queueName);

            // Start with minimum delay
            var delay = _options.MinProcessingDelay;

            // Increase delay based on current load
            var loadFactor = (double)metrics.CurrentLoad / _options.MaxConcurrentLoad;
            if (loadFactor > 0.5)
            {
                var additionalDelay = TimeSpan.FromMilliseconds(
                    _options.MaxProcessingDelay.TotalMilliseconds * (loadFactor - 0.5) * 2);
                delay = delay.Add(additionalDelay);
            }

            // Increase delay based on error rate
            var errorRate = CalculateErrorRate(metrics);
            if (errorRate > 0.1) // 10% error rate threshold
            {
                var errorDelayMultiplier = Math.Min(errorRate * 10, 5); // Max 5x multiplier
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * errorDelayMultiplier);
            }

            // Apply throttling delay
            if (throttling.IsThrottled)
            {
                delay = TimeSpan.FromMilliseconds(Math.Max(delay.TotalMilliseconds, throttling.CurrentThrottleDelay.TotalMilliseconds));
            }

            // Ensure delay doesn't exceed maximum
            if (delay > _options.MaxProcessingDelay)
            {
                delay = _options.MaxProcessingDelay;
            }

            _logger.LogDebug("Optimal delay for queue {QueueName}: {Delay}ms (load: {LoadFactor:P1}, error rate: {ErrorRate:P2})",
                queueName, delay.TotalMilliseconds, loadFactor, errorRate);

            return delay;
        }

        private QueueMetrics GetOrCreateQueueMetrics(string queueName)
        {
            return _queueMetrics.GetOrAdd(queueName, _ => new QueueMetrics
            {
                QueueName = queueName,
                ProcessingTimes = new ConcurrentQueue<ProcessingTimeRecord>(),
                LastActivity = DateTime.UtcNow
            });
        }

        private AdaptiveThrottling GetOrCreateThrottling(string queueName)
        {
            return _throttling.GetOrAdd(queueName, _ => new AdaptiveThrottling
            {
                QueueName = queueName,
                CurrentThrottleDelay = _options.InitialThrottleDelay,
                LastProcessingTime = DateTime.UtcNow
            });
        }

        private double CalculateErrorRate(QueueMetrics metrics)
        {
            var totalOperations = metrics.SuccessCount + metrics.ErrorCount;
            if (totalOperations == 0)
                return 0;

            return (double)metrics.ErrorCount / totalOperations;
        }

        private TimeSpan CalculateAverageProcessingTime(QueueMetrics metrics)
        {
            var recentTimes = metrics.ProcessingTimes
                .Where(pt => pt.Timestamp > DateTime.UtcNow.Subtract(_options.MetricsWindow))
                .ToList();

            if (!recentTimes.Any())
                return TimeSpan.Zero;

            var averageTicks = (long)recentTimes.Average(pt => pt.Duration.Ticks);
            return new TimeSpan(averageTicks);
        }

        private async Task ApplyThrottlingAsync(string queueName, AdaptiveThrottling throttling, string reason)
        {
            throttling.IsThrottled = true;
            throttling.ThrottleUntil = DateTime.UtcNow.Add(throttling.CurrentThrottleDelay);
            
            // Increase throttle delay for next time (exponential backoff)
            throttling.CurrentThrottleDelay = TimeSpan.FromMilliseconds(
                Math.Min(throttling.CurrentThrottleDelay.TotalMilliseconds * _options.ThrottleBackoffMultiplier,
                         _options.MaxThrottleDelay.TotalMilliseconds));

            _logger.LogWarning("Throttling applied to queue {QueueName} for {Duration}: {Reason}",
                queueName, throttling.CurrentThrottleDelay, reason);
        }

        private async Task ReduceThrottlingAsync(string queueName, AdaptiveThrottling throttling)
        {
            // Reduce throttle delay (recovery)
            throttling.CurrentThrottleDelay = TimeSpan.FromMilliseconds(
                Math.Max(throttling.CurrentThrottleDelay.TotalMilliseconds / _options.ThrottleRecoveryFactor,
                         _options.InitialThrottleDelay.TotalMilliseconds));

            _logger.LogInformation("Throttling reduced for queue {QueueName}, new delay: {Delay}ms",
                queueName, throttling.CurrentThrottleDelay.TotalMilliseconds);
        }

        private async Task<bool> ApplyAdaptiveRateLimitingAsync(string queueName, AdaptiveThrottling throttling)
        {
            var now = DateTime.UtcNow;
            var timeSinceLastProcessing = now - throttling.LastProcessingTime;

            // Calculate required delay based on current throttle settings
            var requiredDelay = throttling.CurrentThrottleDelay;
            
            if (timeSinceLastProcessing < requiredDelay)
            {
                return false; // Too soon to process
            }

            throttling.LastProcessingTime = now;
            return true;
        }

        private void CleanupOldMetrics(object state)
        {
            try
            {
                var cutoff = DateTime.UtcNow.Subtract(_options.MetricsRetentionPeriod);

                foreach (var metrics in _queueMetrics.Values)
                {
                    // Clean up old processing time records
                    while (metrics.ProcessingTimes.TryPeek(out var record) && record.Timestamp < cutoff)
                    {
                        metrics.ProcessingTimes.TryDequeue(out _);
                    }
                }

                // Remove inactive queues
                var inactiveQueues = _queueMetrics
                    .Where(kvp => kvp.Value.LastActivity < cutoff)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var queueName in inactiveQueues)
                {
                    _queueMetrics.TryRemove(queueName, out _);
                    _throttling.TryRemove(queueName, out _);
                    
                    _logger.LogDebug("Cleaned up metrics for inactive queue: {QueueName}", queueName);
                }

                if (inactiveQueues.Any())
                {
                    _logger.LogInformation("Cleaned up metrics for {Count} inactive queues", inactiveQueues.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during metrics cleanup");
            }
        }

        public void Dispose()
        {
            _metricsTimer?.Dispose();
            _logger.LogInformation("Load leveling service disposed");
        }
    }

    /// <summary>
    /// Queue metrics for load leveling decisions
    /// </summary>
    internal class QueueMetrics
    {
        public string QueueName { get; set; }
        public int CurrentLoad { get; set; }
        public long SuccessCount { get; set; }
        public long ErrorCount { get; set; }
        public long QueueBacklog { get; set; }
        public DateTime LastActivity { get; set; }
        public ConcurrentQueue<ProcessingTimeRecord> ProcessingTimes { get; set; }
    }

    /// <summary>
    /// Processing time record for metrics
    /// </summary>
    internal class ProcessingTimeRecord
    {
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Adaptive throttling state
    /// </summary>
    internal class AdaptiveThrottling
    {
        public string QueueName { get; set; }
        public bool IsThrottled { get; set; }
        public DateTime? ThrottleUntil { get; set; }
        public TimeSpan CurrentThrottleDelay { get; set; }
        public DateTime LastProcessingTime { get; set; }
        public long ConsecutiveFailures { get; set; }
        public long ConsecutiveSuccesses { get; set; }
    }

    /// <summary>
    /// Load leveling configuration options
    /// </summary>
    public class LoadLevelingOptions
    {
        public int MaxConcurrentLoad { get; set; } = 100;
        public double MaxErrorRateThreshold { get; set; } = 0.1; // 10%
        public TimeSpan MaxAverageProcessingTime { get; set; } = TimeSpan.FromSeconds(30);
        public long MaxQueueBacklog { get; set; } = 1000;
        
        public int DefaultBatchSize { get; set; } = 10;
        public int MaxBatchSize { get; set; } = 50;
        
        public TimeSpan MinProcessingDelay { get; set; } = TimeSpan.FromMilliseconds(100);
        public TimeSpan MaxProcessingDelay { get; set; } = TimeSpan.FromSeconds(30);
        
        public TimeSpan InitialThrottleDelay { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan MaxThrottleDelay { get; set; } = TimeSpan.FromMinutes(5);
        public double ThrottleBackoffMultiplier { get; set; } = 2.0;
        public double ThrottleRecoveryFactor { get; set; } = 1.5;
        
        public int FailureThresholdForThrottling { get; set; } = 5;
        public int SuccessThresholdForRecovery { get; set; } = 10;
        
        public TimeSpan MetricsWindow { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan MetricsRetentionPeriod { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan MetricsCleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
    }
}