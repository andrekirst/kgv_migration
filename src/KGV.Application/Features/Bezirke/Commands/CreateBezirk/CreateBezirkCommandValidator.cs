using FluentValidation;
using KGV.Domain.Enums;

namespace KGV.Application.Features.Bezirke.Commands.CreateBezirk;

/// <summary>
/// Validator for CreateBezirkCommand with German error messages
/// </summary>
public class CreateBezirkCommandValidator : AbstractValidator<CreateBezirkCommand>
{
    public CreateBezirkCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Der Name des Bezirks ist erforderlich.")
            .MaximumLength(10)
            .WithMessage("Der Name des Bezirks darf maximal 10 Zeichen lang sein.")
            .Matches("^[A-Za-z0-9äöüÄÖÜß-_]+$")
            .WithMessage("Der Name des Bezirks darf nur Buchstaben, Zahlen, Umlaute und Bindestriche enthalten.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(100)
            .WithMessage("Der Anzeigename darf maximal 100 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.DisplayName));

        RuleFor(x => x.Beschreibung)
            .MaximumLength(500)
            .WithMessage("Die Beschreibung darf maximal 500 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.Beschreibung));

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Die Sortierreihenfolge muss größer oder gleich 0 sein.");

        RuleFor(x => x.Flaeche)
            .GreaterThan(0)
            .WithMessage("Die Fläche muss größer als 0 sein.")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Die Fläche darf maximal 1.000.000 m² betragen.")
            .When(x => x.Flaeche.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Der Status ist ungültig.")
            .Must(status => status != BezirkStatus.Archived)
            .WithMessage("Ein neuer Bezirk kann nicht mit dem Status 'Archiviert' erstellt werden.");

        RuleFor(x => x.CreatedBy)
            .MaximumLength(255)
            .WithMessage("Der Ersteller-Name darf maximal 255 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.CreatedBy));
    }
}