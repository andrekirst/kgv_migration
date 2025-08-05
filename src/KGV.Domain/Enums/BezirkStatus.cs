namespace KGV.Domain.Enums;

/// <summary>
/// Status enumeration for Bezirk (District) entities
/// </summary>
public enum BezirkStatus
{
    /// <summary>
    /// District is inactive/not in use
    /// </summary>
    Inactive = 0,

    /// <summary>
    /// District is active and operational
    /// </summary>
    Active = 1,

    /// <summary>
    /// District is temporarily suspended
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// District is being restructured or reorganized
    /// </summary>
    UnderRestructuring = 3,

    /// <summary>
    /// District is archived (historical data only)
    /// </summary>
    Archived = 4
}