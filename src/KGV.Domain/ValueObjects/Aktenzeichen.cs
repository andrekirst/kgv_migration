using KGV.Domain.Common;

namespace KGV.Domain.ValueObjects;

/// <summary>
/// German file reference number (Aktenzeichen) value object
/// Format: BEZIRK-NUMMER/JAHR (e.g., "A-123/2024")
/// </summary>
public class Aktenzeichen : ValueObject
{
    /// <summary>
    /// District identifier (Bezirk)
    /// </summary>
    public string Bezirk { get; private set; }

    /// <summary>
    /// Sequential number within the district and year
    /// </summary>
    public int Nummer { get; private set; }

    /// <summary>
    /// Year the file reference was created
    /// </summary>
    public int Jahr { get; private set; }

    /// <summary>
    /// Creates a new Aktenzeichen
    /// </summary>
    /// <param name="bezirk">District identifier</param>
    /// <param name="nummer">Sequential number</param>
    /// <param name="jahr">Year</param>
    public Aktenzeichen(string bezirk, int nummer, int jahr)
    {
        if (string.IsNullOrWhiteSpace(bezirk))
            throw new ArgumentException("Bezirk cannot be empty", nameof(bezirk));

        if (nummer <= 0)
            throw new ArgumentException("Nummer must be positive", nameof(nummer));

        if (jahr < 1900 || jahr > DateTime.Now.Year + 10)
            throw new ArgumentException("Jahr must be a valid year", nameof(jahr));

        if (bezirk.Length > 10)
            throw new ArgumentException("Bezirk cannot be longer than 10 characters", nameof(bezirk));

        Bezirk = bezirk.Trim().ToUpperInvariant();
        Nummer = nummer;
        Jahr = jahr;
    }

    /// <summary>
    /// Gets the formatted file reference number
    /// </summary>
    public string GetFormattedValue()
    {
        return $"{Bezirk}-{Nummer:D3}/{Jahr}";
    }

    /// <summary>
    /// Parses a formatted file reference string
    /// </summary>
    /// <param name="aktenzeichenString">Formatted string (e.g., "A-123/2024")</param>
    /// <returns>Aktenzeichen instance</returns>
    public static Aktenzeichen Parse(string aktenzeichenString)
    {
        if (string.IsNullOrWhiteSpace(aktenzeichenString))
            throw new ArgumentException("Aktenzeichen string cannot be empty", nameof(aktenzeichenString));

        // Expected format: BEZIRK-NUMMER/JAHR
        var parts = aktenzeichenString.Split('-');
        if (parts.Length != 2)
            throw new ArgumentException("Invalid Aktenzeichen format. Expected: BEZIRK-NUMMER/JAHR", nameof(aktenzeichenString));

        var bezirk = parts[0].Trim();
        var numberYearPart = parts[1].Split('/');
        
        if (numberYearPart.Length != 2)
            throw new ArgumentException("Invalid Aktenzeichen format. Expected: BEZIRK-NUMMER/JAHR", nameof(aktenzeichenString));

        if (!int.TryParse(numberYearPart[0], out var nummer))
            throw new ArgumentException("Invalid number in Aktenzeichen", nameof(aktenzeichenString));

        if (!int.TryParse(numberYearPart[1], out var jahr))
            throw new ArgumentException("Invalid year in Aktenzeichen", nameof(aktenzeichenString));

        return new Aktenzeichen(bezirk, nummer, jahr);
    }

    /// <summary>
    /// Tries to parse a formatted file reference string
    /// </summary>
    /// <param name="aktenzeichenString">Formatted string</param>
    /// <param name="aktenzeichen">Parsed Aktenzeichen or null</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParse(string aktenzeichenString, out Aktenzeichen? aktenzeichen)
    {
        try
        {
            aktenzeichen = Parse(aktenzeichenString);
            return true;
        }
        catch
        {
            aktenzeichen = null;
            return false;
        }
    }

    public override string ToString()
    {
        return GetFormattedValue();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Bezirk;
        yield return Nummer;
        yield return Jahr;
    }

    private Aktenzeichen() 
    { 
        Bezirk = string.Empty;
    }
}