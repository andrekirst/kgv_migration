using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KGV.Infrastructure.Patterns.CQRS.Decorators
{
    /// <summary>
    /// Logging decorator for query handlers
    /// Provides detailed logging for query execution
    /// </summary>
    /// <typeparam name="TQuery">The type of query to handle</typeparam>
    /// <typeparam name="TResult">The type of result returned</typeparam>
    public class LoggingQueryHandlerDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly IQueryHandler<TQuery, TResult> _inner;
        private readonly ILogger<LoggingQueryHandlerDecorator<TQuery, TResult>> _logger;

        public LoggingQueryHandlerDecorator(
            IQueryHandler<TQuery, TResult> inner,
            ILogger<LoggingQueryHandlerDecorator<TQuery, TResult>> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            var queryType = typeof(TQuery).Name;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug("Starting execution of query {QueryType} with correlation ID {CorrelationId} by user {UserId}",
                    queryType, query.CorrelationId, query.UserId);

                var result = await _inner.HandleAsync(query, cancellationToken);

                stopwatch.Stop();

                _logger.LogDebug("Successfully executed query {QueryType} in {ElapsedMs}ms with result type {ResultType}",
                    queryType, stopwatch.ElapsedMilliseconds, typeof(TResult).Name);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex, "Query {QueryType} failed after {ElapsedMs}ms. Correlation ID: {CorrelationId}",
                    queryType, stopwatch.ElapsedMilliseconds, query.CorrelationId);

                throw;
            }
        }
    }
}