using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Entities;

namespace KGV.Application.Features.Bezirke.Commands.CreateBezirk;

/// <summary>
/// Handler for CreateBezirkCommand
/// Creates a new district with validation and business rules
/// </summary>
public class CreateBezirkCommandHandler : IRequestHandler<CreateBezirkCommand, Result<BezirkDto>>
{
    private readonly IRepository<Bezirk> _bezirkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateBezirkCommandHandler> _logger;

    public CreateBezirkCommandHandler(
        IRepository<Bezirk> bezirkRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateBezirkCommandHandler> logger)
    {
        _bezirkRepository = bezirkRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<BezirkDto>> Handle(CreateBezirkCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new Bezirk with name: {Name}", request.Name);

        try
        {
            // Check if Bezirk with same name already exists
            var existingBezirk = await _bezirkRepository.FirstOrDefaultAsync(
                b => b.Name.ToUpper() == request.Name.Trim().ToUpper(),
                cancellationToken);

            if (existingBezirk != null)
            {
                _logger.LogWarning("Bezirk with name {Name} already exists", request.Name);
                return Result<BezirkDto>.Failure($"Ein Bezirk mit dem Namen '{request.Name}' existiert bereits.");
            }

            // Create the domain entity
            var bezirk = Bezirk.Create(
                name: request.Name,
                displayName: request.DisplayName,
                description: request.Beschreibung,
                sortOrder: request.SortOrder,
                flaeche: request.Flaeche,
                status: request.Status);

            // Set audit fields
            if (!string.IsNullOrEmpty(request.CreatedBy))
            {
                bezirk.SetCreatedBy(request.CreatedBy);
            }

            // Add to repository
            await _bezirkRepository.AddAsync(bezirk, cancellationToken);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to DTO
            var bezirkDto = _mapper.Map<BezirkDto>(bezirk);

            _logger.LogInformation("Successfully created Bezirk {BezirkId} with name: {Name}", 
                bezirk.Id, bezirk.Name);

            return Result<BezirkDto>.Success(bezirkDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating Bezirk with name: {Name}", request.Name);
            return Result<BezirkDto>.Failure($"Ungültige Eingabe: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Bezirk with name: {Name}", request.Name);
            return Result<BezirkDto>.Failure("Ein Fehler ist beim Erstellen des Bezirks aufgetreten.");
        }
    }
}