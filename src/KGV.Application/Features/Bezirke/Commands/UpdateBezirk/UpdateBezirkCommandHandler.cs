using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Entities;

namespace KGV.Application.Features.Bezirke.Commands.UpdateBezirk;

/// <summary>
/// Handler for UpdateBezirkCommand
/// Updates an existing district with validation and business rules
/// </summary>
public class UpdateBezirkCommandHandler : IRequestHandler<UpdateBezirkCommand, Result<BezirkDto>>
{
    private readonly IRepository<Bezirk> _bezirkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateBezirkCommandHandler> _logger;

    public UpdateBezirkCommandHandler(
        IRepository<Bezirk> bezirkRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateBezirkCommandHandler> logger)
    {
        _bezirkRepository = bezirkRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<BezirkDto>> Handle(UpdateBezirkCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating Bezirk {BezirkId}", request.Id);

        try
        {
            // Retrieve the existing Bezirk
            var bezirk = await _bezirkRepository.FirstOrDefaultAsync(
                b => b.Id == request.Id,
                cancellationToken);

            if (bezirk == null)
            {
                _logger.LogWarning("Bezirk with ID {BezirkId} not found", request.Id);
                return Result<BezirkDto>.Failure("Der angegebene Bezirk wurde nicht gefunden.");
            }

            // Apply updates only if values are provided
            bezirk.Update(
                displayName: request.DisplayName,
                description: request.Beschreibung,
                sortOrder: request.SortOrder,
                flaeche: request.Flaeche);

            // Update status if provided
            if (request.Status.HasValue)
            {
                bezirk.ChangeStatus(request.Status.Value);
            }

            // Set audit fields
            if (!string.IsNullOrEmpty(request.UpdatedBy))
            {
                bezirk.SetUpdatedBy(request.UpdatedBy);
            }

            // Update repository
            _bezirkRepository.Update(bezirk);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to DTO
            var bezirkDto = _mapper.Map<BezirkDto>(bezirk);

            _logger.LogInformation("Successfully updated Bezirk {BezirkId}", bezirk.Id);

            return Result<BezirkDto>.Success(bezirkDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while updating Bezirk {BezirkId}", request.Id);
            return Result<BezirkDto>.Failure($"Ungültige Eingabe: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Bezirk {BezirkId}", request.Id);
            return Result<BezirkDto>.Failure("Ein Fehler ist beim Aktualisieren des Bezirks aufgetreten.");
        }
    }
}