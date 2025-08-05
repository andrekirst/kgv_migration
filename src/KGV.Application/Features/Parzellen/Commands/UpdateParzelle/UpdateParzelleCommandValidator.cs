using FluentValidation;

namespace KGV.Application.Features.Parzellen.Commands.UpdateParzelle;

/// <summary>
/// Validator for UpdateParzelleCommand with German error messages
/// </summary>
public class UpdateParzelleCommandValidator : AbstractValidator<UpdateParzelleCommand>
{
    public UpdateParzelleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Die Parzellen-ID ist erforderlich.");

        RuleFor(x => x.Flaeche)
            .GreaterThan(0.01m)
            .WithMessage("Die Fläche muss größer als 0,01 m² sein.")
            .LessThanOrEqualTo(10000m)
            .WithMessage("Die Fläche darf maximal 10.000 m² betragen.")
            .When(x => x.Flaeche.HasValue);

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
            .WithMessage("Die Priorität darf maximal 1.000 betragen.")
            .When(x => x.Prioritaet.HasValue);

        RuleFor(x => x.UpdatedBy)
            .MaximumLength(255)
            .WithMessage("Der Bearbeiter-Name darf maximal 255 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.UpdatedBy));

        // Business rule validations
        RuleFor(x => x)
            .Must(HaveReasonableFlaeche)
            .WithMessage("Die Fläche erscheint unrealistisch. Bitte überprüfen Sie die Eingabe.")
            .Must(HaveReasonablePricePerSquareMeter)
            .WithMessage("Der Preis pro Quadratmeter erscheint unrealistisch. Bitte überprüfen Sie die Eingabe.")
            .When(x => x.Flaeche.HasValue && x.Preis.HasValue);
    }

    private bool HaveReasonableFlaeche(UpdateParzelleCommand command)
    {
        if (!command.Flaeche.HasValue)
            return true;

        // Check for reasonable plot size (typically 200-1000 m² for garden plots)
        return command.Flaeche.Value >= 50m && command.Flaeche.Value <= 2000m;
    }

    private bool HaveReasonablePricePerSquareMeter(UpdateParzelleCommand command)
    {
        if (!command.Preis.HasValue || !command.Flaeche.HasValue || command.Preis == 0)
            return true;

        var pricePerSqm = command.Preis.Value / command.Flaeche.Value;
        
        // Reasonable price range: 0.50 - 50 EUR per m² per month
        return pricePerSqm >= 0.50m && pricePerSqm <= 50m;
    }
}