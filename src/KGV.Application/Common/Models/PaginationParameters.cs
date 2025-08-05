namespace KGV.Application.Common.Models;

/// <summary>
/// Parameters for pagination requests
/// </summary>
public class PaginationParameters
{
    private int _pageNumber = 1;
    private int _pageSize = 10;

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber 
    { 
        get => _pageNumber;
        set => _pageNumber = Math.Max(1, value);
    }

    /// <summary>
    /// Number of items per page (1-100)
    /// </summary>
    public int PageSize 
    { 
        get => _pageSize;
        set => _pageSize = Math.Max(1, Math.Min(100, value));
    }

    /// <summary>
    /// Number of items to skip (calculated)
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Default constructor
    /// </summary>
    public PaginationParameters()
    {
    }

    /// <summary>
    /// Constructor with parameters
    /// </summary>
    public PaginationParameters(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Creates default pagination parameters
    /// </summary>
    public static PaginationParameters Default => new(1, 10);

    /// <summary>
    /// Creates pagination parameters for a specific page
    /// </summary>
    public static PaginationParameters ForPage(int pageNumber, int pageSize = 10)
    {
        return new PaginationParameters(pageNumber, pageSize);
    }
}