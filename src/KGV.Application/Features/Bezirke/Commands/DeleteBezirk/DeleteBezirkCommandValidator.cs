using FluentValidation;

namespace KGV.Application.Features.Bezirke.Commands.DeleteBezirk;

/// <summary>
/// Validator for DeleteBezirkCommand with German error messages
/// </summary>
public class DeleteBezirkCommandValidator : AbstractValidator<DeleteBezirkCommand>
{
    public DeleteBezirkCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Die Bezirks-ID ist erforderlich.");

        RuleFor(x => x.DeletedBy)
            .MaximumLength(255)
            .WithMessage("Der Name des löschenden Benutzers darf maximal 255 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.DeletedBy));
    }
}