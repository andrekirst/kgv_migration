using MediatR;
using Microsoft.Extensions.Logging;
using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Domain.Entities;
using KGV.Domain.Enums;

namespace KGV.Application.Features.Parzellen.Commands.DeleteParzelle;

/// <summary>
/// Handler for DeleteParzelleCommand
/// Deletes or decommissions a plot with business rules validation
/// </summary>
public class DeleteParzelleCommandHandler : IRequestHandler<DeleteParzelleCommand, Result>
{
    private readonly IRepository<Parzelle> _parzelleRepository;
    private readonly IRepository<Bezirk> _bezirkRepository;
    private readonly IRepository<Antrag> _antragRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteParzelleCommandHandler> _logger;

    public DeleteParzelleCommandHandler(
        IRepository<Parzelle> parzelleRepository,
        IRepository<Bezirk> bezirkRepository,
        IRepository<Antrag> antragRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteParzelleCommandHandler> logger)
    {
        _parzelleRepository = parzelleRepository;
        _bezirkRepository = bezirkRepository;
        _antragRepository = antragRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteParzelleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to delete Parzelle {ParzelleId}", request.Id);

        try
        {
            // Retrieve the Parzelle with Bezirk
            var parzelle = await _parzelleRepository.GetFirstOrDefaultAsync(
                p => p.Id == request.Id,
                "Bezirk",
                cancellationToken);

            if (parzelle == null)
            {
                _logger.LogWarning("Parzelle with ID {ParzelleId} not found", request.Id);
                return Result.Failure("Die angegebene Parzelle wurde nicht gefunden.");
            }

            // Check if there are associated applications
            var hasAssociatedApplications = await _antragRepository.AnyAsync(
                a => a.ParzelleId == request.Id,
                cancellationToken);

            // Validate deletion conditions
            var validationResult = ValidateDeletion(parzelle, hasAssociatedApplications, request);
            if (!validationResult.IsSuccess)
            {
                return validationResult;
            }

            // Handle transfer of existing assignments if requested
            if (request.TransferExistingAssignments && parzelle.Status == ParzellenStatus.Assigned)
            {
                var transferResult = await TransferExistingAssignment(parzelle, request.DeletedBy, cancellationToken);
                if (!transferResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to transfer assignment for Parzelle {ParzelleId}: {Error}", 
                        request.Id, transferResult.Error);
                    return Result.Failure($"Übertragung der bestehenden Vergabe fehlgeschlagen: {transferResult.Error}");
                }
            }

            // Determine action based on conditions
            if (hasAssociatedApplications && request.ForceDelete)
            {
                // Decommission instead of deleting when there are applications
                _logger.LogInformation("Decommissioning Parzelle {ParzelleId} due to associated applications", request.Id);
                
                parzelle.ChangeStatus(ParzellenStatus.Decommissioned);
                
                // Add decommission note
                var decommissionNote = $"[{DateTime.UtcNow:yyyy-MM-dd}] Stillgelegt: {request.DeletionReason ?? "Löschung mit verknüpften Anträgen"}";
                var currentDescription = parzelle.Beschreibung ?? string.Empty;
                var newDescription = string.IsNullOrWhiteSpace(currentDescription) 
                    ? decommissionNote 
                    : $"{currentDescription}\n{decommissionNote}";
                
                parzelle.Update(beschreibung: newDescription);
                
                if (!string.IsNullOrEmpty(request.DeletedBy))
                {
                    parzelle.SetUpdatedBy(request.DeletedBy);
                }

                await _parzelleRepository.UpdateAsync(parzelle, cancellationToken);
                
                _logger.LogInformation("Successfully decommissioned Parzelle {ParzelleId} ({FullDisplayName})", 
                    parzelle.Id, parzelle.GetFullDisplayName());
            }
            else
            {
                // Actual deletion when no applications exist
                _logger.LogInformation("Deleting Parzelle {ParzelleId} - no associated applications", request.Id);
                
                _parzelleRepository.Remove(parzelle);
                
                // Update Bezirk plot count
                if (parzelle.Bezirk != null)
                {
                    parzelle.Bezirk.DecrementPlotCount();
                    await _bezirkRepository.UpdateAsync(parzelle.Bezirk, cancellationToken);
                }
                
                _logger.LogInformation("Successfully deleted Parzelle {ParzelleId} ({FullDisplayName})", 
                    parzelle.Id, parzelle.GetFullDisplayName());
            }

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Parzelle {ParzelleId}", request.Id);
            return Result.Failure("Ein Fehler ist beim Löschen der Parzelle aufgetreten.");
        }
    }

    private Result ValidateDeletion(Parzelle parzelle, bool hasAssociatedApplications, DeleteParzelleCommand request)
    {
        // Check if plot can be deleted
        if (hasAssociatedApplications && !request.ForceDelete)
        {
            _logger.LogWarning("Cannot delete Parzelle {ParzelleId} - has associated applications", parzelle.Id);
            return Result.Failure("Die Parzelle kann nicht gelöscht werden, da noch Anträge zugeordnet sind. " +
                "Verwenden Sie 'ForceDelete = true', um die Parzelle stillzulegen.");
        }

        // Check if plot is currently assigned and no transfer is requested
        if (parzelle.Status == ParzellenStatus.Assigned && !request.TransferExistingAssignments && !request.ForceDelete)
        {
            _logger.LogWarning("Cannot delete assigned Parzelle {ParzelleId} without transfer or force", parzelle.Id);
            return Result.Failure("Die Parzelle ist derzeit vergeben und kann nicht gelöscht werden. " +
                "Verwenden Sie 'TransferExistingAssignments = true' oder 'ForceDelete = true'.");
        }

        // Require deletion reason for assigned or reserved plots
        if ((parzelle.Status == ParzellenStatus.Assigned || parzelle.Status == ParzellenStatus.Reserved) &&
            string.IsNullOrWhiteSpace(request.DeletionReason))
        {
            return Result.Failure("Für die Löschung einer vergebenen oder reservierten Parzelle ist eine Begründung erforderlich.");
        }

        return Result.Success();
    }

    private async Task<Result> TransferExistingAssignment(Parzelle parzelle, string? deletedBy, CancellationToken cancellationToken)
    {
        try
        {
            // Find alternative plots in the same Bezirk
            var alternativePlots = await _parzelleRepository.GetAllAsync(
                p => p.BezirkId == parzelle.BezirkId &&
                     p.Id != parzelle.Id &&
                     p.Status == ParzellenStatus.Available &&
                     Math.Abs(p.Flaeche - parzelle.Flaeche) <= 100, // Similar size (within 100 m²)
                null,
                "",
                cancellationToken);

            var bestAlternative = alternativePlots
                .OrderBy(p => Math.Abs(p.Flaeche - parzelle.Flaeche)) // Closest in size
                .ThenBy(p => p.Prioritaet) // Lowest priority first
                .FirstOrDefault();

            if (bestAlternative == null)
            {
                return Result.Failure("Keine geeignete alternative Parzelle für die Übertragung gefunden.");
            }

            // Transfer the assignment
            bestAlternative.Assign(parzelle.VergebenAm);
            
            var transferNote = $"[{DateTime.UtcNow:yyyy-MM-dd}] Übertragung von Parzelle {parzelle.GetFullDisplayName()} aufgrund Löschung.";
            bestAlternative.Update(
                beschreibung: string.IsNullOrWhiteSpace(bestAlternative.Beschreibung) 
                    ? transferNote 
                    : $"{bestAlternative.Beschreibung}\n{transferNote}");

            if (!string.IsNullOrEmpty(deletedBy))
            {
                bestAlternative.SetUpdatedBy(deletedBy);
            }

            await _parzelleRepository.UpdateAsync(bestAlternative, cancellationToken);

            _logger.LogInformation("Transferred assignment from Parzelle {OldParzelleId} to {NewParzelleId}", 
                parzelle.Id, bestAlternative.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring assignment from Parzelle {ParzelleId}", parzelle.Id);
            return Result.Failure("Fehler bei der Übertragung der Vergabe.");
        }
    }
}