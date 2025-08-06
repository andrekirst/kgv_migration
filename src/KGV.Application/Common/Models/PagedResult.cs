namespace KGV.Application.Common.Models;

/// <summary>
/// Represents a paged result with metadata
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items in this page
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Index of the first item on this page (1-based)
    /// </summary>
    public int FirstItemOnPage => (PageNumber - 1) * PageSize + 1;

    /// <summary>
    /// Index of the last item on this page (1-based)
    /// </summary>
    public int LastItemOnPage => Math.Min(PageNumber * PageSize, TotalCount);

    /// <summary>
    /// Whether this is the first page
    /// </summary>
    public bool IsFirstPage => PageNumber == 1;

    /// <summary>
    /// Whether this is the last page
    /// </summary>
    public bool IsLastPage => PageNumber == TotalPages;

    public PagedResult(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items.ToList().AsReadOnly();
        PageNumber = Math.Max(1, pageNumber);
        PageSize = Math.Max(1, pageSize);
        TotalCount = Math.Max(0, totalCount);
        TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
    }

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    public static PagedResult<T> Empty(int pageNumber, int pageSize)
    {
        return new PagedResult<T>(Enumerable.Empty<T>(), pageNumber, pageSize, 0);
    }

    /// <summary>
    /// Creates a paged result from all items (for in-memory paging)
    /// </summary>
    public static PagedResult<T> Create(IEnumerable<T> allItems, int pageNumber, int pageSize)
    {
        var itemsList = allItems.ToList();
        var pagedItems = itemsList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        return new PagedResult<T>(pagedItems, pageNumber, pageSize, itemsList.Count);
    }
}