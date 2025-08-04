using System.Threading;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.CQRS
{
    /// <summary>
    /// Base interfaces for CQRS pattern implementation
    /// Provides clear separation between Commands and Queries
    /// </summary>

    /// <summary>
    /// Marker interface for commands that don't return a result
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Correlation ID for tracking the command across systems
        /// </summary>
        string CorrelationId { get; set; }
        
        /// <summary>
        /// User ID who initiated the command
        /// </summary>
        string UserId { get; set; }
        
        /// <summary>
        /// Timestamp when the command was created
        /// </summary>
        System.DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Interface for commands that return a result
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the command</typeparam>
    public interface ICommand<TResult> : ICommand
    {
    }

    /// <summary>
    /// Marker interface for queries
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the query</typeparam>
    public interface IQuery<TResult>
    {
        /// <summary>
        /// Correlation ID for tracking the query across systems
        /// </summary>
        string CorrelationId { get; set; }
        
        /// <summary>
        /// User ID who initiated the query
        /// </summary>
        string UserId { get; set; }
        
        /// <summary>
        /// Timestamp when the query was created
        /// </summary>
        System.DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Handler for commands that don't return a result
    /// </summary>
    /// <typeparam name="TCommand">The type of command to handle</typeparam>
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Handler for commands that return a result
    /// </summary>
    /// <typeparam name="TCommand">The type of command to handle</typeparam>
    /// <typeparam name="TResult">The type of result returned</typeparam>
    public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
    {
        Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Handler for queries
    /// </summary>
    /// <typeparam name="TQuery">The type of query to handle</typeparam>
    /// <typeparam name="TResult">The type of result returned</typeparam>
    public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
    {
        Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Mediator interface for dispatching commands and queries
    /// </summary>
    public interface ICqrsMediator
    {
        /// <summary>
        /// Send a command that doesn't return a result
        /// </summary>
        Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand;

        /// <summary>
        /// Send a command that returns a result
        /// </summary>
        Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>;

        /// <summary>
        /// Send a query
        /// </summary>
        Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>;
    }

    /// <summary>
    /// Result wrapper for operations that might fail
    /// </summary>
    /// <typeparam name="T">The type of the result value</typeparam>
    public class OperationResult<T>
    {
        public bool IsSuccess { get; set; }
        public T Value { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }

        public static OperationResult<T> Success(T value)
        {
            return new OperationResult<T>
            {
                IsSuccess = true,
                Value = value,
                Metadata = new System.Collections.Generic.Dictionary<string, object>()
            };
        }

        public static OperationResult<T> Failure(string errorMessage, string errorCode = null)
        {
            return new OperationResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode,
                Metadata = new System.Collections.Generic.Dictionary<string, object>()
            };
        }
    }

    /// <summary>
    /// Result wrapper for operations that don't return a value
    /// </summary>
    public class OperationResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }

        public static OperationResult Success()
        {
            return new OperationResult
            {
                IsSuccess = true,
                Metadata = new System.Collections.Generic.Dictionary<string, object>()
            };
        }

        public static OperationResult Failure(string errorMessage, string errorCode = null)
        {
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode,
                Metadata = new System.Collections.Generic.Dictionary<string, object>()
            };
        }
    }

    /// <summary>
    /// Base class for commands with common properties
    /// </summary>
    public abstract class BaseCommand : ICommand
    {
        protected BaseCommand()
        {
            CorrelationId = System.Guid.NewGuid().ToString();
            Timestamp = System.DateTime.UtcNow;
        }

        public string CorrelationId { get; set; }
        public string UserId { get; set; }
        public System.DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Base class for commands that return a result
    /// </summary>
    /// <typeparam name="TResult">The type of result returned</typeparam>
    public abstract class BaseCommand<TResult> : ICommand<TResult>
    {
        protected BaseCommand()
        {
            CorrelationId = System.Guid.NewGuid().ToString();
            Timestamp = System.DateTime.UtcNow;
        }

        public string CorrelationId { get; set; }
        public string UserId { get; set; }
        public System.DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Base class for queries with common properties
    /// </summary>
    /// <typeparam name="TResult">The type of result returned</typeparam>
    public abstract class BaseQuery<TResult> : IQuery<TResult>
    {
        protected BaseQuery()
        {
            CorrelationId = System.Guid.NewGuid().ToString();
            Timestamp = System.DateTime.UtcNow;
        }

        public string CorrelationId { get; set; }
        public string UserId { get; set; }
        public System.DateTime Timestamp { get; set; }
    }
}