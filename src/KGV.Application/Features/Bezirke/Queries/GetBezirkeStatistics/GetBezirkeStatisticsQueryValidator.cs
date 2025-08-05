using FluentValidation;

namespace KGV.Application.Features.Bezirke.Queries.GetBezirkeStatistics;

/// <summary>
/// Validator for GetBezirkeStatisticsQuery with German error messages
/// </summary>
public class GetBezirkeStatisticsQueryValidator : AbstractValidator<GetBezirkeStatisticsQuery>
{
    public GetBezirkeStatisticsQueryValidator()
    {
        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Das Startdatum darf nicht in der Zukunft liegen.")
            .When(x => x.FromDate.HasValue);

        RuleFor(x => x.ToDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Das Enddatum darf nicht in der Zukunft liegen.")
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("Das Enddatum muss nach dem Startdatum liegen.")
            .When(x => x.ToDate.HasValue);

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .WithMessage("Das Startdatum muss vor dem Enddatum liegen.")
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}