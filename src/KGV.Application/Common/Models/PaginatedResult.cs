namespace KGV.Application.Common.Models;

/// <summary>
/// Represents a paginated result set
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Items in the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Creates a new paginated result
    /// </summary>
    public PaginatedResult()
    {
    }

    /// <summary>
    /// Creates a new paginated result
    /// </summary>
    /// <param name="items">Items in the current page</param>
    /// <param name="pageNumber">Current page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="totalCount">Total number of items</param>
    public PaginatedResult(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Creates a paginated result from existing data
    /// </summary>
    /// <param name="items">All items</param>
    /// <param name="pageNumber">Current page number</param>
    /// <param name="pageSize">Number of items per page</param>
    public static PaginatedResult<T> Create(IEnumerable<T> items, int pageNumber, int pageSize)
    {
        var totalCount = items.Count();
        var pagedItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        return new PaginatedResult<T>(pagedItems, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Maps the items to a different type
    /// </summary>
    /// <typeparam name="TResult">Target type</typeparam>
    /// <param name="mapper">Mapping function</param>
    public PaginatedResult<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        var mappedItems = Items.Select(mapper);
        return new PaginatedResult<TResult>(mappedItems, PageNumber, PageSize, TotalCount);
    }
}