using KGV.Domain.Common;
using System.Text.RegularExpressions;

namespace KGV.Domain.ValueObjects;

/// <summary>
/// German phone number value object
/// </summary>
public class PhoneNumber : ValueObject
{
    /// <summary>
    /// The formatted phone number
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Creates a new PhoneNumber
    /// </summary>
    /// <param name="phoneNumber">The phone number to validate and format</param>
    public PhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

        var cleanedNumber = CleanPhoneNumber(phoneNumber);
        
        if (!IsValidGermanPhoneNumber(cleanedNumber))
            throw new ArgumentException("Invalid German phone number format", nameof(phoneNumber));

        Value = FormatPhoneNumber(cleanedNumber);
    }

    /// <summary>
    /// Removes all non-digit characters except + at the beginning
    /// </summary>
    private static string CleanPhoneNumber(string phoneNumber)
    {
        var cleaned = Regex.Replace(phoneNumber.Trim(), @"[^\d+]", "");
        
        // Ensure + is only at the beginning
        if (cleaned.Contains('+'))
        {
            cleaned = "+" + cleaned.Replace("+", "");
        }
        
        return cleaned;
    }

    /// <summary>
    /// Validates German phone number formats
    /// </summary>
    private static bool IsValidGermanPhoneNumber(string phoneNumber)
    {
        // German phone number patterns:
        // +49... (international format)
        // 0... (national format)
        // Mobile: 015x, 016x, 017x, 019x
        // Landline: various area codes starting with 0
        
        var patterns = new[]
        {
            @"^\+49[1-9]\d{8,11}$",      // International format
            @"^0[1-9]\d{7,11}$",         // National format
            @"^01[5-7]\d{8}$",          // Mobile numbers
            @"^019\d{7}$"               // Special mobile numbers
        };

        return patterns.Any(pattern => Regex.IsMatch(phoneNumber, pattern));
    }

    /// <summary>
    /// Formats the phone number for display
    /// </summary>
    private static string FormatPhoneNumber(string phoneNumber)
    {
        if (phoneNumber.StartsWith("+49"))
        {
            // International format: +49 XXX XXXXXXX
            var withoutCountryCode = phoneNumber[3..];
            if (withoutCountryCode.Length >= 10)
            {
                return $"+49 {withoutCountryCode[..3]} {withoutCountryCode[3..]}";
            }
            return phoneNumber;
        }

        if (phoneNumber.StartsWith("0"))
        {
            // National format: 0XXX XXXXXXX
            if (phoneNumber.Length >= 10)
            {
                return $"{phoneNumber[..4]} {phoneNumber[4..]}";
            }
            return phoneNumber;
        }

        return phoneNumber;
    }

    /// <summary>
    /// Gets the international format of the phone number
    /// </summary>
    public string GetInternationalFormat()
    {
        if (Value.StartsWith("+49"))
            return Value;

        if (Value.StartsWith("0"))
        {
            var withoutLeadingZero = Value[1..].Replace(" ", "");
            return $"+49 {withoutLeadingZero}";
        }

        return Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    private PhoneNumber() 
    { 
        Value = string.Empty;
    }
}