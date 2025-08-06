using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Entities;
using System.Linq.Expressions;

namespace KGV.Application.Features.Bezirke.Queries.GetBezirkById;

/// <summary>
/// Handler for GetBezirkByIdQuery
/// Retrieves a specific district by ID with optional related data
/// </summary>
public class GetBezirkByIdQueryHandler : IRequestHandler<GetBezirkByIdQuery, Result<BezirkDto>>
{
    private readonly IRepository<Bezirk> _bezirkRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetBezirkByIdQueryHandler> _logger;

    public GetBezirkByIdQueryHandler(
        IRepository<Bezirk> bezirkRepository,
        IMapper mapper,
        ILogger<GetBezirkByIdQueryHandler> logger)
    {
        _bezirkRepository = bezirkRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<BezirkDto>> Handle(GetBezirkByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting Bezirk by ID: {BezirkId}, IncludeKatasterbezirke: {IncludeKatasterbezirke}, IncludeParzellen: {IncludeParzellen}", 
            request.Id, request.IncludeKatasterbezirke, request.IncludeParzellen);

        try
        {
            // Build includes based on request
            var includes = BuildIncludes(request);

            // Get the Bezirk
            var bezirk = await _bezirkRepository.FirstOrDefaultAsync(
                b => b.Id == request.Id,
                includes,
                cancellationToken);

            if (bezirk == null)
            {
                _logger.LogWarning("Bezirk with ID {BezirkId} not found", request.Id);
                return Result<BezirkDto>.Failure("Der angegebene Bezirk wurde nicht gefunden.");
            }

            // Map to DTO
            var bezirkDto = _mapper.Map<BezirkDto>(bezirk);

            // Set related data counts if not included
            if (!request.IncludeKatasterbezirke)
            {
                bezirkDto.Katasterbezirke.Clear();
            }

            _logger.LogInformation("Successfully retrieved Bezirk {BezirkId} ({Name})", bezirk.Id, bezirk.Name);

            return Result<BezirkDto>.Success(bezirkDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Bezirk by ID: {BezirkId}", request.Id);
            return Result<BezirkDto>.Failure("Ein Fehler ist beim Abrufen des Bezirks aufgetreten.");
        }
    }

    private Expression<Func<Bezirk, object>>[] BuildIncludes(GetBezirkByIdQuery request)
    {
        var includes = new List<Expression<Func<Bezirk, object>>>();

        if (request.IncludeKatasterbezirke)
        {
            includes.Add(b => b.Katasterbezirke);
        }

        if (request.IncludeParzellen)
        {
            includes.Add(b => b.Parzellen);
        }

        return includes.ToArray();
    }
}