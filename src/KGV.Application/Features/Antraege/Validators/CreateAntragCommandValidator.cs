using FluentValidation;
using KGV.Application.Features.Antraege.Commands;
using System.Text.RegularExpressions;

namespace KGV.Application.Features.Antraege.Validators;

/// <summary>
/// Validator for CreateAntragCommand
/// </summary>
public class CreateAntragCommandValidator : AbstractValidator<CreateAntragCommand>
{
    public CreateAntragCommandValidator()
    {
        RuleFor(x => x.Vorname)
            .NotEmpty()
            .WithMessage("Vorname ist erforderlich")
            .MaximumLength(50)
            .WithMessage("Vorname darf maximal 50 Zeichen lang sein");

        RuleFor(x => x.Nachname)
            .NotEmpty()
            .WithMessage("Nachname ist erforderlich")
            .MaximumLength(50)
            .WithMessage("Nachname darf maximal 50 Zeichen lang sein");

        RuleFor(x => x.Strasse)
            .NotEmpty()
            .WithMessage("Straße ist erforderlich")
            .MaximumLength(50)
            .WithMessage("Straße darf maximal 50 Zeichen lang sein");

        RuleFor(x => x.PLZ)
            .NotEmpty()
            .WithMessage("PLZ ist erforderlich")
            .Matches(@"^\d{5}$")
            .WithMessage("PLZ muss genau 5 Ziffern haben")
            .Must(BeValidGermanPostalCode)
            .WithMessage("PLZ muss zwischen 01000 und 99999 liegen");

        RuleFor(x => x.Ort)
            .NotEmpty()
            .WithMessage("Ort ist erforderlich")
            .MaximumLength(50)
            .WithMessage("Ort darf maximal 50 Zeichen lang sein");

        RuleFor(x => x.EMail)
            .EmailAddress()
            .WithMessage("E-Mail-Adresse ist ungültig")
            .MaximumLength(100)
            .WithMessage("E-Mail-Adresse darf maximal 100 Zeichen lang sein")
            .When(x => !string.IsNullOrEmpty(x.EMail));

        RuleFor(x => x.Telefon)
            .Must(BeValidPhoneNumber)
            .WithMessage("Telefonnummer ist ungültig")
            .When(x => !string.IsNullOrEmpty(x.Telefon));

        RuleFor(x => x.MobilTelefon)
            .Must(BeValidPhoneNumber)
            .WithMessage("Mobiltelefonnummer ist ungültig")
            .When(x => !string.IsNullOrEmpty(x.MobilTelefon));

        RuleFor(x => x.GeschTelefon)
            .Must(BeValidPhoneNumber)
            .WithMessage("Geschäftstelefonnummer ist ungültig")
            .When(x => !string.IsNullOrEmpty(x.GeschTelefon));

        RuleFor(x => x.MobilTelefon2)
            .Must(BeValidPhoneNumber)
            .WithMessage("Zweite Mobiltelefonnummer ist ungültig")
            .When(x => !string.IsNullOrEmpty(x.MobilTelefon2));

        RuleFor(x => x.Titel)
            .MaximumLength(50)
            .WithMessage("Titel darf maximal 50 Zeichen lang sein")
            .When(x => !string.IsNullOrEmpty(x.Titel));

        RuleFor(x => x.Vorname2)
            .MaximumLength(50)
            .WithMessage("Zweiter Vorname darf maximal 50 Zeichen lang sein")
            .When(x => !string.IsNullOrEmpty(x.Vorname2));

        RuleFor(x => x.Nachname2)
            .MaximumLength(50)
            .WithMessage("Zweiter Nachname darf maximal 50 Zeichen lang sein")
            .When(x => !string.IsNullOrEmpty(x.Nachname2));

        RuleFor(x => x.Briefanrede)
            .MaximumLength(150)
            .WithMessage("Briefanrede darf maximal 150 Zeichen lang sein")
            .When(x => !string.IsNullOrEmpty(x.Briefanrede));

        RuleFor(x => x.Wunsch)
            .MaximumLength(600)
            .WithMessage("Wunsch darf maximal 600 Zeichen lang sein")
            .When(x => !string.IsNullOrEmpty(x.Wunsch));

        RuleFor(x => x.Vermerk)
            .MaximumLength(2000)
            .WithMessage("Vermerk darf maximal 2000 Zeichen lang sein")
            .When(x => !string.IsNullOrEmpty(x.Vermerk));

        RuleFor(x => x.Geburtstag)
            .MaximumLength(100)
            .WithMessage("Geburtstag darf maximal 100 Zeichen lang sein")
            .When(x => !string.IsNullOrEmpty(x.Geburtstag));

        RuleFor(x => x.Geburtstag2)
            .MaximumLength(100)
            .WithMessage("Zweiter Geburtstag darf maximal 100 Zeichen lang sein")
            .When(x => !string.IsNullOrEmpty(x.Geburtstag2));

        RuleFor(x => x.WartelistenNr32)
            .MaximumLength(20)
            .WithMessage("Wartelistennummer 32 darf maximal 20 Zeichen lang sein")
            .When(x => !string.IsNullOrEmpty(x.WartelistenNr32));

        RuleFor(x => x.WartelistenNr33)
            .MaximumLength(20)
            .WithMessage("Wartelistennummer 33 darf maximal 20 Zeichen lang sein")
            .When(x => !string.IsNullOrEmpty(x.WartelistenNr33));

        RuleFor(x => x.Bewerbungsdatum)
            .LessThanOrEqualTo(DateTime.Now.AddDays(1))
            .WithMessage("Bewerbungsdatum darf nicht in der Zukunft liegen")
            .GreaterThan(new DateTime(1900, 1, 1))
            .WithMessage("Bewerbungsdatum ist ungültig")
            .When(x => x.Bewerbungsdatum.HasValue);

        // Validate that at least one contact method is provided
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.Telefon) || 
                      !string.IsNullOrEmpty(x.MobilTelefon) || 
                      !string.IsNullOrEmpty(x.EMail))
            .WithMessage("Mindestens eine Kontaktmöglichkeit (Telefon, Mobiltelefon oder E-Mail) ist erforderlich");
    }

    private static bool BeValidGermanPostalCode(string? plz)
    {
        if (string.IsNullOrWhiteSpace(plz) || plz.Length != 5)
            return false;

        if (!plz.All(char.IsDigit))
            return false;

        var numericPlz = int.Parse(plz);
        return numericPlz >= 1000 && numericPlz <= 99999;
    }

    private static bool BeValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return true; // Allow empty values

        // Remove all non-digit characters except + at the beginning
        var cleaned = Regex.Replace(phoneNumber.Trim(), @"[^\d+]", "");
        
        // Ensure + is only at the beginning
        if (cleaned.Contains('+'))
        {
            cleaned = "+" + cleaned.Replace("+", "");
        }

        // German phone number patterns
        var patterns = new[]
        {
            @"^\+49[1-9]\d{8,11}$",      // International format
            @"^0[1-9]\d{7,11}$",         // National format
            @"^01[5-7]\d{8}$",          // Mobile numbers
            @"^019\d{7}$"               // Special mobile numbers
        };

        return patterns.Any(pattern => Regex.IsMatch(cleaned, pattern));
    }
}