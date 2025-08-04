namespace KGV.Application.DTOs;

/// <summary>
/// Data Transfer Object for Bezirk entity
/// </summary>
public class BezirkDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// District name/identifier
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full display name of the district
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Description of the district
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the district is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Sort order for displaying districts
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Display name or name fallback
    /// </summary>
    public string AnzeigeName { get; set; } = string.Empty;

    /// <summary>
    /// Related cadastral districts
    /// </summary>
    public List<KatasterbezirkDto> Katasterbezirke { get; set; } = [];

    /// <summary>
    /// Count of related applications
    /// </summary>
    public int AnzahlAntraege { get; set; }

    /// <summary>
    /// When the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Who created the entity
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Who last updated the entity
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Simplified DTO for Bezirk lists
/// </summary>
public class BezirkListDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// District name/identifier
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name or name fallback
    /// </summary>
    public string AnzeigeName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the district is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Sort order for displaying districts
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Count of related cadastral districts
    /// </summary>
    public int AnzahlKatasterbezirke { get; set; }

    /// <summary>
    /// Count of related applications
    /// </summary>
    public int AnzahlAntraege { get; set; }
}