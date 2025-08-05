using FluentValidation;

namespace KGV.Application.Features.Parzellen.Commands.DeleteParzelle;

/// <summary>
/// Validator for DeleteParzelleCommand with German error messages
/// </summary>
public class DeleteParzelleCommandValidator : AbstractValidator<DeleteParzelleCommand>
{
    public DeleteParzelleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Die Parzellen-ID ist erforderlich.");

        RuleFor(x => x.DeletedBy)
            .MaximumLength(255)
            .WithMessage("Der Name des löschenden Benutzers darf maximal 255 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.DeletedBy));

        RuleFor(x => x.DeletionReason)
            .NotEmpty()
            .WithMessage("Ein Löschgrund ist erforderlich.")
            .MaximumLength(1000)
            .WithMessage("Der Löschgrund darf maximal 1.000 Zeichen lang sein.")
            .When(x => x.ForceDelete || x.TransferExistingAssignments);

        RuleFor(x => x.DeletionReason)
            .MaximumLength(1000)
            .WithMessage("Der Löschgrund darf maximal 1.000 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.DeletionReason));

        // Business rule: require reason when forcing delete or transferring assignments
        RuleFor(x => x)
            .Must(HaveDeletionReasonWhenRequired)
            .WithMessage("Ein Löschgrund ist erforderlich bei erzwungener Löschung oder Übertragung bestehender Vergaben.");
    }

    private bool HaveDeletionReasonWhenRequired(DeleteParzelleCommand command)
    {
        if (command.ForceDelete || command.TransferExistingAssignments)
        {
            return !string.IsNullOrWhiteSpace(command.DeletionReason);
        }

        return true;
    }
}