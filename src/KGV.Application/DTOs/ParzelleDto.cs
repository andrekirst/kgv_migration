using KGV.Domain.Enums;

namespace KGV.Application.DTOs;

/// <summary>
/// Data Transfer Object for Parzelle entity
/// </summary>
public class ParzelleDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Plot number/identifier within the district
    /// </summary>
    public string Nummer { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the parent district
    /// </summary>
    public Guid BezirkId { get; set; }

    /// <summary>
    /// Parent district information
    /// </summary>
    public BezirkListDto? Bezirk { get; set; }

    /// <summary>
    /// Area of the plot in square meters
    /// </summary>
    public decimal Flaeche { get; set; }

    /// <summary>
    /// Current status of the plot
    /// </summary>
    public ParzellenStatus Status { get; set; }

    /// <summary>
    /// Status description in German
    /// </summary>
    public string StatusBeschreibung { get; set; } = string.Empty;

    /// <summary>
    /// Price or rental cost for the plot (optional)
    /// </summary>
    public decimal? Preis { get; set; }

    /// <summary>
    /// Date when the plot was assigned (if applicable)
    /// </summary>
    public DateTime? VergebenAm { get; set; }

    /// <summary>
    /// Additional notes or description for the plot
    /// </summary>
    public string? Beschreibung { get; set; }

    /// <summary>
    /// Special features or characteristics of the plot
    /// </summary>
    public string? Besonderheiten { get; set; }

    /// <summary>
    /// Whether the plot has water access
    /// </summary>
    public bool HasWasser { get; set; }

    /// <summary>
    /// Whether the plot has electricity access
    /// </summary>
    public bool HasStrom { get; set; }

    /// <summary>
    /// Priority level for assignment (higher numbers = higher priority)
    /// </summary>
    public int Prioritaet { get; set; }

    /// <summary>
    /// Full display name including district and plot number
    /// </summary>
    public string FullDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the plot is available for assignment
    /// </summary>
    public bool IsAvailableForAssignment { get; set; }

    /// <summary>
    /// Annual cost if price is set
    /// </summary>
    public decimal? AnnualCost { get; set; }

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
/// Simplified DTO for Parzelle lists
/// </summary>
public class ParzelleListDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Plot number/identifier within the district
    /// </summary>
    public string Nummer { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the parent district
    /// </summary>
    public Guid BezirkId { get; set; }

    /// <summary>
    /// District name
    /// </summary>
    public string BezirkName { get; set; } = string.Empty;

    /// <summary>
    /// Area of the plot in square meters
    /// </summary>
    public decimal Flaeche { get; set; }

    /// <summary>
    /// Current status of the plot
    /// </summary>
    public ParzellenStatus Status { get; set; }

    /// <summary>
    /// Status description in German
    /// </summary>
    public string StatusBeschreibung { get; set; } = string.Empty;

    /// <summary>
    /// Price or rental cost for the plot (optional)
    /// </summary>
    public decimal? Preis { get; set; }

    /// <summary>
    /// Date when the plot was assigned (if applicable)
    /// </summary>
    public DateTime? VergebenAm { get; set; }

    /// <summary>
    /// Whether the plot has water access
    /// </summary>
    public bool HasWasser { get; set; }

    /// <summary>
    /// Whether the plot has electricity access
    /// </summary>
    public bool HasStrom { get; set; }

    /// <summary>
    /// Priority level for assignment
    /// </summary>
    public int Prioritaet { get; set; }

    /// <summary>
    /// Full display name including district and plot number
    /// </summary>
    public string FullDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the plot is available for assignment
    /// </summary>
    public bool IsAvailableForAssignment { get; set; }
}

/// <summary>
/// Input DTO for creating Parzelle
/// </summary>
public class CreateParzelleDto
{
    /// <summary>
    /// Plot number/identifier within the district
    /// </summary>
    public required string Nummer { get; init; }

    /// <summary>
    /// Reference to the parent district
    /// </summary>
    public required Guid BezirkId { get; init; }

    /// <summary>
    /// Area of the plot in square meters
    /// </summary>
    public required decimal Flaeche { get; init; }

    /// <summary>
    /// Initial status of the plot (default: Available)
    /// </summary>
    public ParzellenStatus Status { get; init; } = ParzellenStatus.Available;

    /// <summary>
    /// Price or rental cost for the plot (optional)
    /// </summary>
    public decimal? Preis { get; init; }

    /// <summary>
    /// Additional notes or description for the plot
    /// </summary>
    public string? Beschreibung { get; init; }

    /// <summary>
    /// Special features or characteristics of the plot
    /// </summary>
    public string? Besonderheiten { get; init; }

    /// <summary>
    /// Whether the plot has water access
    /// </summary>
    public bool HasWasser { get; init; } = false;

    /// <summary>
    /// Whether the plot has electricity access
    /// </summary>
    public bool HasStrom { get; init; } = false;

    /// <summary>
    /// Priority level for assignment (default: 0)
    /// </summary>
    public int Prioritaet { get; init; } = 0;
}

/// <summary>
/// Input DTO for updating Parzelle
/// </summary>
public class UpdateParzelleDto
{
    /// <summary>
    /// Area of the plot in square meters (optional)
    /// </summary>
    public decimal? Flaeche { get; init; }

    /// <summary>
    /// Price or rental cost for the plot (optional)
    /// </summary>
    public decimal? Preis { get; init; }

    /// <summary>
    /// Additional notes or description for the plot (optional)
    /// </summary>
    public string? Beschreibung { get; init; }

    /// <summary>
    /// Special features or characteristics of the plot (optional)
    /// </summary>
    public string? Besonderheiten { get; init; }

    /// <summary>
    /// Whether the plot has water access (optional)
    /// </summary>
    public bool? HasWasser { get; init; }

    /// <summary>
    /// Whether the plot has electricity access (optional)
    /// </summary>
    public bool? HasStrom { get; init; }

    /// <summary>
    /// Priority level for assignment (optional)
    /// </summary>
    public int? Prioritaet { get; init; }
}