using KGV.Application.Common.Interfaces;
using System.Linq.Expressions;

namespace KGV.Infrastructure.Repositories.Base;

/// <summary>
/// Base implementation of the Specification pattern
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class BaseSpecification<T> : ISpecification<T>
{
    protected BaseSpecification()
    {
        Includes = new List<Expression<Func<T, object>>>();
        IncludeStrings = new List<string>();
    }

    protected BaseSpecification(Expression<Func<T, bool>> criteria) : this()
    {
        Criteria = criteria;
    }

    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public Expression<Func<T, object>>? GroupBy { get; private set; }
    public int Take { get; private set; }
    public int Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }
    public bool AsNoTracking { get; private set; }
    public bool IgnoreQueryFilters { get; private set; }

    /// <summary>
    /// Adds a where condition
    /// </summary>
    protected virtual void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Adds an include for a navigation property
    /// </summary>
    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds a string-based include for a navigation property
    /// </summary>
    protected virtual void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Adds ascending order by
    /// </summary>
    protected virtual void AddOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Adds descending order by
    /// </summary>
    protected virtual void AddOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Adds group by
    /// </summary>
    protected virtual void AddGroupBy(Expression<Func<T, object>> groupByExpression)
    {
        GroupBy = groupByExpression;
    }

    /// <summary>
    /// Enables paging with skip and take
    /// </summary>
    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>
    /// Disables change tracking for read-only queries
    /// </summary>
    protected virtual void ApplyNoTracking()
    {
        AsNoTracking = true;
    }

    /// <summary>
    /// Ignores global query filters (e.g., for soft deletes)
    /// </summary>
    protected virtual void ApplyIgnoreQueryFilters()
    {
        IgnoreQueryFilters = true;
    }
}