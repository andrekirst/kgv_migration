using FluentValidation;

namespace KGV.Application.Features.Parzellen.Commands.AssignParzelle;

/// <summary>
/// Validator for AssignParzelleCommand with German error messages
/// </summary>
public class AssignParzelleCommandValidator : AbstractValidator<AssignParzelleCommand>
{
    public AssignParzelleCommandValidator()
    {
        RuleFor(x => x.ParzelleId)
            .NotEmpty()
            .WithMessage("Die Parzellen-ID ist erforderlich.");

        RuleFor(x => x)
            .Must(HavePersonOrApplication)
            .WithMessage("Entweder eine Personen-ID oder eine Antrags-ID muss angegeben werden.");

        RuleFor(x => x.PersonId)
            .NotEmpty()
            .WithMessage("Die Personen-ID ist erforderlich wenn keine Antrags-ID angegeben wird.")
            .When(x => !x.AntragId.HasValue);

        RuleFor(x => x.AntragId)
            .NotEmpty()
            .WithMessage("Die Antrags-ID ist erforderlich wenn keine Personen-ID angegeben wird.")
            .When(x => !x.PersonId.HasValue);

        RuleFor(x => x.AssignmentDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(30))
            .WithMessage("Das Vergabedatum darf nicht mehr als 30 Tage in der Zukunft liegen.")
            .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-1))
            .WithMessage("Das Vergabedatum darf nicht mehr als 1 Jahr in der Vergangenheit liegen.")
            .When(x => x.AssignmentDate.HasValue);

        RuleFor(x => x.AssignmentNotes)
            .MaximumLength(500)
            .WithMessage("Die Vergabe-Notizen dürfen maximal 500 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.AssignmentNotes));

        RuleFor(x => x.PriorityOverride)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Die Prioritäts-Überschreibung muss größer oder gleich 0 sein.")
            .LessThanOrEqualTo(1000)
            .WithMessage("Die Prioritäts-Überschreibung darf maximal 1.000 betragen.")
            .When(x => x.PriorityOverride.HasValue);

        RuleFor(x => x.AssignedBy)
            .MaximumLength(255)
            .WithMessage("Der Name des zuweisenden Benutzers darf maximal 255 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.AssignedBy));

        RuleFor(x => x.AssignmentReason)
            .MaximumLength(1000)
            .WithMessage("Der Vergabegrund darf maximal 1.000 Zeichen lang sein.")
            .When(x => !string.IsNullOrEmpty(x.AssignmentReason));

        // Business rule validations
        RuleFor(x => x)
            .Must(HaveAssignmentReasonWhenForcing)
            .WithMessage("Bei einer erzwungenen Vergabe muss ein Grund angegeben werden.");
    }

    private bool HavePersonOrApplication(AssignParzelleCommand command)
    {
        return command.PersonId.HasValue || command.AntragId.HasValue;
    }

    private bool HaveAssignmentReasonWhenForcing(AssignParzelleCommand command)
    {
        if (!command.ForceAssignment)
            return true;

        return !string.IsNullOrWhiteSpace(command.AssignmentReason);
    }
}