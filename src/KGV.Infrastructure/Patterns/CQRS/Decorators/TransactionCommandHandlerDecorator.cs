using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;

namespace KGV.Infrastructure.Patterns.CQRS.Decorators
{
    /// <summary>
    /// Transaction decorator for command handlers without return value
    /// Wraps command execution in a transaction scope
    /// </summary>
    /// <typeparam name="TCommand">The type of command to handle</typeparam>
    public class TransactionCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> _inner;
        private readonly ILogger<TransactionCommandHandlerDecorator<TCommand>> _logger;

        public TransactionCommandHandlerDecorator(
            ICommandHandler<TCommand> inner,
            ILogger<TransactionCommandHandlerDecorator<TCommand>> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var commandType = typeof(TCommand).Name;
            
            _logger.LogDebug("Starting transaction for command {CommandType} with correlation ID {CorrelationId}",
                commandType, command.CorrelationId);

            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromMinutes(1) // 1 minute timeout
            };

            using var transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                transactionOptions,
                TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                await _inner.HandleAsync(command, cancellationToken);
                
                transactionScope.Complete();

                _logger.LogDebug("Transaction committed successfully for command {CommandType}",
                    commandType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction rolled back for command {CommandType}. Correlation ID: {CorrelationId}",
                    commandType, command.CorrelationId);
                
                // Transaction will be automatically rolled back when scope is disposed
                throw;
            }
        }
    }

    /// <summary>
    /// Transaction decorator for command handlers with return value
    /// Wraps command execution in a transaction scope
    /// </summary>
    /// <typeparam name="TCommand">The type of command to handle</typeparam>
    /// <typeparam name="TResult">The type of result returned</typeparam>
    public class TransactionCommandHandlerDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        private readonly ICommandHandler<TCommand, TResult> _inner;
        private readonly ILogger<TransactionCommandHandlerDecorator<TCommand, TResult>> _logger;

        public TransactionCommandHandlerDecorator(
            ICommandHandler<TCommand, TResult> inner,
            ILogger<TransactionCommandHandlerDecorator<TCommand, TResult>> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var commandType = typeof(TCommand).Name;
            
            _logger.LogDebug("Starting transaction for command {CommandType} with correlation ID {CorrelationId}",
                commandType, command.CorrelationId);

            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromMinutes(1) // 1 minute timeout
            };

            using var transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                transactionOptions,
                TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var result = await _inner.HandleAsync(command, cancellationToken);
                
                transactionScope.Complete();

                _logger.LogDebug("Transaction committed successfully for command {CommandType}",
                    commandType);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction rolled back for command {CommandType}. Correlation ID: {CorrelationId}",
                    commandType, command.CorrelationId);
                
                // Transaction will be automatically rolled back when scope is disposed
                throw;
            }
        }
    }
}