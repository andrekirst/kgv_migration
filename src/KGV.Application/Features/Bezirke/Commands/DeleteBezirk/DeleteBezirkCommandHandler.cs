using MediatR;
using Microsoft.Extensions.Logging;
using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Domain.Entities;

namespace KGV.Application.Features.Bezirke.Commands.DeleteBezirk;

/// <summary>
/// Handler for DeleteBezirkCommand
/// Deletes or archives a district with business rules validation
/// </summary>
public class DeleteBezirkCommandHandler : IRequestHandler<DeleteBezirkCommand, Result>
{
    private readonly IRepository<Bezirk> _bezirkRepository;
    private readonly IRepository<Parzelle> _parzelleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteBezirkCommandHandler> _logger;

    public DeleteBezirkCommandHandler(
        IRepository<Bezirk> bezirkRepository,
        IRepository<Parzelle> parzelleRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteBezirkCommandHandler> logger)
    {
        _bezirkRepository = bezirkRepository;
        _parzelleRepository = parzelleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteBezirkCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to delete Bezirk {BezirkId}", request.Id);

        try
        {
            // Retrieve the Bezirk
            var bezirk = await _bezirkRepository.GetFirstOrDefaultAsync(
                b => b.Id == request.Id,
                cancellationToken);

            if (bezirk == null)
            {
                _logger.LogWarning("Bezirk with ID {BezirkId} not found", request.Id);
                return Result.Failure("Der angegebene Bezirk wurde nicht gefunden.");
            }

            // Check if there are associated Parzellen
            var hasActiveParzellen = await _parzelleRepository.ExistsAsync(
                p => p.BezirkId == request.Id,
                cancellationToken);

            if (hasActiveParzellen && !request.ForceDelete)
            {
                _logger.LogWarning("Cannot delete Bezirk {BezirkId} - has active Parzellen", request.Id);
                return Result.Failure("Der Bezirk kann nicht gelöscht werden, da noch Parzellen zugeordnet sind. " +
                    "Verwenden Sie 'ForceDelete = true', um den Bezirk zu archivieren.");
            }

            if (hasActiveParzellen && request.ForceDelete)
            {
                // Archive instead of deleting when there are Parzellen
                _logger.LogInformation("Archiving Bezirk {BezirkId} due to active Parzellen", request.Id);
                
                bezirk.Archive();
                
                if (!string.IsNullOrEmpty(request.DeletedBy))
                {
                    bezirk.SetUpdatedBy(request.DeletedBy);
                }

                await _bezirkRepository.UpdateAsync(bezirk, cancellationToken);
                
                _logger.LogInformation("Successfully archived Bezirk {BezirkId}", bezirk.Id);
            }
            else
            {
                // Actual deletion when no Parzellen exist
                _logger.LogInformation("Deleting Bezirk {BezirkId} - no active Parzellen", request.Id);
                
                _bezirkRepository.Remove(bezirk);
                
                _logger.LogInformation("Successfully deleted Bezirk {BezirkId}", bezirk.Id);
            }

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Bezirk {BezirkId}", request.Id);
            return Result.Failure("Ein Fehler ist beim Löschen des Bezirks aufgetreten.");
        }
    }
}