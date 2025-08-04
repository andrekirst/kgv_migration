using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.Messaging
{
    /// <summary>
    /// Redis-based message queue implementation for Queue-Based Load Leveling
    /// Provides reliable message queuing with Redis List and Stream support
    /// </summary>
    public class RedisMessageQueue<T> : IMessageQueue<T> where T : class
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly ILogger<RedisMessageQueue<T>> _logger;
        private readonly RedisQueueOptions _options;
        private readonly IMessageSerializer _serializer;
        private readonly string _queueName;
        private readonly string _processingQueueName;
        private readonly string _deadLetterQueueName;
        private readonly string _statisticsKey;

        public RedisMessageQueue(
            IConnectionMultiplexer redis,
            ILogger<RedisMessageQueue<T>> logger,
            IOptions<RedisQueueOptions> options,
            IMessageSerializer serializer,
            string queueName)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));

            _database = _redis.GetDatabase(_options.DatabaseNumber);
            _processingQueueName = $"{_queueName}:processing";
            _deadLetterQueueName = $"{_queueName}:deadletter";
            _statisticsKey = $"{_queueName}:stats";

            _logger.LogDebug("Redis message queue initialized: {QueueName}", _queueName);
        }

        public async Task SendAsync(T message, QueueOptions options = null, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var queueMessage = CreateQueueMessage(message, options);
                var serializedMessage = SerializeMessage(queueMessage);

                // Add delay if specified
                if (options?.DelayUntil.HasValue == true)
                {
                    var delayedQueueName = $"{_queueName}:delayed";
                    var score = DateTimeOffset.UtcNow.Add(options.DelayUntil.Value).ToUnixTimeSeconds();
                    await _database.SortedSetAddAsync(delayedQueueName, serializedMessage, score);
                }
                else
                {
                    // Send to main queue based on priority
                    var targetQueue = GetQueueNameByPriority(queueMessage.Priority);
                    await _database.ListLeftPushAsync(targetQueue, serializedMessage);
                }

                // Update statistics
                await IncrementStatisticAsync("messages_sent");

                stopwatch.Stop();

                _logger.LogDebug("Message sent to queue {QueueName} in {ElapsedMs}ms. MessageId: {MessageId}",
                    _queueName, stopwatch.ElapsedMilliseconds, queueMessage.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to send message to queue {QueueName}", _queueName);
                throw;
            }
        }

        public async Task SendBatchAsync(IEnumerable<T> messages, QueueOptions options = null, CancellationToken cancellationToken = default)
        {
            var messageList = messages?.ToList();
            if (messageList == null || !messageList.Any())
                return;

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var batch = _database.CreateBatch();
                var tasks = new List<Task>();

                foreach (var message in messageList)
                {
                    var queueMessage = CreateQueueMessage(message, options);
                    var serializedMessage = SerializeMessage(queueMessage);

                    if (options?.DelayUntil.HasValue == true)
                    {
                        var delayedQueueName = $"{_queueName}:delayed";
                        var score = DateTimeOffset.UtcNow.Add(options.DelayUntil.Value).ToUnixTimeSeconds();
                        tasks.Add(batch.SortedSetAddAsync(delayedQueueName, serializedMessage, score));
                    }
                    else
                    {
                        var targetQueue = GetQueueNameByPriority(queueMessage.Priority);
                        tasks.Add(batch.ListLeftPushAsync(targetQueue, serializedMessage));
                    }
                }

                // Update statistics
                tasks.Add(batch.HashIncrementAsync(_statisticsKey, "messages_sent", messageList.Count));

                batch.Execute();
                await Task.WhenAll(tasks);

                stopwatch.Stop();

                _logger.LogDebug("Batch of {Count} messages sent to queue {QueueName} in {ElapsedMs}ms",
                    messageList.Count, _queueName, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to send batch of {Count} messages to queue {QueueName}",
                    messageList.Count, _queueName);
                throw;
            }
        }

        public async Task<IEnumerable<QueueMessage<T>>> ReceiveAsync(int maxMessages = 1, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
        {
            if (maxMessages <= 0)
                throw new ArgumentException("Max messages must be greater than 0", nameof(maxMessages));

            var timeout = visibilityTimeout ?? _options.DefaultVisibilityTimeout;
            var messages = new List<QueueMessage<T>>();

            try
            {
                // Process delayed messages first
                await ProcessDelayedMessages();

                // Receive messages from priority queues (high to low priority)
                var priorityQueues = new[]
                {
                    GetQueueNameByPriority(MessagePriority.Critical),
                    GetQueueNameByPriority(MessagePriority.High),
                    GetQueueNameByPriority(MessagePriority.Normal),
                    GetQueueNameByPriority(MessagePriority.Low)
                };

                foreach (var queueName in priorityQueues)
                {
                    if (messages.Count >= maxMessages)
                        break;

                    var remainingMessages = maxMessages - messages.Count;
                    var queueMessages = await ReceiveFromQueue(queueName, remainingMessages, timeout);
                    messages.AddRange(queueMessages);
                }

                // Update statistics
                if (messages.Any())
                {
                    await IncrementStatisticAsync("messages_received", messages.Count);
                }

                _logger.LogDebug("Received {Count} messages from queue {QueueName}", messages.Count, _queueName);

                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive messages from queue {QueueName}", _queueName);
                throw;
            }
        }

        public async Task CompleteAsync(QueueMessage<T> message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                // Remove from processing queue
                var serializedMessage = SerializeMessage(message);
                await _database.ListRemoveAsync(_processingQueueName, serializedMessage);

                // Update statistics
                await IncrementStatisticAsync("messages_completed");

                _logger.LogDebug("Message completed: {MessageId} from queue {QueueName}",
                    message.Id, _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete message {MessageId} from queue {QueueName}",
                    message.Id, _queueName);
                throw;
            }
        }

        public async Task AbandonAsync(QueueMessage<T> message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                // Remove from processing queue
                var serializedMessage = SerializeMessage(message);
                await _database.ListRemoveAsync(_processingQueueName, serializedMessage);

                // Increment delivery count
                message.DeliveryCount++;

                // Check if should be dead lettered
                if (message.DeliveryCount >= _options.MaxDeliveryCount)
                {
                    await DeadLetterAsync(message, "Max delivery count exceeded", cancellationToken);
                    return;
                }

                // Return to appropriate queue with backoff delay
                var delay = CalculateBackoffDelay(message.DeliveryCount);
                var delayedQueueName = $"{_queueName}:delayed";
                var score = DateTimeOffset.UtcNow.Add(delay).ToUnixTimeSeconds();
                var updatedSerializedMessage = SerializeMessage(message);

                await _database.SortedSetAddAsync(delayedQueueName, updatedSerializedMessage, score);

                // Update statistics
                await IncrementStatisticAsync("messages_abandoned");

                _logger.LogDebug("Message abandoned: {MessageId} from queue {QueueName}, delivery count: {DeliveryCount}",
                    message.Id, _queueName, message.DeliveryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to abandon message {MessageId} from queue {QueueName}",
                    message.Id, _queueName);
                throw;
            }
        }

        public async Task DeadLetterAsync(QueueMessage<T> message, string reason = null, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                // Remove from processing queue
                var serializedMessage = SerializeMessage(message);
                await _database.ListRemoveAsync(_processingQueueName, serializedMessage);

                // Add reason to message properties
                if (!string.IsNullOrEmpty(reason))
                {
                    message.Properties["DeadLetterReason"] = reason;
                    message.Properties["DeadLetterTime"] = DateTime.UtcNow;
                }

                // Move to dead letter queue
                var deadLetterMessage = SerializeMessage(message);
                await _database.ListLeftPushAsync(_deadLetterQueueName, deadLetterMessage);

                // Update statistics
                await IncrementStatisticAsync("messages_deadlettered");

                _logger.LogWarning("Message dead lettered: {MessageId} from queue {QueueName}. Reason: {Reason}",
                    message.Id, _queueName, reason ?? "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dead letter message {MessageId} from queue {QueueName}",
                    message.Id, _queueName);
                throw;
            }
        }

        public async Task<QueueStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = await _database.HashGetAllAsync(_statisticsKey);
                var statsDict = stats.ToDictionary(x => x.Name.ToString(), x => (long)x.Value);

                var queueLengths = await GetQueueLengthsAsync();

                return new QueueStatistics
                {
                    QueueName = _queueName,
                    ActiveMessageCount = queueLengths.Values.Sum(),
                    DeadLetterMessageCount = await _database.ListLengthAsync(_deadLetterQueueName),
                    TotalMessagesSent = statsDict.GetValueOrDefault("messages_sent", 0),
                    TotalMessagesReceived = statsDict.GetValueOrDefault("messages_received", 0),
                    TotalMessagesCompleted = statsDict.GetValueOrDefault("messages_completed", 0),
                    TotalMessagesAbandoned = statsDict.GetValueOrDefault("messages_abandoned", 0),
                    LastUpdated = DateTime.UtcNow,
                    AdditionalMetrics = new Dictionary<string, object>
                    {
                        { "processing_queue_length", await _database.ListLengthAsync(_processingQueueName) },
                        { "delayed_message_count", await _database.SortedSetLengthAsync($"{_queueName}:delayed") },
                        { "queue_lengths_by_priority", queueLengths }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get statistics for queue {QueueName}", _queueName);
                throw;
            }
        }

        public async Task PurgeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var tasks = new[]
                {
                    _database.KeyDeleteAsync(GetQueueNameByPriority(MessagePriority.Critical)),
                    _database.KeyDeleteAsync(GetQueueNameByPriority(MessagePriority.High)),
                    _database.KeyDeleteAsync(GetQueueNameByPriority(MessagePriority.Normal)),
                    _database.KeyDeleteAsync(GetQueueNameByPriority(MessagePriority.Low)),
                    _database.KeyDeleteAsync(_processingQueueName),
                    _database.KeyDeleteAsync($"{_queueName}:delayed"),
                    _database.KeyDeleteAsync(_statisticsKey)
                };

                await Task.WhenAll(tasks);

                _logger.LogInformation("Queue {QueueName} purged", _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to purge queue {QueueName}", _queueName);
                throw;
            }
        }

        private QueueMessage<T> CreateQueueMessage(T message, QueueOptions options)
        {
            var queueMessage = new QueueMessage<T>
            {
                Id = Guid.NewGuid().ToString(),
                Body = message,
                EnqueuedTime = DateTime.UtcNow,
                DeliveryCount = 0,
                Priority = options?.Priority ?? MessagePriority.Normal,
                CorrelationId = options?.CorrelationId,
                ReplyTo = options?.ReplyTo,
                Label = options?.Label,
                TimeToLive = options?.TimeToLive
            };

            if (options?.TimeToLive.HasValue == true)
            {
                queueMessage.ExpiresAt = DateTime.UtcNow.Add(options.TimeToLive.Value);
            }

            if (options?.Properties != null)
            {
                foreach (var prop in options.Properties)
                {
                    queueMessage.Properties[prop.Key] = prop.Value;
                }
            }

            return queueMessage;
        }

        private string SerializeMessage(QueueMessage<T> message)
        {
            var wrapper = new MessageWrapper
            {
                Id = message.Id,
                Body = _serializer.Serialize(message.Body),
                Properties = message.Properties,
                EnqueuedTime = message.EnqueuedTime,
                DequeueTime = message.DequeueTime,
                DeliveryCount = message.DeliveryCount,
                TimeToLive = message.TimeToLive,
                ExpiresAt = message.ExpiresAt,
                CorrelationId = message.CorrelationId,
                ReplyTo = message.ReplyTo,
                Label = message.Label,
                Priority = message.Priority
            };

            return JsonSerializer.Serialize(wrapper);
        }

        private QueueMessage<T> DeserializeMessage(string serializedMessage)
        {
            var wrapper = JsonSerializer.Deserialize<MessageWrapper>(serializedMessage);
            
            return new QueueMessage<T>
            {
                Id = wrapper.Id,
                Body = _serializer.Deserialize<T>(wrapper.Body),
                Properties = wrapper.Properties ?? new Dictionary<string, object>(),
                EnqueuedTime = wrapper.EnqueuedTime,
                DequeueTime = wrapper.DequeueTime,
                DeliveryCount = wrapper.DeliveryCount,
                TimeToLive = wrapper.TimeToLive,
                ExpiresAt = wrapper.ExpiresAt,
                CorrelationId = wrapper.CorrelationId,
                ReplyTo = wrapper.ReplyTo,
                Label = wrapper.Label,
                Priority = wrapper.Priority
            };
        }

        private string GetQueueNameByPriority(MessagePriority priority)
        {
            return priority switch
            {
                MessagePriority.Critical => $"{_queueName}:critical",
                MessagePriority.High => $"{_queueName}:high",
                MessagePriority.Normal => _queueName,
                MessagePriority.Low => $"{_queueName}:low",
                _ => _queueName
            };
        }

        private async Task ProcessDelayedMessages()
        {
            try
            {
                var delayedQueueName = $"{_queueName}:delayed";
                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Get messages ready for processing
                var readyMessages = await _database.SortedSetRangeByScoreAsync(
                    delayedQueueName, 0, currentTime, take: _options.MaxBatchSize);

                if (readyMessages.Length > 0)
                {
                    var batch = _database.CreateBatch();
                    var tasks = new List<Task>();

                    foreach (var serializedMessage in readyMessages)
                    {
                        // Remove from delayed queue
                        tasks.Add(batch.SortedSetRemoveAsync(delayedQueueName, serializedMessage));

                        // Deserialize to get priority
                        var message = DeserializeMessage(serializedMessage);
                        var targetQueue = GetQueueNameByPriority(message.Priority);

                        // Add to appropriate priority queue
                        tasks.Add(batch.ListLeftPushAsync(targetQueue, serializedMessage));
                    }

                    batch.Execute();
                    await Task.WhenAll(tasks);

                    _logger.LogDebug("Processed {Count} delayed messages for queue {QueueName}",
                        readyMessages.Length, _queueName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process delayed messages for queue {QueueName}", _queueName);
            }
        }

        private async Task<List<QueueMessage<T>>> ReceiveFromQueue(string queueName, int maxMessages, TimeSpan visibilityTimeout)
        {
            var messages = new List<QueueMessage<T>>();

            for (int i = 0; i < maxMessages; i++)
            {
                // Use BRPOPLPUSH for atomic move from queue to processing queue
                var serializedMessage = await _database.ListRightPopLeftPushAsync(queueName, _processingQueueName);
                
                if (!serializedMessage.HasValue)
                    break;

                try
                {
                    var message = DeserializeMessage(serializedMessage);
                    message.DequeueTime = DateTime.UtcNow;
                    message.PopReceipt = Guid.NewGuid().ToString();

                    // Check if message has expired
                    if (message.ExpiresAt.HasValue && message.ExpiresAt.Value < DateTime.UtcNow)
                    {
                        await DeadLetterAsync(message, "Message expired");
                        continue;
                    }

                    messages.Add(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize message from queue {QueueName}: {Message}",
                        queueName, serializedMessage.ToString());
                    
                    // Move malformed message to dead letter queue
                    await _database.ListLeftPushAsync(_deadLetterQueueName, serializedMessage);
                }
            }

            return messages;
        }

        private async Task<Dictionary<string, long>> GetQueueLengthsAsync()
        {
            var tasks = new Dictionary<string, Task<long>>
            {
                { "critical", _database.ListLengthAsync(GetQueueNameByPriority(MessagePriority.Critical)) },
                { "high", _database.ListLengthAsync(GetQueueNameByPriority(MessagePriority.High)) },
                { "normal", _database.ListLengthAsync(GetQueueNameByPriority(MessagePriority.Normal)) },
                { "low", _database.ListLengthAsync(GetQueueNameByPriority(MessagePriority.Low)) }
            };

            await Task.WhenAll(tasks.Values);

            return tasks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Result);
        }

        private TimeSpan CalculateBackoffDelay(int deliveryCount)
        {
            var baseDelay = _options.RetryPolicy.InitialDelay;
            
            return _options.RetryPolicy.BackoffType switch
            {
                RetryBackoffType.Fixed => baseDelay,
                RetryBackoffType.Linear => TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * deliveryCount),
                RetryBackoffType.Exponential => TimeSpan.FromMilliseconds(
                    Math.Min(baseDelay.TotalMilliseconds * Math.Pow(_options.RetryPolicy.BackoffMultiplier, deliveryCount - 1),
                             _options.RetryPolicy.MaxDelay.TotalMilliseconds)),
                _ => baseDelay
            };
        }

        private async Task IncrementStatisticAsync(string key, long increment = 1)
        {
            try
            {
                await _database.HashIncrementAsync(_statisticsKey, key, increment);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update statistics for queue {QueueName}", _queueName);
            }
        }
    }

    /// <summary>
    /// Message wrapper for serialization
    /// </summary>
    internal class MessageWrapper
    {
        public string Id { get; set; }
        public string Body { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public DateTime EnqueuedTime { get; set; }
        public DateTime? DequeueTime { get; set; }
        public int DeliveryCount { get; set; }
        public TimeSpan? TimeToLive { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string CorrelationId { get; set; }
        public string ReplyTo { get; set; }
        public string Label { get; set; }
        public MessagePriority Priority { get; set; }
    }

    /// <summary>
    /// Redis queue configuration options
    /// </summary>
    public class RedisQueueOptions
    {
        public int DatabaseNumber { get; set; } = 0;
        public TimeSpan DefaultVisibilityTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public int MaxDeliveryCount { get; set; } = 5;
        public int MaxBatchSize { get; set; } = 32;
        public RetryPolicy RetryPolicy { get; set; } = new RetryPolicy();
        public TimeSpan StatisticsUpdateInterval { get; set; } = TimeSpan.FromSeconds(30);
    }
}