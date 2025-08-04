using KGV.Domain.Common;

namespace KGV.Domain.ValueObjects;

/// <summary>
/// German address value object
/// </summary>
public class Address : ValueObject
{
    /// <summary>
    /// Street name and number
    /// </summary>
    public string Strasse { get; private set; }

    /// <summary>
    /// Postal code (PLZ)
    /// </summary>
    public string PLZ { get; private set; }

    /// <summary>
    /// City name
    /// </summary>
    public string Ort { get; private set; }

    /// <summary>
    /// Creates a new Address
    /// </summary>
    /// <param name="strasse">Street name and number</param>
    /// <param name="plz">Postal code</param>
    /// <param name="ort">City name</param>
    public Address(string strasse, string plz, string ort)
    {
        if (string.IsNullOrWhiteSpace(strasse))
            throw new ArgumentException("Strasse cannot be empty", nameof(strasse));
        
        if (string.IsNullOrWhiteSpace(plz))
            throw new ArgumentException("PLZ cannot be empty", nameof(plz));
        
        if (string.IsNullOrWhiteSpace(ort))
            throw new ArgumentException("Ort cannot be empty", nameof(ort));

        // German postal code validation
        if (!IsValidGermanPostalCode(plz))
            throw new ArgumentException("Invalid German postal code", nameof(plz));

        Strasse = strasse.Trim();
        PLZ = plz.Trim();
        Ort = ort.Trim();
    }

    /// <summary>
    /// Validates German postal codes (5 digits, 01000-99999)
    /// </summary>
    private static bool IsValidGermanPostalCode(string plz)
    {
        if (string.IsNullOrWhiteSpace(plz) || plz.Length != 5)
            return false;

        if (!plz.All(char.IsDigit))
            return false;

        var numericPlz = int.Parse(plz);
        return numericPlz >= 1000 && numericPlz <= 99999;
    }

    /// <summary>
    /// Gets the full address as a formatted string
    /// </summary>
    public string GetFullAddress()
    {
        return $"{Strasse}, {PLZ} {Ort}";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Strasse.ToUpperInvariant();
        yield return PLZ;
        yield return Ort.ToUpperInvariant();
    }

    private Address() 
    { 
        Strasse = string.Empty;
        PLZ = string.Empty;
        Ort = string.Empty;
    }
}