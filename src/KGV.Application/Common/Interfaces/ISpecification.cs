using System.Linq.Expressions;

namespace KGV.Application.Common.Interfaces;

/// <summary>
/// Specification pattern interface for encapsulating query logic
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Where condition predicate
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }
    
    /// <summary>
    /// Include expressions for navigation properties
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }
    
    /// <summary>
    /// String-based includes for navigation properties
    /// </summary>
    List<string> IncludeStrings { get; }
    
    /// <summary>
    /// Order by expression
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }
    
    /// <summary>
    /// Order by descending expression
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }
    
    /// <summary>
    /// Group by expression
    /// </summary>
    Expression<Func<T, object>>? GroupBy { get; }
    
    /// <summary>
    /// Number of items to take (for pagination)
    /// </summary>
    int Take { get; }
    
    /// <summary>
    /// Number of items to skip (for pagination)
    /// </summary>
    int Skip { get; }
    
    /// <summary>
    /// Whether paging is enabled
    /// </summary>
    bool IsPagingEnabled { get; }
    
    /// <summary>
    /// Whether to track changes (default: true)
    /// </summary>
    bool AsNoTracking { get; }
    
    /// <summary>
    /// Whether to ignore global filters (for soft deletes, etc.)
    /// </summary>
    bool IgnoreQueryFilters { get; }
}