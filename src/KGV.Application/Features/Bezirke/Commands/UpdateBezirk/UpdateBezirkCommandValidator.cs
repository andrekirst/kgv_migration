using FluentValidation;

namespace KGV.Application.Features.Bezirke.Commands.UpdateBezirk;

/// <summary>
/// Validator for UpdateBezirkCommand with German error messages
/// </summary>
public class UpdateBezirkCommandValidator : AbstractValidator<UpdateBezirkCommand>
{
    public UpdateBezirkCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Die Bezirks-ID ist erforderlich.");

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
            .WithMessage("Die Sortierreihenfolge muss größer oder gleich 0 sein.")
            .When(x => x.SortOrder.HasValue);

        RuleFor(x => x.Flaeche)
            .GreaterThan(0)
            .WithMessage("Die Fläche muss größer als 0 sein.")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Die Fläche darf maximal 1.000.000 m² betragen.")
            .When(x => x.Flaeche.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Der Status ist ungültig.")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.UpdatedBy)
            .MaximumLength(255)
            .WithMessage("Der Bearbeiter-Name darf maximal 255 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.UpdatedBy));
    }
}