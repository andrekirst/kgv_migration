using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KGV.Infrastructure.Patterns.CQRS.Decorators
{
    /// <summary>
    /// Retry configuration options
    /// </summary>
    public class RetryOptions
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
        public double BackoffMultiplier { get; set; } = 2.0;
    }

    /// <summary>
    /// Retry decorator for command handlers without return value
    /// Implements exponential backoff retry logic
    /// </summary>
    /// <typeparam name="TCommand">The type of command to handle</typeparam>
    public class RetryCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> _inner;
        private readonly ILogger<RetryCommandHandlerDecorator<TCommand>> _logger;
        private readonly RetryOptions _retryOptions;

        public RetryCommandHandlerDecorator(
            ICommandHandler<TCommand> inner,
            ILogger<RetryCommandHandlerDecorator<TCommand>> logger,
            IOptions<RetryOptions> retryOptions = null)
        {
            _inner = inner;
            _logger = logger;
            _retryOptions = retryOptions?.Value ?? new RetryOptions();
        }

        public async Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var commandType = typeof(TCommand).Name;
            var attempt = 0;
            var delay = _retryOptions.BaseDelay;

            while (attempt <= _retryOptions.MaxRetryAttempts)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation("Retrying command {CommandType} (attempt {Attempt}/{MaxAttempts}) after delay of {DelayMs}ms",
                            commandType, attempt, _retryOptions.MaxRetryAttempts, delay.TotalMilliseconds);

                        await Task.Delay(delay, cancellationToken);
                    }

                    await _inner.HandleAsync(command, cancellationToken);
                    
                    if (attempt > 0)
                    {
                        _logger.LogInformation("Command {CommandType} succeeded on retry attempt {Attempt}",
                            commandType, attempt);
                    }

                    return; // Success
                }
                catch (Exception ex) when (attempt < _retryOptions.MaxRetryAttempts && IsRetriableException(ex))
                {
                    attempt++;
                    delay = TimeSpan.FromMilliseconds(Math.Min(
                        delay.TotalMilliseconds * _retryOptions.BackoffMultiplier,
                        _retryOptions.MaxDelay.TotalMilliseconds));

                    _logger.LogWarning(ex, "Command {CommandType} failed on attempt {Attempt}, will retry. Correlation ID: {CorrelationId}",
                        commandType, attempt, command.CorrelationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command {CommandType} failed after {Attempts} attempts. Correlation ID: {CorrelationId}",
                        commandType, attempt + 1, command.CorrelationId);
                    throw;
                }
            }
        }

        private static bool IsRetriableException(Exception exception)
        {
            // Define which exceptions are retriable
            return exception switch
            {
                TimeoutException => true,
                TaskCanceledException => false, // Don't retry cancellation
                OperationCanceledException => false, // Don't retry cancellation
                ArgumentException => false, // Don't retry validation errors
                InvalidOperationException => false, // Don't retry invalid operations
                _ => true // Retry other exceptions (network, transient database errors, etc.)
            };
        }
    }

    /// <summary>
    /// Retry decorator for command handlers with return value
    /// Implements exponential backoff retry logic
    /// </summary>
    /// <typeparam name="TCommand">The type of command to handle</typeparam>
    /// <typeparam name="TResult">The type of result returned</typeparam>
    public class RetryCommandHandlerDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        private readonly ICommandHandler<TCommand, TResult> _inner;
        private readonly ILogger<RetryCommandHandlerDecorator<TCommand, TResult>> _logger;
        private readonly RetryOptions _retryOptions;

        public RetryCommandHandlerDecorator(
            ICommandHandler<TCommand, TResult> inner,
            ILogger<RetryCommandHandlerDecorator<TCommand, TResult>> logger,
            IOptions<RetryOptions> retryOptions = null)
        {
            _inner = inner;
            _logger = logger;
            _retryOptions = retryOptions?.Value ?? new RetryOptions();
        }

        public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var commandType = typeof(TCommand).Name;
            var attempt = 0;
            var delay = _retryOptions.BaseDelay;

            while (attempt <= _retryOptions.MaxRetryAttempts)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation("Retrying command {CommandType} (attempt {Attempt}/{MaxAttempts}) after delay of {DelayMs}ms",
                            commandType, attempt, _retryOptions.MaxRetryAttempts, delay.TotalMilliseconds);

                        await Task.Delay(delay, cancellationToken);
                    }

                    var result = await _inner.HandleAsync(command, cancellationToken);
                    
                    if (attempt > 0)
                    {
                        _logger.LogInformation("Command {CommandType} succeeded on retry attempt {Attempt}",
                            commandType, attempt);
                    }

                    return result; // Success
                }
                catch (Exception ex) when (attempt < _retryOptions.MaxRetryAttempts && IsRetriableException(ex))
                {
                    attempt++;
                    delay = TimeSpan.FromMilliseconds(Math.Min(
                        delay.TotalMilliseconds * _retryOptions.BackoffMultiplier,
                        _retryOptions.MaxDelay.TotalMilliseconds));

                    _logger.LogWarning(ex, "Command {CommandType} failed on attempt {Attempt}, will retry. Correlation ID: {CorrelationId}",
                        commandType, attempt, command.CorrelationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command {CommandType} failed after {Attempts} attempts. Correlation ID: {CorrelationId}",
                        commandType, attempt + 1, command.CorrelationId);
                    throw;
                }
            }

            // This should never be reached, but compiler requires it
            throw new InvalidOperationException($"Command {commandType} exhausted all retry attempts");
        }

        private static bool IsRetriableException(Exception exception)
        {
            // Define which exceptions are retriable
            return exception switch
            {
                TimeoutException => true,
                TaskCanceledException => false, // Don't retry cancellation
                OperationCanceledException => false, // Don't retry cancellation
                ArgumentException => false, // Don't retry validation errors
                InvalidOperationException => false, // Don't retry invalid operations
                _ => true // Retry other exceptions (network, transient database errors, etc.)
            };
        }
    }
}