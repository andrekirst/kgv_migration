using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Entities;

namespace KGV.Application.Features.Parzellen.Commands.CreateParzelle;

/// <summary>
/// Handler for CreateParzelleCommand
/// Creates a new plot with validation and business rules
/// </summary>
public class CreateParzelleCommandHandler : IRequestHandler<CreateParzelleCommand, Result<ParzelleDto>>
{
    private readonly IRepository<Parzelle> _parzelleRepository;
    private readonly IRepository<Bezirk> _bezirkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateParzelleCommandHandler> _logger;

    public CreateParzelleCommandHandler(
        IRepository<Parzelle> parzelleRepository,
        IRepository<Bezirk> bezirkRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateParzelleCommandHandler> logger)
    {
        _parzelleRepository = parzelleRepository;
        _bezirkRepository = bezirkRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ParzelleDto>> Handle(CreateParzelleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new Parzelle with number: {Nummer} in Bezirk: {BezirkId}", 
            request.Nummer, request.BezirkId);

        try
        {
            // Verify that the Bezirk exists and can accept new plots
            var bezirk = await _bezirkRepository.FirstOrDefaultAsync(
                b => b.Id == request.BezirkId,
                cancellationToken);

            if (bezirk == null)
            {
                _logger.LogWarning("Bezirk with ID {BezirkId} not found", request.BezirkId);
                return Result<ParzelleDto>.Failure("Der angegebene Bezirk wurde nicht gefunden.");
            }

            if (!bezirk.CanAcceptNewPlots())
            {
                _logger.LogWarning("Bezirk {BezirkId} cannot accept new plots. Status: {Status}", 
                    request.BezirkId, bezirk.Status);
                return Result<ParzelleDto>.Failure($"Der Bezirk '{bezirk.GetDisplayName()}' kann keine neuen Parzellen aufnehmen. Status: {bezirk.GetStatusDescription()}");
            }

            // Check if Parzelle with same number already exists in this Bezirk
            var existingParzelle = await _parzelleRepository.FirstOrDefaultAsync(
                p => p.BezirkId == request.BezirkId && p.Nummer.ToUpper() == request.Nummer.Trim().ToUpper(),
                cancellationToken);

            if (existingParzelle != null)
            {
                _logger.LogWarning("Parzelle with number {Nummer} already exists in Bezirk {BezirkId}", 
                    request.Nummer, request.BezirkId);
                return Result<ParzelleDto>.Failure($"Eine Parzelle mit der Nummer '{request.Nummer}' existiert bereits im Bezirk '{bezirk.GetDisplayName()}'.");
            }

            // Create the domain entity
            var parzelle = Parzelle.Create(
                nummer: request.Nummer,
                bezirkId: request.BezirkId,
                flaeche: request.Flaeche,
                status: request.Status,
                preis: request.Preis,
                beschreibung: request.Beschreibung,
                hasWasser: request.HasWasser,
                hasStrom: request.HasStrom,
                prioritaet: request.Prioritaet);

            // Set audit fields
            if (!string.IsNullOrEmpty(request.CreatedBy))
            {
                parzelle.SetCreatedBy(request.CreatedBy);
            }

            // Add to repository
            await _parzelleRepository.AddAsync(parzelle, cancellationToken);

            // Update Bezirk plot count
            bezirk.IncrementPlotCount();
            _bezirkRepository.Update(bezirk);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Load the created Parzelle with Bezirk for mapping
            var createdParzelle = await _parzelleRepository.FirstOrDefaultAsync(
                p => p.Id == parzelle.Id,
                p => p.Bezirk,
                cancellationToken);

            // Map to DTO
            var parzelleDto = _mapper.Map<ParzelleDto>(createdParzelle);

            _logger.LogInformation("Successfully created Parzelle {ParzelleId} with number: {Nummer} in Bezirk: {BezirkName}", 
                parzelle.Id, parzelle.Nummer, bezirk.Name);

            return Result<ParzelleDto>.Success(parzelleDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating Parzelle with number: {Nummer}", request.Nummer);
            return Result<ParzelleDto>.Failure($"Ungültige Eingabe: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Parzelle with number: {Nummer} in Bezirk: {BezirkId}", 
                request.Nummer, request.BezirkId);
            return Result<ParzelleDto>.Failure("Ein Fehler ist beim Erstellen der Parzelle aufgetreten.");
        }
    }
}