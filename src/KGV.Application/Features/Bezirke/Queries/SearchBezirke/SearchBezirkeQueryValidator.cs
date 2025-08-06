using FluentValidation;

namespace KGV.Application.Features.Bezirke.Queries.SearchBezirke;

/// <summary>
/// Validator for SearchBezirkeQuery with German error messages
/// </summary>
public class SearchBezirkeQueryValidator : AbstractValidator<SearchBezirkeQuery>
{
    public SearchBezirkeQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty()
            .WithMessage("Der Suchbegriff ist erforderlich.")
            .MinimumLength(2)
            .WithMessage("Der Suchbegriff muss mindestens 2 Zeichen lang sein.")
            .MaximumLength(100)
            .WithMessage("Der Suchbegriff darf maximal 100 Zeichen lang sein.");

        RuleFor(x => x.MaxResults)
            .GreaterThan(0)
            .WithMessage("Die maximale Anzahl Ergebnisse muss größer als 0 sein.")
            .LessThanOrEqualTo(200)
            .WithMessage("Die maximale Anzahl Ergebnisse darf 200 nicht überschreiten.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Der Status ist ungültig.")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.MinRelevanceScore)
            .GreaterThanOrEqualTo(0.0)
            .WithMessage("Der minimale Relevanz-Score muss größer oder gleich 0.0 sein.")
            .LessThanOrEqualTo(1.0)
            .WithMessage("Der minimale Relevanz-Score darf maximal 1.0 betragen.");
    }
}