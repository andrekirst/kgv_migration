using KGV.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace KGV.API.Models;

/// <summary>
/// Query parameters for filtering and paginating Parzellen (Plots)
/// </summary>
public class ParzelleQueryParameters
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
    /// Search term for plot number or description
    /// </summary>
    [FromQuery(Name = "search")]
    [StringLength(100, ErrorMessage = "Search term cannot exceed 100 characters")]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by district ID
    /// </summary>
    [FromQuery(Name = "bezirkId")]
    public Guid? BezirkId { get; set; }

    /// <summary>
    /// Filter by plot status
    /// </summary>
    [FromQuery(Name = "status")]
    public ParzellenStatus? Status { get; set; }

    /// <summary>
    /// Filter by availability for assignment (true = available only, false = not available, null = all)
    /// </summary>
    [FromQuery(Name = "available")]
    public bool? IsAvailable { get; set; }

    /// <summary>
    /// Filter by plots with water access (true = with water, false = without water, null = all)
    /// </summary>
    [FromQuery(Name = "hasWater")]
    public bool? HasWasser { get; set; }

    /// <summary>
    /// Filter by plots with electricity access (true = with electricity, false = without, null = all)
    /// </summary>
    [FromQuery(Name = "hasElectricity")]
    public bool? HasStrom { get; set; }

    /// <summary>
    /// Minimum area in square meters
    /// </summary>
    [FromQuery(Name = "minArea")]
    [Range(0, double.MaxValue, ErrorMessage = "Minimum area must be greater than or equal to 0")]
    public decimal? MinFlaeche { get; set; }

    /// <summary>
    /// Maximum area in square meters
    /// </summary>
    [FromQuery(Name = "maxArea")]
    [Range(0, double.MaxValue, ErrorMessage = "Maximum area must be greater than or equal to 0")]
    public decimal? MaxFlaeche { get; set; }

    /// <summary>
    /// Minimum price
    /// </summary>
    [FromQuery(Name = "minPrice")]
    [Range(0, double.MaxValue, ErrorMessage = "Minimum price must be greater than or equal to 0")]
    public decimal? MinPreis { get; set; }

    /// <summary>
    /// Maximum price
    /// </summary>
    [FromQuery(Name = "maxPrice")]
    [Range(0, double.MaxValue, ErrorMessage = "Maximum price must be greater than or equal to 0")]
    public decimal? MaxPreis { get; set; }

    /// <summary>
    /// Minimum priority level
    /// </summary>
    [FromQuery(Name = "minPriority")]
    [Range(0, int.MaxValue, ErrorMessage = "Minimum priority must be greater than or equal to 0")]
    public int? MinPrioritaet { get; set; }

    /// <summary>
    /// Filter by assigned date range - from
    /// </summary>
    [FromQuery(Name = "assignedFrom")]
    public DateTime? VergebenFrom { get; set; }

    /// <summary>
    /// Filter by assigned date range - to
    /// </summary>
    [FromQuery(Name = "assignedTo")]
    public DateTime? VergebenTo { get; set; }

    /// <summary>
    /// Sort field (default: Nummer)
    /// </summary>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; } = "Nummer";

    /// <summary>
    /// Sort direction (asc or desc, default: asc)
    /// </summary>
    [FromQuery(Name = "sortDirection")]
    [RegularExpression("^(asc|desc)$", ErrorMessage = "Sort direction must be 'asc' or 'desc'")]
    public string SortDirection { get; set; } = "asc";

    /// <summary>
    /// Whether to include district information in the response
    /// </summary>
    [FromQuery(Name = "includeBezirk")]
    public bool IncludeBezirk { get; set; } = true;

    /// <summary>
    /// Valid sort fields for Parzellen
    /// </summary>
    public static readonly string[] ValidSortFields = 
    {
        "Nummer", "BezirkId", "Flaeche", "Status", "Preis", "VergebenAm", 
        "Prioritaet", "CreatedAt", "UpdatedAt", "HasWasser", "HasStrom"
    };

    /// <summary>
    /// Validates if the sort field is allowed
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValidSortField()
    {
        return ValidSortFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates that min/max values are logically correct
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidateRanges()
    {
        var errors = new List<string>();

        if (MinFlaeche.HasValue && MaxFlaeche.HasValue && MinFlaeche > MaxFlaeche)
        {
            errors.Add("Minimum area cannot be greater than maximum area");
        }

        if (MinPreis.HasValue && MaxPreis.HasValue && MinPreis > MaxPreis)
        {
            errors.Add("Minimum price cannot be greater than maximum price");
        }

        if (VergebenFrom.HasValue && VergebenTo.HasValue && VergebenFrom > VergebenTo)
        {
            errors.Add("Assignment date 'from' cannot be greater than 'to'");
        }

        return errors;
    }
}

/// <summary>
/// Specialized query parameters for searching Parzellen
/// </summary>
public class ParzelleSearchParameters
{
    /// <summary>
    /// Search query term (required)
    /// </summary>
    [FromQuery(Name = "q")]
    [Required(ErrorMessage = "Search query is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Search query must be between 1 and 100 characters")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results to return (default: 10, max: 50)
    /// </summary>
    [FromQuery(Name = "limit")]
    [Range(1, 50, ErrorMessage = "Limit must be between 1 and 50")]
    public int Limit { get; set; } = 10;

    /// <summary>
    /// Filter by district ID
    /// </summary>
    [FromQuery(Name = "bezirkId")]
    public Guid? BezirkId { get; set; }

    /// <summary>
    /// Whether to search only in available plots
    /// </summary>
    [FromQuery(Name = "availableOnly")]
    public bool AvailableOnly { get; set; } = false;

    /// <summary>
    /// Whether to include fuzzy matching
    /// </summary>
    [FromQuery(Name = "fuzzy")]
    public bool FuzzyMatch { get; set; } = true;
}

/// <summary>
/// Parameters for plot assignment operation
/// </summary>
public class AssignParzelleParameters
{
    /// <summary>
    /// ID of the person/application to assign the plot to
    /// </summary>
    [Required(ErrorMessage = "Assignment target ID is required")]
    public Guid AssigneeId { get; set; }

    /// <summary>
    /// Assignment date (default: current date)
    /// </summary>
    public DateTime AssignmentDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Notes about the assignment
    /// </summary>
    [StringLength(500, ErrorMessage = "Assignment notes cannot exceed 500 characters")]
    public string? Notes { get; set; }

    /// <summary>
    /// Assignment price if different from default
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Assignment price must be greater than or equal to 0")]
    public decimal? AssignmentPrice { get; set; }
}

/// <summary>
/// Statistics filter parameters for plots
/// </summary>
public class ParzelleStatisticsParameters
{
    /// <summary>
    /// Filter by district ID
    /// </summary>
    [FromQuery(Name = "bezirkId")]
    public Guid? BezirkId { get; set; }

    /// <summary>
    /// Include historical data (default: false - current data only)
    /// </summary>
    [FromQuery(Name = "includeHistory")]
    public bool IncludeHistory { get; set; } = false;

    /// <summary>
    /// Date range start for historical analysis
    /// </summary>
    [FromQuery(Name = "fromDate")]
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Date range end for historical analysis
    /// </summary>
    [FromQuery(Name = "toDate")]
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Group statistics by district
    /// </summary>
    [FromQuery(Name = "groupByBezirk")]
    public bool GroupByBezirk { get; set; } = false;

    /// <summary>
    /// Group statistics by status
    /// </summary>
    [FromQuery(Name = "groupByStatus")]
    public bool GroupByStatus { get; set; } = true;
}