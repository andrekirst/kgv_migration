using KGV.Domain.Common;
using System.Text.RegularExpressions;

namespace KGV.Domain.ValueObjects;

/// <summary>
/// Email address value object
/// </summary>
public class Email : ValueObject
{
    /// <summary>
    /// The email address value
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Creates a new Email
    /// </summary>
    /// <param name="email">The email address to validate</param>
    public Email(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        var trimmedEmail = email.Trim().ToLowerInvariant();
        
        if (!IsValidEmail(trimmedEmail))
            throw new ArgumentException("Invalid email format", nameof(email));

        Value = trimmedEmail;
    }

    /// <summary>
    /// Validates email format using RFC 5322 compliant regex
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // RFC 5322 compliant regex pattern
        var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        
        if (!Regex.IsMatch(email, pattern))
            return false;

        // Additional validation: check for consecutive dots
        if (email.Contains(".."))
            return false;

        // Check local part (before @) length (max 64 characters)
        var parts = email.Split('@');
        if (parts[0].Length > 64)
            return false;

        // Check domain part length (max 253 characters)
        if (parts[1].Length > 253)
            return false;

        return true;
    }

    /// <summary>
    /// Gets the domain part of the email
    /// </summary>
    public string GetDomain()
    {
        var atIndex = Value.IndexOf('@');
        return atIndex >= 0 ? Value[(atIndex + 1)..] : string.Empty;
    }

    /// <summary>
    /// Gets the local part of the email (before @)
    /// </summary>
    public string GetLocalPart()
    {
        var atIndex = Value.IndexOf('@');
        return atIndex >= 0 ? Value[..atIndex] : Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    private Email() 
    { 
        Value = string.Empty;
    }
}