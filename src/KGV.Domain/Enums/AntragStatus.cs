namespace KGV.Domain.Enums;

/// <summary>
/// Status values for Antrag (Application) entities
/// </summary>
public enum AntragStatus
{
    /// <summary>
    /// Neu eingegangen (Newly received)
    /// </summary>
    NeuEingegangen = 1,

    /// <summary>
    /// In Bearbeitung (In processing)
    /// </summary>
    InBearbeitung = 2,

    /// <summary>
    /// Warteschlange (Queue/Waiting list)
    /// </summary>
    Warteschlange = 3,

    /// <summary>
    /// Angebot gemacht (Offer made)
    /// </summary>
    AngebotGemacht = 4,

    /// <summary>
    /// Angebot angenommen (Offer accepted)
    /// </summary>
    AngebotAngenommen = 5,

    /// <summary>
    /// Angebot abgelehnt (Offer declined)
    /// </summary>
    AngebotAbgelehnt = 6,

    /// <summary>
    /// Abgeschlossen (Completed)
    /// </summary>
    Abgeschlossen = 7,

    /// <summary>
    /// Abgebrochen (Cancelled)
    /// </summary>
    Abgebrochen = 8,

    /// <summary>
    /// Deaktiviert (Deactivated)
    /// </summary>
    Deaktiviert = 9,

    /// <summary>
    /// In Review (In review/evaluation)
    /// </summary>
    InReview = 10,

    /// <summary>
    /// Approved (Approved for processing)
    /// </summary>
    Approved = 11
}