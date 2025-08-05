using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Entities;

namespace KGV.Application.Features.Parzellen.Commands.UpdateParzelle;

/// <summary>
/// Handler for UpdateParzelleCommand
/// Updates an existing plot with validation and business rules
/// </summary>
public class UpdateParzelleCommandHandler : IRequestHandler<UpdateParzelleCommand, Result<ParzelleDto>>
{
    private readonly IRepository<Parzelle> _parzelleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateParzelleCommandHandler> _logger;

    public UpdateParzelleCommandHandler(
        IRepository<Parzelle> parzelleRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateParzelleCommandHandler> logger)
    {
        _parzelleRepository = parzelleRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ParzelleDto>> Handle(UpdateParzelleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating Parzelle {ParzelleId}", request.Id);

        try
        {
            // Retrieve the existing Parzelle with Bezirk
            var parzelle = await _parzelleRepository.GetFirstOrDefaultAsync(
                p => p.Id == request.Id,
                "Bezirk",
                cancellationToken);

            if (parzelle == null)
            {
                _logger.LogWarning("Parzelle with ID {ParzelleId} not found", request.Id);
                return Result<ParzelleDto>.Failure("Die angegebene Parzelle wurde nicht gefunden.");
            }

            // Store original values for change tracking
            var originalFlaeche = parzelle.Flaeche;

            // Apply updates
            parzelle.Update(
                flaeche: request.Flaeche,
                preis: request.Preis,
                beschreibung: request.Beschreibung,
                besonderheiten: request.Besonderheiten,
                hasWasser: request.HasWasser,
                hasStrom: request.HasStrom,
                prioritaet: request.Prioritaet);

            // Set audit fields
            if (!string.IsNullOrEmpty(request.UpdatedBy))
            {
                parzelle.SetUpdatedBy(request.UpdatedBy);
            }

            // Update repository
            await _parzelleRepository.UpdateAsync(parzelle, cancellationToken);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to DTO
            var parzelleDto = _mapper.Map<ParzelleDto>(parzelle);

            _logger.LogInformation("Successfully updated Parzelle {ParzelleId} ({FullDisplayName})", 
                parzelle.Id, parzelle.GetFullDisplayName());

            // Log significant changes
            if (request.Flaeche.HasValue && originalFlaeche != request.Flaeche.Value)
            {
                _logger.LogInformation("Parzelle {ParzelleId} area changed from {OldFlaeche} to {NewFlaeche} m²", 
                    parzelle.Id, originalFlaeche, request.Flaeche.Value);
            }

            return Result<ParzelleDto>.Success(parzelleDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while updating Parzelle {ParzelleId}", request.Id);
            return Result<ParzelleDto>.Failure($"Ungültige Eingabe: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Parzelle {ParzelleId}", request.Id);
            return Result<ParzelleDto>.Failure("Ein Fehler ist beim Aktualisieren der Parzelle aufgetreten.");
        }
    }
}