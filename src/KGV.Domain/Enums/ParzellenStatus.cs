namespace KGV.Domain.Enums;

/// <summary>
/// Status enumeration for Parzelle (Plot) entities
/// </summary>
public enum ParzellenStatus
{
    /// <summary>
    /// Plot is available for application
    /// </summary>
    Available = 0,

    /// <summary>
    /// Plot is reserved for a specific applicant
    /// </summary>
    Reserved = 1,

    /// <summary>
    /// Plot is currently assigned/occupied
    /// </summary>
    Assigned = 2,

    /// <summary>
    /// Plot is temporarily unavailable (maintenance, etc.)
    /// </summary>
    Unavailable = 3,

    /// <summary>
    /// Plot is under development or construction
    /// </summary>
    UnderDevelopment = 4,

    /// <summary>
    /// Plot has been decommissioned or removed
    /// </summary>
    Decommissioned = 5,

    /// <summary>
    /// Plot is pending approval for assignment
    /// </summary>
    PendingApproval = 6
}