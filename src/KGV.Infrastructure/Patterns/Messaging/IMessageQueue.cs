using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.Messaging
{
    /// <summary>
    /// Message queue interface for Queue-Based Load Leveling pattern
    /// Provides asynchronous message processing and load distribution
    /// </summary>
    public interface IMessageQueue<T> where T : class
    {
        /// <summary>
        /// Send a message to the queue
        /// </summary>
        Task SendAsync(T message, QueueOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send multiple messages as a batch
        /// </summary>
        Task SendBatchAsync(IEnumerable<T> messages, QueueOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Receive messages from the queue
        /// </summary>
        Task<IEnumerable<QueueMessage<T>>> ReceiveAsync(int maxMessages = 1, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Complete processing of a message (remove from queue)
        /// </summary>
        Task CompleteAsync(QueueMessage<T> message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Abandon message processing (return to queue)
        /// </summary>
        Task AbandonAsync(QueueMessage<T> message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dead letter a message (move to dead letter queue)
        /// </summary>
        Task DeadLetterAsync(QueueMessage<T> message, string reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get queue statistics
        /// </summary>
        Task<QueueStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Purge all messages from the queue
        /// </summary>
        Task PurgeAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Generic message publisher interface
    /// </summary>
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(string queueName, T message, PublishOptions options = null, CancellationToken cancellationToken = default) where T : class;
        Task PublishBatchAsync<T>(string queueName, IEnumerable<T> messages, PublishOptions options = null, CancellationToken cancellationToken = default) where T : class;
    }

    /// <summary>
    /// Message consumer interface for background processing
    /// </summary>
    public interface IMessageConsumer<T> where T : class
    {
        Task<bool> HandleAsync(QueueMessage<T> message, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Queue message wrapper
    /// </summary>
    public class QueueMessage<T> where T : class
    {
        public string Id { get; set; }
        public T Body { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public DateTime EnqueuedTime { get; set; }
        public DateTime? DequeueTime { get; set; }
        public int DeliveryCount { get; set; }
        public string PopReceipt { get; set; }
        public TimeSpan? TimeToLive { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string CorrelationId { get; set; }
        public string ReplyTo { get; set; }
        public string Label { get; set; }
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;
    }

    /// <summary>
    /// Queue options for sending messages
    /// </summary>
    public class QueueOptions
    {
        public TimeSpan? DelayUntil { get; set; }
        public TimeSpan? TimeToLive { get; set; }
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;
        public string CorrelationId { get; set; }
        public string ReplyTo { get; set; }
        public string Label { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Publish options for message publishing
    /// </summary>
    public class PublishOptions : QueueOptions
    {
        public bool RequireConfirmation { get; set; } = false;
        public string RoutingKey { get; set; }
        public string Exchange { get; set; }
    }

    /// <summary>
    /// Message priority levels
    /// </summary>
    public enum MessagePriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Queue statistics for monitoring
    /// </summary>
    public class QueueStatistics
    {
        public string QueueName { get; set; }
        public long ActiveMessageCount { get; set; }
        public long DeadLetterMessageCount { get; set; }
        public long TotalMessagesSent { get; set; }
        public long TotalMessagesReceived { get; set; }
        public long TotalMessagesCompleted { get; set; }
        public long TotalMessagesAbandoned { get; set; }
        public double AverageProcessingTime { get; set; }
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }

    /// <summary>
    /// Queue health status
    /// </summary>
    public class QueueHealth
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; }
        public long MessageBacklog { get; set; }
        public double ProcessingRate { get; set; }
        public DateTime LastMessageProcessed { get; set; }
        public List<string> Issues { get; set; } = new();
    }

    /// <summary>
    /// Load leveling strategy interface
    /// </summary>
    public interface ILoadLevelingStrategy
    {
        Task<bool> ShouldProcessAsync(string queueName, int currentLoad, CancellationToken cancellationToken = default);
        Task RecordProcessingTimeAsync(string queueName, TimeSpan processingTime, bool success, CancellationToken cancellationToken = default);
        Task<int> GetOptimalBatchSizeAsync(string queueName, CancellationToken cancellationToken = default);
        Task<TimeSpan> GetOptimalDelayAsync(string queueName, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Circuit breaker for queue operations
    /// </summary>
    public interface IQueueCircuitBreaker
    {
        Task<bool> CanExecuteAsync(string queueName, CancellationToken cancellationToken = default);
        Task RecordSuccessAsync(string queueName, CancellationToken cancellationToken = default);
        Task RecordFailureAsync(string queueName, Exception exception, CancellationToken cancellationToken = default);
        Task<CircuitBreakerState> GetStateAsync(string queueName, CancellationToken cancellationToken = default);
    }

    public enum CircuitBreakerState
    {
        Closed,    // Normal operation
        Open,      // Failing, requests rejected
        HalfOpen   // Testing if service recovered
    }

    /// <summary>
    /// Message retry policy
    /// </summary>
    public class RetryPolicy
    {
        public int MaxRetryCount { get; set; } = 3;
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);
        public RetryBackoffType BackoffType { get; set; } = RetryBackoffType.Exponential;
        public double BackoffMultiplier { get; set; } = 2.0;
        public Func<Exception, bool> ShouldRetry { get; set; } = ex => true;
    }

    public enum RetryBackoffType
    {
        Fixed,
        Linear,
        Exponential
    }

    /// <summary>
    /// Batch processing options
    /// </summary>
    public class BatchProcessingOptions
    {
        public int MaxBatchSize { get; set; } = 10;
        public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxConcurrentBatches { get; set; } = 5;
        public bool EnableDynamicBatching { get; set; } = true;
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Message serialization interface
    /// </summary>
    public interface IMessageSerializer
    {
        string Serialize<T>(T message) where T : class;
        T Deserialize<T>(string serializedMessage) where T : class;
        string GetContentType();
    }

    /// <summary>
    /// Queue monitoring interface
    /// </summary>
    public interface IQueueMonitor
    {
        Task<QueueHealth> CheckHealthAsync(string queueName, CancellationToken cancellationToken = default);
        Task<QueueStatistics> GetStatisticsAsync(string queueName, CancellationToken cancellationToken = default);
        Task<IEnumerable<QueueAlert>> GetAlertsAsync(CancellationToken cancellationToken = default);
        event Action<QueueAlert> AlertRaised;
    }

    /// <summary>
    /// Queue alert for monitoring
    /// </summary>
    public class QueueAlert
    {
        public string QueueName { get; set; }
        public QueueAlertType Type { get; set; }
        public string Message { get; set; }
        public QueueAlertSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    public enum QueueAlertType
    {
        HighMessageCount,
        SlowProcessing,
        ConsumerDown,
        DeadLetterMessages,
        CircuitBreakerOpen,
        ConnectionFailure
    }

    public enum QueueAlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}