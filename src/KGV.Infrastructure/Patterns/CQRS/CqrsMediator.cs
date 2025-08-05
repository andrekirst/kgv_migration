using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.CQRS
{
    /// <summary>
    /// CQRS Mediator implementation with performance monitoring and validation
    /// Provides centralized routing for commands and queries
    /// </summary>
    public class CqrsMediator : ICqrsMediator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CqrsMediator> _logger;
        private readonly ICqrsMetrics _metrics;

        public CqrsMediator(
            IServiceProvider serviceProvider,
            ILogger<CqrsMediator> logger,
            ICqrsMetrics metrics)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _metrics = metrics;
        }

        public async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var stopwatch = Stopwatch.StartNew();
            var commandType = typeof(TCommand);
            var commandName = commandType.Name;

            try
            {
                _logger.LogInformation("Executing command {CommandName} with correlation ID {CorrelationId}",
                    commandName, command.CorrelationId);

                // Validate command
                await ValidateCommand(command);

                // Get handler
                var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
                if (handler == null)
                {
                    throw new InvalidOperationException($"No handler registered for command {commandName}");
                }

                // Execute command
                await handler.HandleAsync(command, cancellationToken);

                stopwatch.Stop();
                
                _metrics.RecordCommandExecution(commandName, stopwatch.ElapsedMilliseconds, true);

                _logger.LogInformation("Command {CommandName} executed successfully in {ElapsedMs}ms",
                    commandName, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _metrics.RecordCommandExecution(commandName, stopwatch.ElapsedMilliseconds, false);

                _logger.LogError(ex, "Command {CommandName} failed after {ElapsedMs}ms. Correlation ID: {CorrelationId}",
                    commandName, stopwatch.ElapsedMilliseconds, command.CorrelationId);

                throw;
            }
        }

        public async Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var stopwatch = Stopwatch.StartNew();
            var commandType = typeof(TCommand);
            var commandName = commandType.Name;

            try
            {
                _logger.LogInformation("Executing command {CommandName} with correlation ID {CorrelationId}",
                    commandName, command.CorrelationId);

                // Validate command
                await ValidateCommand(command);

                // Get handler
                var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
                if (handler == null)
                {
                    throw new InvalidOperationException($"No handler registered for command {commandName}");
                }

                // Execute command
                var result = await handler.HandleAsync(command, cancellationToken);

                stopwatch.Stop();
                
                _metrics.RecordCommandExecution(commandName, stopwatch.ElapsedMilliseconds, true);

                _logger.LogInformation("Command {CommandName} executed successfully in {ElapsedMs}ms",
                    commandName, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _metrics.RecordCommandExecution(commandName, stopwatch.ElapsedMilliseconds, false);

                _logger.LogError(ex, "Command {CommandName} failed after {ElapsedMs}ms. Correlation ID: {CorrelationId}",
                    commandName, stopwatch.ElapsedMilliseconds, command.CorrelationId);

                throw;
            }
        }

        public async Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var stopwatch = Stopwatch.StartNew();
            var queryType = typeof(TQuery);
            var queryName = queryType.Name;

            try
            {
                _logger.LogDebug("Executing query {QueryName} with correlation ID {CorrelationId}",
                    queryName, query.CorrelationId);

                // Validate query
                await ValidateQuery<TQuery, TResult>(query);

                // Get handler
                var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
                if (handler == null)
                {
                    throw new InvalidOperationException($"No handler registered for query {queryName}");
                }

                // Execute query
                var result = await handler.HandleAsync(query, cancellationToken);

                stopwatch.Stop();
                
                _metrics.RecordQueryExecution(queryName, stopwatch.ElapsedMilliseconds, true);

                _logger.LogDebug("Query {QueryName} executed successfully in {ElapsedMs}ms",
                    queryName, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _metrics.RecordQueryExecution(queryName, stopwatch.ElapsedMilliseconds, false);

                _logger.LogError(ex, "Query {QueryName} failed after {ElapsedMs}ms. Correlation ID: {CorrelationId}",
                    queryName, stopwatch.ElapsedMilliseconds, query.CorrelationId);

                throw;
            }
        }

        private async Task ValidateCommand<TCommand>(TCommand command) where TCommand : ICommand
        {
            // Basic validation
            if (string.IsNullOrEmpty(command.CorrelationId))
            {
                command.CorrelationId = Guid.NewGuid().ToString();
                _logger.LogWarning("Command {CommandType} had no correlation ID, generated: {CorrelationId}",
                    typeof(TCommand).Name, command.CorrelationId);
            }

            if (command.Timestamp == default)
            {
                command.Timestamp = DateTime.UtcNow;
                _logger.LogWarning("Command {CommandType} had no timestamp, set to: {Timestamp}",
                    typeof(TCommand).Name, command.Timestamp);
            }

            // Custom validation can be added here
            var validator = _serviceProvider.GetService<ICommandValidator<TCommand>>();
            if (validator != null)
            {
                var validationResult = await validator.ValidateAsync(command);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors);
                    throw new ValidationException($"Command validation failed: {errors}");
                }
            }
        }

        private async Task ValidateQuery<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
        {
            // Basic validation
            if (string.IsNullOrEmpty(query.CorrelationId))
            {
                query.CorrelationId = Guid.NewGuid().ToString();
                _logger.LogWarning("Query {QueryType} had no correlation ID, generated: {CorrelationId}",
                    typeof(TQuery).Name, query.CorrelationId);
            }

            if (query.Timestamp == default)
            {
                query.Timestamp = DateTime.UtcNow;
                _logger.LogWarning("Query {QueryType} had no timestamp, set to: {Timestamp}",
                    typeof(TQuery).Name, query.Timestamp);
            }

            // Custom validation can be added here
            var validator = _serviceProvider.GetService<IQueryValidator<TQuery>>();
            if (validator != null)
            {
                var validationResult = await validator.ValidateAsync(query);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors);
                    throw new ValidationException($"Query validation failed: {errors}");
                }
            }
        }
    }

    /// <summary>
    /// Metrics collection interface for CQRS operations
    /// </summary>
    public interface ICqrsMetrics
    {
        void RecordCommandExecution(string commandName, long elapsedMilliseconds, bool success);
        void RecordQueryExecution(string queryName, long elapsedMilliseconds, bool success);
        void RecordValidationFailure(string operationType, string operationName);
    }

    /// <summary>
    /// Default implementation of CQRS metrics collection
    /// </summary>
    public class CqrsMetrics : ICqrsMetrics
    {
        private readonly ILogger<CqrsMetrics> _logger;

        public CqrsMetrics(ILogger<CqrsMetrics> logger)
        {
            _logger = logger;
        }

        public void RecordCommandExecution(string commandName, long elapsedMilliseconds, bool success)
        {
            _logger.LogInformation("CQRS Command Metric: {CommandName} - {ElapsedMs}ms - Success: {Success}",
                commandName, elapsedMilliseconds, success);

            // TODO: Send to metrics backend (Prometheus, Application Insights, etc.)
        }

        public void RecordQueryExecution(string queryName, long elapsedMilliseconds, bool success)
        {
            _logger.LogDebug("CQRS Query Metric: {QueryName} - {ElapsedMs}ms - Success: {Success}",
                queryName, elapsedMilliseconds, success);

            // TODO: Send to metrics backend
        }

        public void RecordValidationFailure(string operationType, string operationName)
        {
            _logger.LogWarning("CQRS Validation Failure: {OperationType} {OperationName}",
                operationType, operationName);

            // TODO: Send to metrics backend
        }
    }

    /// <summary>
    /// Validation interfaces for commands and queries
    /// </summary>
    public interface ICommandValidator<in TCommand> where TCommand : ICommand
    {
        Task<ValidationResult> ValidateAsync(TCommand command);
    }

    public interface IQueryValidator<in TQuery>
    {
        Task<ValidationResult> ValidateAsync(TQuery query);
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public System.Collections.Generic.List<string> Errors { get; set; } = new();

        public static ValidationResult Success() => new() { IsValid = true };
        
        public static ValidationResult Failure(params string[] errors) => new()
        {
            IsValid = false,
            Errors = new System.Collections.Generic.List<string>(errors)
        };
    }

    /// <summary>
    /// Custom exception for validation failures
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}