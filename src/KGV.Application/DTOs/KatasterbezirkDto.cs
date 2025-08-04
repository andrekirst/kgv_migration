namespace KGV.Application.DTOs;

/// <summary>
/// Data Transfer Object for Katasterbezirk entity
/// </summary>
public class KatasterbezirkDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the parent district
    /// </summary>
    public Guid BezirkId { get; set; }

    /// <summary>
    /// Parent district name
    /// </summary>
    public string? BezirkName { get; set; }

    /// <summary>
    /// Cadastral district identifier
    /// </summary>
    public string KatasterbezirkCode { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the cadastral district
    /// </summary>
    public string KatasterbezirkName { get; set; } = string.Empty;

    /// <summary>
    /// Additional description or notes
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this cadastral district is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Sort order for displaying cadastral districts within a district
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Full display name including code and name
    /// </summary>
    public string VollAnzeigeName { get; set; } = string.Empty;

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