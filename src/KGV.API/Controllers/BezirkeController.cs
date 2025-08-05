using KGV.API.Models;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Application.Features.Bezirke.Commands.CreateBezirk;
using KGV.Application.Features.Bezirke.Commands.DeleteBezirk;
using KGV.Application.Features.Bezirke.Commands.UpdateBezirk;
using KGV.Application.Features.Bezirke.DTOs;
using KGV.Application.Features.Bezirke.Queries.GetAllBezirke;
using KGV.Application.Features.Bezirke.Queries.GetBezirkById;
using KGV.Application.Features.Bezirke.Queries.GetBezirkeStatistics;
using KGV.Application.Features.Bezirke.Queries.SearchBezirke;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace KGV.API.Controllers;

/// <summary>
/// Controller for managing Bezirke (Districts)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("DefaultPolicy")]
[SwaggerTag("Bezirke (Districts) management operations")]
public class BezirkeController : BaseApiController
{
    public BezirkeController(IMediator mediator, ILogger<BezirkeController> logger) 
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Gets a paginated list of districts with filtering and sorting options
    /// </summary>
    /// <param name="parameters">Query parameters for filtering and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of districts</returns>
    /// <response code="200">Success - Returns paginated list of districts</response>
    /// <response code="400">Bad Request - Invalid query parameters</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get districts",
        Description = "Retrieves a paginated list of districts with comprehensive filtering and sorting options. " +
                     "Supports filtering by active status, search terms, and including related data."
    )]
    [SwaggerResponse(200, "Success", typeof(PaginatedResult<BezirkListDto>))]
    [SwaggerResponse(400, "Bad request - Invalid parameters")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(PaginatedResult<BezirkListDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<PaginatedResult<BezirkListDto>>> GetBezirke(
        [FromQuery] BezirkQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate parameters
            if (!parameters.IsValidSortField())
            {
                return BadRequest(CreateProblemDetails(
                    "Invalid Sort Field", 
                    $"Sort field '{parameters.SortBy}' is not valid. Valid fields: {string.Join(", ", BezirkQueryParameters.ValidSortFields)}", 
                    400));
            }

            if (!ValidatePaginationParameters(parameters.PageNumber, parameters.PageSize))
            {
                return BadRequest(ModelState);
            }

            var query = new GetAllBezirkeQuery
            {
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                SearchTerm = parameters.SearchTerm,
                IsActive = parameters.IsActive,
                HasApplications = parameters.HasApplications,
                HasCadastralAreas = parameters.HasCadastralAreas,
                SortBy = parameters.SortBy,
                SortDirection = parameters.SortDirection,
                IncludeCadastralAreas = parameters.IncludeCadastralAreas,
                IncludeApplicationCount = parameters.IncludeApplicationCount
            };

            var result = await Mediator.Send(query, cancellationToken);

            // Set caching headers for list data (5 minutes)
            SetCacheHeaders(300, isPublic: false);

            return HandlePaginatedResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Getting districts", parameters);
        }
    }

    /// <summary>
    /// Gets a specific district by ID
    /// </summary>
    /// <param name="id">District unique identifier</param>
    /// <param name="includeCadastralAreas">Whether to include related cadastral areas</param>
    /// <param name="includeApplications">Whether to include application count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>District details</returns>
    /// <response code="200">Success - Returns district details</response>
    /// <response code="400">Bad Request - Invalid ID format</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Not Found - District not found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get district by ID",
        Description = "Retrieves detailed information about a specific district by its unique identifier. " +
                     "Optionally includes related cadastral areas and application statistics."
    )]
    [SwaggerResponse(200, "Success", typeof(BezirkDto))]
    [SwaggerResponse(400, "Bad request - Invalid ID")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "District not found")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(BezirkDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<BezirkDto>> GetBezirk(
        Guid id,
        [FromQuery] bool includeCadastralAreas = true,
        [FromQuery] bool includeApplications = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(CreateProblemDetails("Invalid ID", "District ID cannot be empty", 400));
            }

            var query = new GetBezirkByIdQuery(id, includeCadastralAreas, includeApplications);
            var result = await Mediator.Send(query, cancellationToken);

            // Set caching headers for individual resource (10 minutes)
            SetCacheHeaders(600, isPublic: false);

            return HandleResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Getting district by ID", new { id, includeCadastralAreas, includeApplications });
        }
    }

    /// <summary>
    /// Search districts by name or display name
    /// </summary>
    /// <param name="parameters">Search parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching districts</returns>
    /// <response code="200">Success - Returns matching districts</response>
    /// <response code="400">Bad Request - Invalid search parameters</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("search")]
    [SwaggerOperation(
        Summary = "Search districts",
        Description = "Searches districts by name, display name, or description. " +
                     "Supports fuzzy matching and can be limited to active districts only."
    )]
    [SwaggerResponse(200, "Success", typeof(List<BezirkListDto>))]
    [SwaggerResponse(400, "Bad request - Invalid search parameters")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(List<BezirkListDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<List<BezirkListDto>>> SearchBezirke(
        [FromQuery] BezirkSearchParameters parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var query = new SearchBezirkeQuery
            {
                Query = parameters.Query,
                Limit = parameters.Limit,
                ActiveOnly = parameters.ActiveOnly,
                FuzzyMatch = parameters.FuzzyMatch
            };

            var result = await Mediator.Send(query, cancellationToken);

            // Set shorter cache for search results (2 minutes)
            SetCacheHeaders(120, isPublic: false);

            return HandleResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Searching districts", parameters);
        }
    }

    /// <summary>
    /// Gets statistical information about districts
    /// </summary>
    /// <param name="includeCadastralAreas">Include cadastral area statistics</param>
    /// <param name="includeApplications">Include application statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>District statistics</returns>
    /// <response code="200">Success - Returns district statistics</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("statistics")]
    [SwaggerOperation(
        Summary = "Get district statistics",
        Description = "Retrieves comprehensive statistical information about districts, " +
                     "including counts, distributions, and related data statistics."
    )]
    [SwaggerResponse(200, "Success", typeof(BezirkStatistics))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(BezirkStatistics), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<BezirkStatistics>> GetBezirkeStatistics(
        [FromQuery] bool includeCadastralAreas = true,
        [FromQuery] bool includeApplications = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetBezirkeStatisticsQuery
            {
                IncludeCadastralAreas = includeCadastralAreas,
                IncludeApplications = includeApplications
            };

            var result = await Mediator.Send(query, cancellationToken);

            // Cache statistics for longer (15 minutes)
            SetCacheHeaders(900, isPublic: false);

            return HandleResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Getting district statistics", new { includeCadastralAreas, includeApplications });
        }
    }

    /// <summary>
    /// Creates a new district
    /// </summary>
    /// <param name="command">District creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created district</returns>
    /// <response code="201">Created - Returns the newly created district</response>
    /// <response code="400">Bad Request - Invalid data</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="403">Forbidden - Insufficient permissions</response>
    /// <response code="422">Unprocessable Entity - Validation errors</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create district",
        Description = "Creates a new district with the provided information. " +
                     "Requires administrative privileges."
    )]
    [SwaggerResponse(201, "Created", typeof(BezirkDto))]
    [SwaggerResponse(400, "Bad request - Invalid data")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions")]
    [SwaggerResponse(422, "Unprocessable entity - Validation errors")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(BezirkDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    [Authorize(Policy = "RequireSachbearbeiterRole")]
    public async Task<ActionResult<BezirkDto>> CreateBezirk(
        [FromBody] CreateBezirkCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Set user context
            command.CreatedBy = CurrentUserName ?? CurrentUserId ?? "System";

            var result = await Mediator.Send(command, cancellationToken);

            // No caching for create operations
            SetNoCacheHeaders();

            if (result.IsSuccess)
            {
                Logger.LogInformation("District created successfully by user {User}. ID: {Id}", 
                    CurrentUserName ?? CurrentUserId, result.Value?.Id);
                
                return CreatedAtAction(
                    nameof(GetBezirk),
                    new { id = result.Value!.Id },
                    result.Value);
            }

            return HandleResult(result, 201);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Creating district", command);
        }
    }

    /// <summary>
    /// Updates an existing district
    /// </summary>
    /// <param name="id">District ID</param>
    /// <param name="command">Updated district data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated district</returns>
    /// <response code="200">Success - Returns the updated district</response>
    /// <response code="400">Bad Request - Invalid data or ID mismatch</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="403">Forbidden - Insufficient permissions</response>
    /// <response code="404">Not Found - District not found</response>
    /// <response code="422">Unprocessable Entity - Validation errors</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Update district",
        Description = "Updates an existing district with the provided information. " +
                     "The ID in the URL must match the ID in the request body. " +
                     "Requires administrative privileges."
    )]
    [SwaggerResponse(200, "Success", typeof(BezirkDto))]
    [SwaggerResponse(400, "Bad request - Invalid data or ID mismatch")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions")]
    [SwaggerResponse(404, "District not found")]
    [SwaggerResponse(422, "Unprocessable entity - Validation errors")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(BezirkDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    [Authorize(Policy = "RequireSachbearbeiterRole")]
    public async Task<ActionResult<BezirkDto>> UpdateBezirk(
        Guid id,
        [FromBody] UpdateBezirkCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(CreateProblemDetails("Invalid ID", "District ID cannot be empty", 400));
            }

            if (id != command.Id)
            {
                return BadRequest(CreateProblemDetails(
                    "ID Mismatch", 
                    "URL ID does not match command ID", 
                    400));
            }

            // Set user context
            command.UpdatedBy = CurrentUserName ?? CurrentUserId ?? "System";

            var result = await Mediator.Send(command, cancellationToken);

            // No caching for update operations
            SetNoCacheHeaders();

            if (result.IsSuccess)
            {
                Logger.LogInformation("District updated successfully by user {User}. ID: {Id}", 
                    CurrentUserName ?? CurrentUserId, id);
            }

            return HandleResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Updating district", new { id, command });
        }
    }

    /// <summary>
    /// Deletes a district
    /// </summary>
    /// <param name="id">District ID</param>
    /// <param name="force">Force deletion even if district has related data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">No Content - District deleted successfully</response>
    /// <response code="400">Bad Request - Invalid ID or deletion not allowed</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="403">Forbidden - Insufficient permissions</response>
    /// <response code="404">Not Found - District not found</response>
    /// <response code="409">Conflict - District has related data and force is false</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Delete district",
        Description = "Deletes a district. By default, deletion is prevented if the district has " +
                     "related data (applications, cadastral areas). Use force=true to override. " +
                     "Requires administrative privileges and special rate limiting."
    )]
    [SwaggerResponse(204, "No content - District deleted successfully")]
    [SwaggerResponse(400, "Bad request - Invalid ID")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions")]
    [SwaggerResponse(404, "District not found")]
    [SwaggerResponse(409, "Conflict - District has related data")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    [Authorize(Policy = "RequireAdminRole")]
    [EnableRateLimiting("StrictPolicy")]
    public async Task<IActionResult> DeleteBezirk(
        Guid id,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(CreateProblemDetails("Invalid ID", "District ID cannot be empty", 400));
            }

            var command = new DeleteBezirkCommand(id, force)
            {
                DeletedBy = CurrentUserName ?? CurrentUserId ?? "System"
            };

            var result = await Mediator.Send(command, cancellationToken);

            // No caching for delete operations
            SetNoCacheHeaders();

            if (result.IsSuccess)
            {
                Logger.LogWarning("District deleted by user {User}. ID: {Id}, Force: {Force}", 
                    CurrentUserName ?? CurrentUserId, id, force);
            }

            return HandleResult(result, 204);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Deleting district", new { id, force });
        }
    }
}