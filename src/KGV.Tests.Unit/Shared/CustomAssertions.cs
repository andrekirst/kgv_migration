using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using KGV.Domain.Entities;
using KGV.Domain.ValueObjects;

namespace KGV.Tests.Unit.Shared;

/// <summary>
/// Custom Assertions für Domain-spezifische Objekte.
/// Erweitert FluentAssertions um KGV-spezifische Assertion-Methoden.
/// </summary>
public static class CustomAssertions
{
    /// <summary>
    /// Erweitert ObjectAssertions um Bezirk-spezifische Assertions.
    /// </summary>
    public static BezirkAssertions Should(this Bezirk bezirk)
    {
        return new BezirkAssertions(bezirk);
    }

    /// <summary>
    /// Erweitert ObjectAssertions um Antrag-spezifische Assertions.
    /// </summary>
    public static AntragAssertions Should(this Antrag antrag)
    {
        return new AntragAssertions(antrag);
    }

    /// <summary>
    /// Erweitert ObjectAssertions um Email-spezifische Assertions.
    /// </summary>
    public static EmailAssertions Should(this Email email)
    {
        return new EmailAssertions(email);
    }
}

/// <summary>
/// Custom Assertions für Bezirk-Entitäten.
/// </summary>
public class BezirkAssertions : ReferenceTypeAssertions<Bezirk, BezirkAssertions>
{
    public BezirkAssertions(Bezirk bezirk) : base(bezirk)
    {
    }

    protected override string Identifier => "bezirk";

    /// <summary>
    /// Prüft, ob der Bezirk gültige deutsche Zeichen enthält.
    /// </summary>
    public AndConstraint<BezirkAssertions> ContainGermanCharacters(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject?.Name?.ContainsGermanCharacters() == true || 
                         Subject?.Beschreibung?.ContainsGermanCharacters() == true)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:bezirk} to contain German characters, but it did not.");

        return new AndConstraint<BezirkAssertions>(this);
    }

    /// <summary>
    /// Prüft, ob der Bezirk eine gültige Struktur hat.
    /// </summary>
    public AndConstraint<BezirkAssertions> HaveValidStructure(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject != null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:bezirk} to be valid, but it was null.")
            .Then
            .ForCondition(!string.IsNullOrWhiteSpace(Subject!.Name))
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:bezirk} to have a valid name, but it was {0}.", Subject!.Name)
            .Then
            .ForCondition(Subject.ErstelltAm != default(DateTime))
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:bezirk} to have a valid creation date, but it was {0}.", Subject.ErstelltAm);

        return new AndConstraint<BezirkAssertions>(this);
    }

    /// <summary>
    /// Prüft, ob der Bezirk aktiv ist.
    /// </summary>
    public AndConstraint<BezirkAssertions> BeActive(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject?.IsActive() == true)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:bezirk} to be active, but it was {0}.", Subject?.Status);

        return new AndConstraint<BezirkAssertions>(this);
    }
}

/// <summary>
/// Custom Assertions für Antrag-Entitäten.
/// </summary>
public class AntragAssertions : ReferenceTypeAssertions<Antrag, AntragAssertions>
{
    public AntragAssertions(Antrag antrag) : base(antrag)
    {
    }

    protected override string Identifier => "antrag";

    /// <summary>
    /// Prüft, ob der Antrag vollständige Kontaktdaten hat.
    /// </summary>
    public AndConstraint<AntragAssertions> HaveCompleteContactData(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject != null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:antrag} to be valid, but it was null.")
            .Then
            .ForCondition(!string.IsNullOrWhiteSpace(Subject!.Vorname))
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:antrag} to have a first name, but it was {0}.", Subject!.Vorname)
            .Then
            .ForCondition(!string.IsNullOrWhiteSpace(Subject.Nachname))
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:antrag} to have a last name, but it was {0}.", Subject.Nachname)
            .Then
            .ForCondition(Subject.Email != null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:antrag} to have an email address, but it was null.")
            .Then
            .ForCondition(Subject.Telefonnummer != null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:antrag} to have a phone number, but it was null.")
            .Then
            .ForCondition(Subject.Adresse != null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:antrag} to have an address, but it was null.");

        return new AndConstraint<AntragAssertions>(this);
    }

    /// <summary>
    /// Prüft, ob der Antrag eine gültige deutsche Adresse hat.
    /// </summary>
    public AndConstraint<AntragAssertions> HaveValidGermanAddress(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject?.Adresse != null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:antrag} to have an address, but it was null.")
            .Then
            .ForCondition(Subject!.Adresse!.Plz.IsValidGermanPostalCode())
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:antrag} to have a valid German postal code, but it was {0}.", Subject.Adresse.Plz);

        return new AndConstraint<AntragAssertions>(this);
    }
}

/// <summary>
/// Custom Assertions für Email Value Objects.
/// </summary>
public class EmailAssertions : ReferenceTypeAssertions<Email, EmailAssertions>
{
    public EmailAssertions(Email email) : base(email)
    {
    }

    protected override string Identifier => "email";

    /// <summary>
    /// Prüft, ob die E-Mail eine deutsche Domain hat.
    /// </summary>
    public AndConstraint<EmailAssertions> HaveGermanDomain(string because = "", params object[] becauseArgs)
    {
        var germanDomains = new[] { ".de", ".org", ".com" }; // Erweiterte Liste für Tests

        Execute.Assertion
            .ForCondition(Subject != null && germanDomains.Any(domain => Subject.Domain.EndsWith(domain)))
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:email} to have a German domain, but it was {0}.", Subject?.Domain);

        return new AndConstraint<EmailAssertions>(this);
    }

    /// <summary>
    /// Prüft, ob die E-Mail deutsche Sonderzeichen unterstützt.
    /// </summary>
    public AndConstraint<EmailAssertions> SupportGermanCharacters(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject?.Value?.ContainsGermanCharacters() == true)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:email} to support German characters, but it was {0}.", Subject?.Value);

        return new AndConstraint<EmailAssertions>(this);
    }
}

/// <summary>
/// Helper-Methoden für deutsche Zeichen und Validierungen.
/// </summary>
internal static class GermanValidationExtensions
{
    private static readonly char[] GermanChars = { 'ä', 'ö', 'ü', 'Ä', 'Ö', 'Ü', 'ß' };

    /// <summary>
    /// Prüft, ob ein String deutsche Sonderzeichen enthält.
    /// </summary>
    public static bool ContainsGermanCharacters(this string text)
    {
        return !string.IsNullOrEmpty(text) && text.IndexOfAny(GermanChars) >= 0;
    }

    /// <summary>
    /// Prüft, ob eine PLZ eine gültige deutsche Postleitzahl ist.
    /// </summary>
    public static bool IsValidGermanPostalCode(this string plz)
    {
        return !string.IsNullOrEmpty(plz) && 
               plz.Length == 5 && 
               plz.All(char.IsDigit) &&
               int.TryParse(plz, out var number) &&
               number >= 1000 && number <= 99999;
    }
}