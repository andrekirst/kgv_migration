using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.Messaging
{
    /// <summary>
    /// Background message processor implementing Queue-Based Load Leveling
    /// Processes messages from queues with adaptive load balancing and resilience
    /// </summary>
    public class MessageProcessor<T> : BackgroundService where T : class
    {
        private readonly IMessageQueue<T> _messageQueue;
        private readonly IMessageConsumer<T> _consumer;
        private readonly ILoadLevelingStrategy _loadLeveling;
        private readonly IQueueCircuitBreaker _circuitBreaker;
        private readonly ILogger<MessageProcessor<T>> _logger;
        private readonly MessageProcessorOptions _options;
        private readonly SemaphoreSlim _processingLimitSemaphore;
        private readonly CancellationTokenSource _shutdownTokenSource;
        
        private volatile int _currentLoad = 0;
        private DateTime _lastHealthCheck = DateTime.MinValue;

        public MessageProcessor(
            IMessageQueue<T> messageQueue,
            IMessageConsumer<T> consumer,
            ILoadLevelingStrategy loadLeveling,
            IQueueCircuitBreaker circuitBreaker,
            ILogger<MessageProcessor<T>> logger,
            IOptions<MessageProcessorOptions> options)
        {
            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
            _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
            _loadLeveling = loadLeveling ?? throw new ArgumentNullException(nameof(loadLeveling));
            _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _processingLimitSemaphore = new SemaphoreSlim(_options.MaxConcurrentMessages, _options.MaxConcurrentMessages);
            _shutdownTokenSource = new CancellationTokenSource();

            _logger.LogInformation("Message processor initialized for type {MessageType} with max concurrent messages: {MaxConcurrent}",
                typeof(T).Name, _options.MaxConcurrentMessages);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Message processor started for type {MessageType}", typeof(T).Name);

            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _shutdownTokenSource.Token).Token;

            // Start health monitoring task
            var healthTask = MonitorHealthAsync(combinedToken);

            // Start main processing loop
            var processingTask = ProcessMessagesAsync(combinedToken);

            try
            {
                await Task.WhenAny(healthTask, processingTask);
            }
            catch (OperationCanceledException) when (combinedToken.IsCancellationRequested)
            {
                _logger.LogInformation("Message processor stopping gracefully for type {MessageType}", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Message processor failed for type {MessageType}", typeof(T).Name);
                throw;
            }
            finally
            {
                _shutdownTokenSource.Cancel();
                await WaitForProcessingToComplete();
                _logger.LogInformation("Message processor stopped for type {MessageType}", typeof(T).Name);
            }
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check if we should process messages based on load leveling
                    var shouldProcess = await _loadLeveling.ShouldProcessAsync(
                        _options.QueueName, _currentLoad, cancellationToken);

                    if (!shouldProcess)
                    {
                        // Wait before checking again
                        var delay = await _loadLeveling.GetOptimalDelayAsync(_options.QueueName, cancellationToken);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }

                    // Check circuit breaker
                    var canExecute = await _circuitBreaker.CanExecuteAsync(_options.QueueName, cancellationToken);
                    if (!canExecute)
                    {
                        _logger.LogWarning("Circuit breaker is open for queue {QueueName}, skipping processing", _options.QueueName);
                        await Task.Delay(_options.CircuitBreakerCooldownDelay, cancellationToken);
                        continue;
                    }

                    // Get optimal batch size for current conditions
                    var batchSize = await _loadLeveling.GetOptimalBatchSizeAsync(_options.QueueName, cancellationToken);
                    
                    // Limit batch size to available semaphore slots
                    var availableSlots = _processingLimitSemaphore.CurrentCount;
                    batchSize = Math.Min(batchSize, Math.Max(1, availableSlots));

                    // Receive messages
                    var messages = await _messageQueue.ReceiveAsync(batchSize, _options.VisibilityTimeout, cancellationToken);
                    var messageList = messages.ToList();

                    if (!messageList.Any())
                    {
                        // No messages available, wait before polling again
                        await Task.Delay(_options.EmptyQueueDelay, cancellationToken);
                        continue;
                    }

                    _logger.LogDebug("Received {Count} messages from queue {QueueName}", messageList.Count, _options.QueueName);

                    // Process messages concurrently
                    var processingTasks = messageList.Select(message => ProcessMessageAsync(message, cancellationToken));
                    await Task.WhenAll(processingTasks);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in message processing loop for queue {QueueName}", _options.QueueName);
                    
                    // Record failure for circuit breaker
                    await _circuitBreaker.RecordFailureAsync(_options.QueueName, ex, cancellationToken);
                    
                    // Wait before retrying to avoid tight error loops
                    await Task.Delay(_options.ErrorRetryDelay, cancellationToken);
                }
            }
        }

        private async Task ProcessMessageAsync(QueueMessage<T> message, CancellationToken cancellationToken)
        {
            // Acquire processing slot
            await _processingLimitSemaphore.WaitAsync(cancellationToken);
            
            Interlocked.Increment(ref _currentLoad);
            var stopwatch = Stopwatch.StartNew();
            var success = false;

            try
            {
                _logger.LogDebug("Processing message {MessageId} of type {MessageType}", 
                    message.Id, typeof(T).Name);

                // Check if message has expired
                if (message.ExpiresAt.HasValue && message.ExpiresAt.Value < DateTime.UtcNow)
                {
                    _logger.LogWarning("Message {MessageId} has expired, moving to dead letter queue", message.Id);
                    await _messageQueue.DeadLetterAsync(message, "Message expired", cancellationToken);
                    return;
                }

                // Process the message
                success = await _consumer.HandleAsync(message, cancellationToken);

                if (success)
                {
                    // Complete the message (remove from queue)
                    await _messageQueue.CompleteAsync(message, cancellationToken);
                    await _circuitBreaker.RecordSuccessAsync(_options.QueueName, cancellationToken);
                    
                    _logger.LogDebug("Message {MessageId} processed successfully in {ElapsedMs}ms",
                        message.Id, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    // Abandon the message (retry later)
                    await _messageQueue.AbandonAsync(message, cancellationToken);
                    
                    _logger.LogWarning("Message {MessageId} processing failed, abandoned for retry",
                        message.Id);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Abandon message on cancellation
                await _messageQueue.AbandonAsync(message, cancellationToken);
                _logger.LogInformation("Message {MessageId} processing cancelled", message.Id);
            }
            catch (Exception ex)
            {
                success = false;
                _logger.LogError(ex, "Error processing message {MessageId}", message.Id);

                try
                {
                    // Check if we should dead letter based on delivery count or exception type
                    if (ShouldDeadLetter(message, ex))
                    {
                        await _messageQueue.DeadLetterAsync(message, ex.Message, cancellationToken);
                    }
                    else
                    {
                        await _messageQueue.AbandonAsync(message, cancellationToken);
                    }
                }
                catch (Exception abandonEx)
                {
                    _logger.LogError(abandonEx, "Failed to abandon/dead letter message {MessageId}", message.Id);
                }

                await _circuitBreaker.RecordFailureAsync(_options.QueueName, ex, cancellationToken);
            }
            finally
            {
                stopwatch.Stop();
                
                // Record processing metrics
                await _loadLeveling.RecordProcessingTimeAsync(
                    _options.QueueName, stopwatch.Elapsed, success, cancellationToken);

                Interlocked.Decrement(ref _currentLoad);
                _processingLimitSemaphore.Release();
            }
        }

        private bool ShouldDeadLetter(QueueMessage<T> message, Exception exception)
        {
            // Dead letter if max delivery count exceeded
            if (message.DeliveryCount >= _options.MaxDeliveryCount)
            {
                return true;
            }

            // Dead letter for specific exception types that shouldn't be retried
            if (exception is ArgumentException || 
                exception is InvalidOperationException ||
                exception is NotSupportedException)
            {
                return true;
            }

            // Dead letter if message is too old
            var messageAge = DateTime.UtcNow - message.EnqueuedTime;
            if (messageAge > _options.MaxMessageAge)
            {
                return true;
            }

            return false;
        }

        private async Task MonitorHealthAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_options.HealthCheckInterval, cancellationToken);

                    var now = DateTime.UtcNow;
                    if (now - _lastHealthCheck < _options.HealthCheckInterval)
                        continue;

                    _lastHealthCheck = now;

                    // Get queue statistics
                    var stats = await _messageQueue.GetStatisticsAsync(cancellationToken);
                    
                    // Log health metrics
                    _logger.LogDebug("Queue health - {QueueName}: Active: {Active}, Dead Letter: {DeadLetter}, Processing Load: {Load}",
                        _options.QueueName, stats.ActiveMessageCount, stats.DeadLetterMessageCount, _currentLoad);

                    // Check for concerning conditions
                    if (stats.DeadLetterMessageCount > _options.DeadLetterWarningThreshold)
                    {
                        _logger.LogWarning("High dead letter message count for queue {QueueName}: {Count}",
                            _options.QueueName, stats.DeadLetterMessageCount);
                    }

                    if (stats.ActiveMessageCount > _options.BacklogWarningThreshold)
                    {
                        _logger.LogWarning("High message backlog for queue {QueueName}: {Count}",
                            _options.QueueName, stats.ActiveMessageCount);
                    }

                    if (_currentLoad > _options.MaxConcurrentMessages * 0.9)
                    {
                        _logger.LogWarning("High processing load for queue {QueueName}: {Load}/{Max}",
                            _options.QueueName, _currentLoad, _options.MaxConcurrentMessages);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in health monitoring for queue {QueueName}", _options.QueueName);
                }
            }
        }

        private async Task WaitForProcessingToComplete()
        {
            var timeout = TimeSpan.FromSeconds(30);
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Waiting for {Count} messages to complete processing...", 
                _options.MaxConcurrentMessages - _processingLimitSemaphore.CurrentCount);

            while (_processingLimitSemaphore.CurrentCount < _options.MaxConcurrentMessages && 
                   stopwatch.Elapsed < timeout)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            var remainingMessages = _options.MaxConcurrentMessages - _processingLimitSemaphore.CurrentCount;
            if (remainingMessages > 0)
            {
                _logger.LogWarning("Shutdown timeout reached, {Count} messages may not have completed processing",
                    remainingMessages);
            }
            else
            {
                _logger.LogInformation("All messages completed processing during shutdown");
            }
        }

        public override void Dispose()
        {
            _shutdownTokenSource?.Cancel();
            _processingLimitSemaphore?.Dispose();
            _shutdownTokenSource?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// Message processor configuration options
    /// </summary>
    public class MessageProcessorOptions
    {
        public string QueueName { get; set; }
        public int MaxConcurrentMessages { get; set; } = 10;
        public int MaxDeliveryCount { get; set; } = 5;
        public TimeSpan VisibilityTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan MaxMessageAge { get; set; } = TimeSpan.FromDays(7);
        public TimeSpan EmptyQueueDelay { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan ErrorRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan CircuitBreakerCooldownDelay { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
        public long DeadLetterWarningThreshold { get; set; } = 100;
        public long BacklogWarningThreshold { get; set; } = 1000;
    }

    /// <summary>
    /// Generic message publisher implementation
    /// </summary>
    public class MessagePublisher : IMessagePublisher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessagePublisher> _logger;

        public MessagePublisher(IServiceProvider serviceProvider, ILogger<MessagePublisher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task PublishAsync<T>(string queueName, T message, PublishOptions options = null, CancellationToken cancellationToken = default) where T : class
        {
            var messageQueue = _serviceProvider.GetRequiredService<IMessageQueue<T>>();
            await messageQueue.SendAsync(message, options, cancellationToken);
            
            _logger.LogDebug("Published message of type {MessageType} to queue {QueueName}", 
                typeof(T).Name, queueName);
        }

        public async Task PublishBatchAsync<T>(string queueName, IEnumerable<T> messages, PublishOptions options = null, CancellationToken cancellationToken = default) where T : class
        {
            var messageQueue = _serviceProvider.GetRequiredService<IMessageQueue<T>>();
            await messageQueue.SendBatchAsync(messages, options, cancellationToken);
            
            var messageList = messages.ToList();
            _logger.LogDebug("Published batch of {Count} messages of type {MessageType} to queue {QueueName}", 
                messageList.Count, typeof(T).Name, queueName);
        }
    }

    /// <summary>
    /// JSON message serializer implementation
    /// </summary>
    public class JsonMessageSerializer : IMessageSerializer
    {
        private readonly System.Text.Json.JsonSerializerOptions _options;

        public JsonMessageSerializer()
        {
            _options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public string Serialize<T>(T message) where T : class
        {
            return System.Text.Json.JsonSerializer.Serialize(message, _options);
        }

        public T Deserialize<T>(string serializedMessage) where T : class
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(serializedMessage, _options);
        }

        public string GetContentType()
        {
            return "application/json";
        }
    }
}