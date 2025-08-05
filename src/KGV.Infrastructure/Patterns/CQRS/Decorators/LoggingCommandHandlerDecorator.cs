using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KGV.Infrastructure.Patterns.CQRS.Decorators
{
    /// <summary>
    /// Logging decorator for command handlers without return value
    /// Provides detailed logging for command execution
    /// </summary>
    /// <typeparam name="TCommand">The type of command to handle</typeparam>
    public class LoggingCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> _inner;
        private readonly ILogger<LoggingCommandHandlerDecorator<TCommand>> _logger;

        public LoggingCommandHandlerDecorator(
            ICommandHandler<TCommand> inner,
            ILogger<LoggingCommandHandlerDecorator<TCommand>> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var commandType = typeof(TCommand).Name;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting execution of command {CommandType} with correlation ID {CorrelationId} by user {UserId}",
                    commandType, command.CorrelationId, command.UserId);

                await _inner.HandleAsync(command, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation("Successfully executed command {CommandType} in {ElapsedMs}ms",
                    commandType, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex, "Command {CommandType} failed after {ElapsedMs}ms. Correlation ID: {CorrelationId}",
                    commandType, stopwatch.ElapsedMilliseconds, command.CorrelationId);

                throw;
            }
        }
    }

    /// <summary>
    /// Logging decorator for command handlers with return value
    /// Provides detailed logging for command execution
    /// </summary>
    /// <typeparam name="TCommand">The type of command to handle</typeparam>
    /// <typeparam name="TResult">The type of result returned</typeparam>
    public class LoggingCommandHandlerDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        private readonly ICommandHandler<TCommand, TResult> _inner;
        private readonly ILogger<LoggingCommandHandlerDecorator<TCommand, TResult>> _logger;

        public LoggingCommandHandlerDecorator(
            ICommandHandler<TCommand, TResult> inner,
            ILogger<LoggingCommandHandlerDecorator<TCommand, TResult>> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var commandType = typeof(TCommand).Name;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting execution of command {CommandType} with correlation ID {CorrelationId} by user {UserId}",
                    commandType, command.CorrelationId, command.UserId);

                var result = await _inner.HandleAsync(command, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation("Successfully executed command {CommandType} in {ElapsedMs}ms with result type {ResultType}",
                    commandType, stopwatch.ElapsedMilliseconds, typeof(TResult).Name);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex, "Command {CommandType} failed after {ElapsedMs}ms. Correlation ID: {CorrelationId}",
                    commandType, stopwatch.ElapsedMilliseconds, command.CorrelationId);

                throw;
            }
        }
    }
}