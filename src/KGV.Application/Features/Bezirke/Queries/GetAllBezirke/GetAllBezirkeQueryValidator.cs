using FluentValidation;

namespace KGV.Application.Features.Bezirke.Queries.GetAllBezirke;

/// <summary>
/// Validator for GetAllBezirkeQuery with German error messages
/// </summary>
public class GetAllBezirkeQueryValidator : AbstractValidator<GetAllBezirkeQuery>
{
    public GetAllBezirkeQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Die Seitennummer muss größer als 0 sein.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Die Seitengröße muss größer als 0 sein.")
            .LessThanOrEqualTo(100)
            .WithMessage("Die Seitengröße darf maximal 100 betragen.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .WithMessage("Der Suchbegriff darf maximal 100 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.SearchTerm));

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Der Status ist ungültig.")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.MinFlaeche)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Die minimale Fläche muss größer oder gleich 0 sein.")
            .When(x => x.MinFlaeche.HasValue);

        RuleFor(x => x.MaxFlaeche)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Die maximale Fläche muss größer oder gleich 0 sein.")
            .GreaterThanOrEqualTo(x => x.MinFlaeche)
            .WithMessage("Die maximale Fläche muss größer oder gleich der minimalen Fläche sein.")
            .When(x => x.MaxFlaeche.HasValue);

        RuleFor(x => x.SortBy)
            .NotEmpty()
            .WithMessage("Das Sortierfeld ist erforderlich.")
            .Must(BeValidSortField)
            .WithMessage("Das Sortierfeld ist ungültig. Gültige Werte: Name, DisplayName, Status, IsActive, Flaeche, AnzahlParzellen, CreatedAt, UpdatedAt, SortOrder");
    }

    private bool BeValidSortField(string sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return false;

        var validSortFields = new[]
        {
            "name", "displayname", "status", "isactive", "flaeche", 
            "anzahlparzellen", "createdat", "updatedat", "sortorder"
        };

        return validSortFields.Contains(sortBy.ToLower());
    }
}