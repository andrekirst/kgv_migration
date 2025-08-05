using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Entities;
using KGV.Domain.Enums;

namespace KGV.Application.Features.Parzellen.Commands.AssignParzelle;

/// <summary>
/// Handler for AssignParzelleCommand
/// Implements complex business logic for plot assignment including validation,
/// priority checking, and related plot management
/// </summary>
public class AssignParzelleCommandHandler : IRequestHandler<AssignParzelleCommand, Result<ParzelleDto>>
{
    private readonly IRepository<Parzelle> _parzelleRepository;
    private readonly IRepository<Antrag> _antragRepository;
    private readonly IRepository<Person> _personRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AssignParzelleCommandHandler> _logger;

    public AssignParzelleCommandHandler(
        IRepository<Parzelle> parzelleRepository,
        IRepository<Antrag> antragRepository,
        IRepository<Person> personRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AssignParzelleCommandHandler> logger)
    {
        _parzelleRepository = parzelleRepository;
        _antragRepository = antragRepository;
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ParzelleDto>> Handle(AssignParzelleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Assigning Parzelle {ParzelleId} to Person {PersonId} or Antrag {AntragId}", 
            request.ParzelleId, request.PersonId, request.AntragId);

        try
        {
            // Retrieve the Parzelle with Bezirk
            var parzelle = await _parzelleRepository.FirstOrDefaultAsync(
                p => p.Id == request.ParzelleId,
                p => p.Bezirk,
                cancellationToken);

            if (parzelle == null)
            {
                _logger.LogWarning("Parzelle with ID {ParzelleId} not found", request.ParzelleId);
                return Result<ParzelleDto>.Failure("Die angegebene Parzelle wurde nicht gefunden.");
            }

            // Validate business rules for assignment
            var validationResult = await ValidateAssignment(parzelle, request, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Result<ParzelleDto>.Failure(validationResult.Error!);
            }

            // Validate person or application exists
            var entityValidationResult = await ValidatePersonOrApplication(request, cancellationToken);
            if (!entityValidationResult.IsSuccess)
            {
                return Result<ParzelleDto>.Failure(entityValidationResult.Error!);
            }

            // Apply priority override if specified
            if (request.PriorityOverride.HasValue)
            {
                parzelle.Update(prioritaet: request.PriorityOverride.Value);
                _logger.LogInformation("Applied priority override {Priority} to Parzelle {ParzelleId}", 
                    request.PriorityOverride.Value, parzelle.Id);
            }

            // Perform the assignment
            var assignmentDate = request.AssignmentDate ?? DateTime.UtcNow;
            parzelle.Assign(assignmentDate);

            // Add assignment notes to description if provided
            if (!string.IsNullOrWhiteSpace(request.AssignmentNotes))
            {
                var currentDescription = parzelle.Beschreibung ?? string.Empty;
                var assignmentNote = $"[{assignmentDate:yyyy-MM-dd}] Vergabe: {request.AssignmentNotes}";
                var newDescription = string.IsNullOrWhiteSpace(currentDescription) 
                    ? assignmentNote 
                    : $"{currentDescription}\n{assignmentNote}";
                
                parzelle.Update(beschreibung: newDescription);
            }

            // Handle related plots reservation if requested
            if (request.ReserveRelatedPlots)
            {
                await ReserveRelatedPlots(parzelle, request.AssignedBy, cancellationToken);
            }

            // Set audit fields
            if (!string.IsNullOrEmpty(request.AssignedBy))
            {
                parzelle.SetUpdatedBy(request.AssignedBy);
            }

            // Update repository
            _parzelleRepository.Update(parzelle);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to DTO
            var parzelleDto = _mapper.Map<ParzelleDto>(parzelle);

            _logger.LogInformation("Successfully assigned Parzelle {ParzelleId} ({FullDisplayName}) on {Date}", 
                parzelle.Id, parzelle.GetFullDisplayName(), assignmentDate);

            return Result<ParzelleDto>.Success(parzelleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning Parzelle {ParzelleId}", request.ParzelleId);
            return Result<ParzelleDto>.Failure("Ein Fehler ist bei der Vergabe der Parzelle aufgetreten.");
        }
    }

    private async Task<Result> ValidateAssignment(Parzelle parzelle, AssignParzelleCommand request, CancellationToken cancellationToken)
    {
        // Check if plot is available for assignment
        if (!request.ForceAssignment && !parzelle.IsAvailableForAssignment())
        {
            _logger.LogWarning("Parzelle {ParzelleId} is not available for assignment. Status: {Status}", 
                parzelle.Id, parzelle.Status);
            return Result.Failure($"Die Parzelle '{parzelle.GetFullDisplayName()}' ist nicht für eine Vergabe verfügbar. " +
                $"Status: {parzelle.GetStatusDescription()}. Verwenden Sie 'ForceAssignment = true', um dies zu überschreiben.");
        }

        // Check if Bezirk can accept assignments
        if (parzelle.Bezirk != null && !parzelle.Bezirk.CanAcceptNewPlots())
        {
            _logger.LogWarning("Bezirk {BezirkId} cannot accept new assignments. Status: {Status}", 
                parzelle.BezirkId, parzelle.Bezirk.Status);
            return Result.Failure($"Der Bezirk '{parzelle.Bezirk.GetDisplayName()}' kann keine neuen Vergaben annehmen. " +
                $"Status: {parzelle.Bezirk.GetStatusDescription()}");
        }

        // Ensure either PersonId or AntragId is provided
        if (!request.PersonId.HasValue && !request.AntragId.HasValue)
        {
            return Result.Failure("Entweder eine Personen-ID oder eine Antrags-ID muss angegeben werden.");
        }

        return Result.Success();
    }

    private async Task<Result> ValidatePersonOrApplication(AssignParzelleCommand request, CancellationToken cancellationToken)
    {
        if (request.PersonId.HasValue)
        {
            var person = await _personRepository.FirstOrDefaultAsync(
                p => p.Id == request.PersonId.Value,
                cancellationToken);

            if (person == null)
            {
                _logger.LogWarning("Person with ID {PersonId} not found", request.PersonId);
                return Result.Failure("Die angegebene Person wurde nicht gefunden.");
            }
        }

        if (request.AntragId.HasValue)
        {
            var antrag = await _antragRepository.FirstOrDefaultAsync(
                a => a.Id == request.AntragId.Value,
                cancellationToken);

            if (antrag == null)
            {
                _logger.LogWarning("Antrag with ID {AntragId} not found", request.AntragId);
                return Result.Failure("Der angegebene Antrag wurde nicht gefunden.");
            }

            // Check if application is in a status that allows assignment
            if (antrag.Status != AntragStatus.InReview && antrag.Status != AntragStatus.Approved)
            {
                _logger.LogWarning("Antrag {AntragId} is not in a status that allows assignment. Status: {Status}", 
                    request.AntragId, antrag.Status);
                return Result.Failure($"Der Antrag befindet sich nicht in einem Status, der eine Vergabe erlaubt. " +
                    $"Aktueller Status: {antrag.Status}");
            }
        }

        return Result.Success();
    }

    private async Task ReserveRelatedPlots(Parzelle assignedParzelle, string? assignedBy, CancellationToken cancellationToken)
    {
        try
        {
            // Find plots in the same Bezirk that are available and might be related
            var relatedPlots = await _parzelleRepository.GetAllAsync(
                p => p.BezirkId == assignedParzelle.BezirkId &&
                     p.Id != assignedParzelle.Id &&
                     p.Status == ParzellenStatus.Available &&
                     p.Prioritaet == assignedParzelle.Prioritaet, // Same priority level
                cancellationToken);

            var plotsToReserve = relatedPlots
                .Where(p => IsRelatedPlot(assignedParzelle, p))
                .Take(2) // Limit to 2 related plots to avoid over-reservation
                .ToList();

            foreach (var plot in plotsToReserve)
            {
                plot.Reserve();
                if (!string.IsNullOrEmpty(assignedBy))
                {
                    plot.SetUpdatedBy(assignedBy);
                }
                _parzelleRepository.Update(plot);

                _logger.LogInformation("Reserved related Parzelle {ParzelleId} ({FullDisplayName}) due to assignment of {AssignedParzelleId}", 
                    plot.Id, plot.GetFullDisplayName(), assignedParzelle.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reserving related plots for Parzelle {ParzelleId}", assignedParzelle.Id);
            // Don't fail the main assignment if related plot reservation fails
        }
    }

    private bool IsRelatedPlot(Parzelle assignedPlot, Parzelle candidateePlot)
    {
        // Simple heuristic: plots are related if they have similar numbers or are adjacent
        // This is a simplified example - in real scenarios, you might have more complex logic
        
        // Check if plot numbers are sequential or similar
        if (TryExtractNumber(assignedPlot.Nummer, out var assignedNumber) &&
            TryExtractNumber(candidateePlot.Nummer, out var candidateNumber))
        {
            return Math.Abs(assignedNumber - candidateNumber) <= 2;
        }

        // Check if they have similar characteristics
        return assignedPlot.HasWasser == candidateePlot.HasWasser &&
               assignedPlot.HasStrom == candidateePlot.HasStrom &&
               Math.Abs(assignedPlot.Flaeche - candidateePlot.Flaeche) <= 50; // Within 50 m²
    }

    private bool TryExtractNumber(string plotNumber, out int number)
    {
        number = 0;
        
        // Extract digits from plot number (e.g., "A123" -> 123, "Plot-456" -> 456)
        var digits = new string(plotNumber.Where(char.IsDigit).ToArray());
        
        return int.TryParse(digits, out number);
    }
}