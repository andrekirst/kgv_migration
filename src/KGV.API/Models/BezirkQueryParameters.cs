using KGV.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace KGV.API.Models;

/// <summary>
/// Query parameters for filtering and paginating Bezirke (Districts)
/// </summary>
public class BezirkQueryParameters
{
    /// <summary>
    /// Page number (1-based, default: 1)
    /// </summary>
    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size (default: 20, max: 100)
    /// </summary>
    [FromQuery(Name = "pageSize")]
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Search term for district name or display name
    /// </summary>
    [FromQuery(Name = "search")]
    [StringLength(100, ErrorMessage = "Search term cannot exceed 100 characters")]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by active status (true = active only, false = inactive only, null = all)
    /// </summary>
    [FromQuery(Name = "active")]
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filter by districts with applications (true = with applications, false = without applications, null = all)
    /// </summary>
    [FromQuery(Name = "hasApplications")]
    public bool? HasApplications { get; set; }

    /// <summary>
    /// Filter by districts with cadastral areas (true = with cadastral areas, false = without, null = all)
    /// </summary>
    [FromQuery(Name = "hasCadastralAreas")]
    public bool? HasCadastralAreas { get; set; }

    /// <summary>
    /// Sort field (default: Name)
    /// </summary>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; } = "Name";

    /// <summary>
    /// Sort direction (asc or desc, default: asc)
    /// </summary>
    [FromQuery(Name = "sortDirection")]
    [RegularExpression("^(asc|desc)$", ErrorMessage = "Sort direction must be 'asc' or 'desc'")]
    public string SortDirection { get; set; } = "asc";

    /// <summary>
    /// Whether to include related cadastral areas in the response
    /// </summary>
    [FromQuery(Name = "includeCadastralAreas")]
    public bool IncludeCadastralAreas { get; set; } = false;

    /// <summary>
    /// Whether to include application count in the response
    /// </summary>
    [FromQuery(Name = "includeApplicationCount")]
    public bool IncludeApplicationCount { get; set; } = true;

    /// <summary>
    /// Valid sort fields for Bezirke
    /// </summary>
    public static readonly string[] ValidSortFields = 
    {
        "Name", "DisplayName", "IsActive", "SortOrder", "CreatedAt", "UpdatedAt", 
        "AnzahlAntraege", "AnzahlKatasterbezirke"
    };

    /// <summary>
    /// Validates if the sort field is allowed
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValidSortField()
    {
        return ValidSortFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Specialized query parameters for searching Bezirke
/// </summary>
public class BezirkSearchParameters
{
    /// <summary>
    /// Search query term (required)
    /// </summary>
    [FromQuery(Name = "q")]
    [Required(ErrorMessage = "Search query is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Search query must be between 2 and 100 characters")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results to return (default: 10, max: 50)
    /// </summary>
    [FromQuery(Name = "limit")]
    [Range(1, 50, ErrorMessage = "Limit must be between 1 and 50")]
    public int Limit { get; set; } = 10;

    /// <summary>
    /// Whether to search only in active districts
    /// </summary>
    [FromQuery(Name = "activeOnly")]
    public bool ActiveOnly { get; set; } = true;

    /// <summary>
    /// Whether to include fuzzy matching
    /// </summary>
    [FromQuery(Name = "fuzzy")]
    public bool FuzzyMatch { get; set; } = true;
}

/// <summary>
/// API response wrapper for consistent pagination metadata
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// The response data
    /// </summary>
    public T Data { get; set; }

    /// <summary>
    /// Success indicator
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Response message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Pagination metadata (for paginated responses)
    /// </summary>
    public PaginationMetadata? Pagination { get; set; }

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiResponse(T data)
    {
        Data = data;
    }

    public ApiResponse(T data, string message) : this(data)
    {
        Message = message;
    }

    public ApiResponse(T data, PaginationMetadata pagination) : this(data)
    {
        Pagination = pagination;
    }
}

/// <summary>
/// Pagination metadata for API responses
/// </summary>
public class PaginationMetadata
{
    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// First item number on current page
    /// </summary>
    public int FirstItemOnPage { get; set; }

    /// <summary>
    /// Last item number on current page
    /// </summary>
    public int LastItemOnPage { get; set; }
}