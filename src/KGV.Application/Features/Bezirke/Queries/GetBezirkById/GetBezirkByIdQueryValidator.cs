using FluentValidation;

namespace KGV.Application.Features.Bezirke.Queries.GetBezirkById;

/// <summary>
/// Validator for GetBezirkByIdQuery with German error messages
/// </summary>
public class GetBezirkByIdQueryValidator : AbstractValidator<GetBezirkByIdQuery>
{
    public GetBezirkByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Die Bezirks-ID ist erforderlich.");
    }
}