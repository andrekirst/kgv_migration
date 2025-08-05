using FluentValidation;
using KGV.Domain.Enums;

namespace KGV.Application.Features.Parzellen.Commands.CreateParzelle;

/// <summary>
/// Validator for CreateParzelleCommand with German error messages
/// </summary>
public class CreateParzelleCommandValidator : AbstractValidator<CreateParzelleCommand>
{
    public CreateParzelleCommandValidator()
    {
        RuleFor(x => x.Nummer)
            .NotEmpty()
            .WithMessage("Die Parzellennummer ist erforderlich.")
            .MaximumLength(20)
            .WithMessage("Die Parzellennummer darf maximal 20 Zeichen lang sein.")
            .Matches("^[A-Za-z0-9äöüÄÖÜß-_\\.]+$")
            .WithMessage("Die Parzellennummer darf nur Buchstaben, Zahlen, Umlaute, Bindestriche und Punkte enthalten.");

        RuleFor(x => x.BezirkId)
            .NotEmpty()
            .WithMessage("Die Bezirks-ID ist erforderlich.");

        RuleFor(x => x.Flaeche)
            .GreaterThan(0.01m)
            .WithMessage("Die Fläche muss größer als 0,01 m² sein.")
            .LessThanOrEqualTo(10000m)
            .WithMessage("Die Fläche darf maximal 10.000 m² betragen.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Der Status ist ungültig.")
            .Must(status => status != ParzellenStatus.Decommissioned)
            .WithMessage("Eine neue Parzelle kann nicht mit dem Status 'Stillgelegt' erstellt werden.");

        RuleFor(x => x.Preis)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Der Preis darf nicht negativ sein.")
            .LessThanOrEqualTo(100000)
            .WithMessage("Der Preis darf maximal 100.000 € betragen.")
            .When(x => x.Preis.HasValue);

        RuleFor(x => x.Beschreibung)
            .MaximumLength(1000)
            .WithMessage("Die Beschreibung darf maximal 1.000 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.Beschreibung));

        RuleFor(x => x.Besonderheiten)
            .MaximumLength(500)
            .WithMessage("Die Besonderheiten dürfen maximal 500 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.Besonderheiten));

        RuleFor(x => x.Prioritaet)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Die Priorität muss größer oder gleich 0 sein.")
            .LessThanOrEqualTo(1000)
            .WithMessage("Die Priorität darf maximal 1.000 betragen.");

        RuleFor(x => x.CreatedBy)
            .MaximumLength(255)
            .WithMessage("Der Ersteller-Name darf maximal 255 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.CreatedBy));

        // Business rule validations
        RuleFor(x => x)
            .Must(HaveReasonableFlaeche)
            .WithMessage("Die Fläche erscheint unrealistisch. Bitte überprüfen Sie die Eingabe.")
            .Must(HaveReasonablePricePerSquareMeter)
            .WithMessage("Der Preis pro Quadratmeter erscheint unrealistisch. Bitte überprüfen Sie die Eingabe.");
    }

    private bool HaveReasonableFlaeche(CreateParzelleCommand command)
    {
        // Check for reasonable plot size (typically 200-1000 m² for garden plots)
        return command.Flaeche >= 50m && command.Flaeche <= 2000m;
    }

    private bool HaveReasonablePricePerSquareMeter(CreateParzelleCommand command)
    {
        if (!command.Preis.HasValue || command.Preis == 0)
            return true; // No price set is fine

        var pricePerSqm = command.Preis.Value / command.Flaeche;
        
        // Reasonable price range: 0.50 - 50 EUR per m² per month
        return pricePerSqm >= 0.50m && pricePerSqm <= 50m;
    }
}