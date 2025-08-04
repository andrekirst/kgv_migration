namespace KGV.Domain.Enums;

/// <summary>
/// Types of history entries (Verlauf)
/// </summary>
public enum VerlaufArt
{
    /// <summary>
    /// Antrag eingegangen (Application received)
    /// </summary>
    AntragEingegangen = 1,

    /// <summary>
    /// Bestätigung versendet (Confirmation sent)
    /// </summary>
    BestaetigungVersendet = 2,

    /// <summary>
    /// Angebot gemacht (Offer made)
    /// </summary>
    AngebotGemacht = 3,

    /// <summary>
    /// Angebot angenommen (Offer accepted)
    /// </summary>
    AngebotAngenommen = 4,

    /// <summary>
    /// Angebot abgelehnt (Offer declined)
    /// </summary>
    AngebotAbgelehnt = 5,

    /// <summary>
    /// Besichtigung (Inspection)
    /// </summary>
    Besichtigung = 6,

    /// <summary>
    /// Vertrag erstellt (Contract created)
    /// </summary>
    VertragErstellt = 7,

    /// <summary>
    /// Abgeschlossen (Completed)
    /// </summary>
    Abgeschlossen = 8,

    /// <summary>
    /// Notiz (Note)
    /// </summary>
    Notiz = 9
}