using KGV.API.Models;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Application.Features.Parzellen.Commands.AssignParzelle;
using KGV.Application.Features.Parzellen.Commands.CreateParzelle;
using KGV.Application.Features.Parzellen.Commands.DeleteParzelle;
using KGV.Application.Features.Parzellen.Commands.UpdateParzelle;
using KGV.Application.Features.Parzellen.DTOs;
using KGV.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace KGV.API.Controllers;

/// <summary>
/// Controller for managing Parzellen (Garden Plots)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("DefaultPolicy")]
[SwaggerTag("Parzellen (Garden Plots) management operations")]
public class ParzellenController : BaseApiController
{
    public ParzellenController(IMediator mediator, ILogger<ParzellenController> logger) 
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Gets a paginated list of plots with comprehensive filtering options
    /// </summary>
    /// <param name="parameters">Query parameters for filtering and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of plots</returns>
    /// <response code="200">Success - Returns paginated list of plots</response>
    /// <response code="400">Bad Request - Invalid query parameters</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get plots",
        Description = "Retrieves a paginated list of garden plots with comprehensive filtering options. " +
                     "Supports filtering by district, status, availability, utilities, area, price, and assignment date ranges."
    )]
    [SwaggerResponse(200, "Success", typeof(PaginatedResult<ParzelleListDto>))]
    [SwaggerResponse(400, "Bad request - Invalid parameters")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(PaginatedResult<ParzelleListDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<PaginatedResult<ParzelleListDto>>> GetParzellen(
        [FromQuery] ParzelleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate parameters
            if (!parameters.IsValidSortField())
            {
                return BadRequest(CreateProblemDetails(
                    "Invalid Sort Field", 
                    $"Sort field '{parameters.SortBy}' is not valid. Valid fields: {string.Join(", ", ParzelleQueryParameters.ValidSortFields)}", 
                    400));
            }

            if (!ValidatePaginationParameters(parameters.PageNumber, parameters.PageSize))
            {
                return BadRequest(ModelState);
            }

            // Validate ranges
            var rangeErrors = parameters.ValidateRanges();
            if (rangeErrors.Any())
            {
                foreach (var error in rangeErrors)
                {
                    ModelState.AddModelError("", error);
                }
                return BadRequest(ModelState);
            }

            // Create query - this would need to be implemented in Application layer
            var query = new GetParzellenQuery
            {
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                SearchTerm = parameters.SearchTerm,
                BezirkId = parameters.BezirkId,
                Status = parameters.Status,
                IsAvailable = parameters.IsAvailable,
                HasWasser = parameters.HasWasser,
                HasStrom = parameters.HasStrom,
                MinFlaeche = parameters.MinFlaeche,
                MaxFlaeche = parameters.MaxFlaeche,
                MinPreis = parameters.MinPreis,
                MaxPreis = parameters.MaxPreis,
                MinPrioritaet = parameters.MinPrioritaet,
                VergebenFrom = parameters.VergebenFrom,
                VergebenTo = parameters.VergebenTo,
                SortBy = parameters.SortBy,
                SortDirection = parameters.SortDirection,
                IncludeBezirk = parameters.IncludeBezirk
            };

            var result = await Mediator.Send(query, cancellationToken);

            // Set caching headers for list data (3 minutes - shorter than districts due to more dynamic nature)
            SetCacheHeaders(180, isPublic: false);

            return HandlePaginatedResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Getting plots", parameters);
        }
    }

    /// <summary>
    /// Gets a specific plot by ID
    /// </summary>
    /// <param name="id">Plot unique identifier</param>
    /// <param name="includeBezirk">Whether to include district information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plot details</returns>
    /// <response code="200">Success - Returns plot details</response>
    /// <response code="400">Bad Request - Invalid ID format</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Not Found - Plot not found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get plot by ID",
        Description = "Retrieves detailed information about a specific garden plot by its unique identifier. " +
                     "Optionally includes related district information."
    )]
    [SwaggerResponse(200, "Success", typeof(ParzelleDto))]
    [SwaggerResponse(400, "Bad request - Invalid ID")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Plot not found")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(ParzelleDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<ParzelleDto>> GetParzelle(
        Guid id,
        [FromQuery] bool includeBezirk = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(CreateProblemDetails("Invalid ID", "Plot ID cannot be empty", 400));
            }

            var query = new GetParzelleByIdQuery(id, includeBezirk);
            var result = await Mediator.Send(query, cancellationToken);

            // Set caching headers for individual resource (5 minutes)
            SetCacheHeaders(300, isPublic: false);

            return HandleResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Getting plot by ID", new { id, includeBezirk });
        }
    }

    /// <summary>
    /// Gets plots by district ID
    /// </summary>
    /// <param name="bezirkId">District unique identifier</param>
    /// <param name="parameters">Additional query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of plots in the district</returns>
    /// <response code="200">Success - Returns plots in the district</response>
    /// <response code="400">Bad Request - Invalid district ID</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Not Found - District not found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("bezirke/{bezirkId:guid}/parzellen")]
    [SwaggerOperation(
        Summary = "Get plots by district",
        Description = "Retrieves all plots within a specific district with optional filtering and pagination."
    )]
    [SwaggerResponse(200, "Success", typeof(PaginatedResult<ParzelleListDto>))]
    [SwaggerResponse(400, "Bad request - Invalid district ID")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "District not found")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(PaginatedResult<ParzelleListDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<PaginatedResult<ParzelleListDto>>> GetParzellenByBezirk(
        Guid bezirkId,
        [FromQuery] ParzelleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (bezirkId == Guid.Empty)
            {
                return BadRequest(CreateProblemDetails("Invalid District ID", "District ID cannot be empty", 400));
            }

            // Override the BezirkId parameter
            parameters.BezirkId = bezirkId;

            // Reuse the main Get method logic
            return await GetParzellen(parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Getting plots by district", new { bezirkId, parameters });
        }
    }

    /// <summary>
    /// Gets available plots for assignment
    /// </summary>
    /// <param name="bezirkId">Filter by district (optional)</param>
    /// <param name="minArea">Minimum area in square meters (optional)</param>
    /// <param name="maxArea">Maximum area in square meters (optional)</param>
    /// <param name="hasWater">Filter by water access (optional)</param>
    /// <param name="hasElectricity">Filter by electricity access (optional)</param>
    /// <param name="maxPrice">Maximum price (optional)</param>
    /// <param name="limit">Maximum number of results (default: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available plots</returns>
    /// <response code="200">Success - Returns available plots</response>
    /// <response code="400">Bad Request - Invalid parameters</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("freie")]
    [SwaggerOperation(
        Summary = "Get available plots",
        Description = "Retrieves plots that are currently available for assignment. " +
                     "Supports filtering by district, area, utilities, and price."
    )]
    [SwaggerResponse(200, "Success", typeof(List<ParzelleListDto>))]
    [SwaggerResponse(400, "Bad request - Invalid parameters")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(List<ParzelleListDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<List<ParzelleListDto>>> GetFreieParzellen(
        [FromQuery] Guid? bezirkId = null,
        [FromQuery] decimal? minArea = null,
        [FromQuery] decimal? maxArea = null,
        [FromQuery] bool? hasWater = null,
        [FromQuery] bool? hasElectricity = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery, Range(1, 100)] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAvailableParzellenQuery
            {
                BezirkId = bezirkId,
                MinArea = minArea,
                MaxArea = maxArea,
                HasWater = hasWater,
                HasElectricity = hasElectricity,
                MaxPrice = maxPrice,
                Limit = limit
            };

            var result = await Mediator.Send(query, cancellationToken);

            // Set shorter cache for availability data (1 minute)
            SetCacheHeaders(60, isPublic: false);

            return HandleResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Getting available plots", new { bezirkId, minArea, maxArea, hasWater, hasElectricity, maxPrice, limit });
        }
    }

    /// <summary>
    /// Search plots by number, description, or features
    /// </summary>
    /// <param name="parameters">Search parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching plots</returns>
    /// <response code="200">Success - Returns matching plots</response>
    /// <response code="400">Bad Request - Invalid search parameters</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("search")]
    [SwaggerOperation(
        Summary = "Search plots",
        Description = "Searches plots by plot number, description, or special features. " +
                     "Supports fuzzy matching and can be limited to available plots or specific districts."
    )]
    [SwaggerResponse(200, "Success", typeof(List<ParzelleListDto>))]
    [SwaggerResponse(400, "Bad request - Invalid search parameters")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(List<ParzelleListDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<List<ParzelleListDto>>> SearchParzellen(
        [FromQuery] ParzelleSearchParameters parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var query = new SearchParzellenQuery
            {
                Query = parameters.Query,
                Limit = parameters.Limit,
                BezirkId = parameters.BezirkId,
                AvailableOnly = parameters.AvailableOnly,
                FuzzyMatch = parameters.FuzzyMatch
            };

            var result = await Mediator.Send(query, cancellationToken);

            // Set shorter cache for search results (1 minute)
            SetCacheHeaders(60, isPublic: false);

            return HandleResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Searching plots", parameters);
        }
    }

    /// <summary>
    /// Gets statistical information about plots
    /// </summary>
    /// <param name="parameters">Statistics filter parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plot statistics</returns>
    /// <response code="200">Success - Returns plot statistics</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("statistics")]
    [SwaggerOperation(
        Summary = "Get plot statistics",
        Description = "Retrieves comprehensive statistical information about plots, " +
                     "including status distributions, area statistics, and availability metrics."
    )]
    [SwaggerResponse(200, "Success", typeof(ParzellenStatistics))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(ParzellenStatistics), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<ParzellenStatistics>> GetParzellenStatistics(
        [FromQuery] ParzelleStatisticsParameters parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetParzellenStatisticsQuery
            {
                BezirkId = parameters.BezirkId,
                IncludeHistory = parameters.IncludeHistory,
                FromDate = parameters.FromDate,
                ToDate = parameters.ToDate,
                GroupByBezirk = parameters.GroupByBezirk,
                GroupByStatus = parameters.GroupByStatus
            };

            var result = await Mediator.Send(query, cancellationToken);

            // Cache statistics for longer (10 minutes)
            SetCacheHeaders(600, isPublic: false);

            return HandleResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Getting plot statistics", parameters);
        }
    }

    /// <summary>
    /// Creates a new plot
    /// </summary>
    /// <param name="command">Plot creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created plot</returns>
    /// <response code="201">Created - Returns the newly created plot</response>
    /// <response code="400">Bad Request - Invalid data</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="403">Forbidden - Insufficient permissions</response>
    /// <response code="422">Unprocessable Entity - Validation errors</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create plot",
        Description = "Creates a new garden plot with the provided information. " +
                     "Requires administrative privileges."
    )]
    [SwaggerResponse(201, "Created", typeof(ParzelleDto))]
    [SwaggerResponse(400, "Bad request - Invalid data")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions")]
    [SwaggerResponse(422, "Unprocessable entity - Validation errors")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(ParzelleDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    [Authorize(Policy = "RequireSachbearbeiterRole")]
    public async Task<ActionResult<ParzelleDto>> CreateParzelle(
        [FromBody] CreateParzelleCommand command,
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
                Logger.LogInformation("Plot created successfully by user {User}. ID: {Id}", 
                    CurrentUserName ?? CurrentUserId, result.Value?.Id);
                
                return CreatedAtAction(
                    nameof(GetParzelle),
                    new { id = result.Value!.Id },
                    result.Value);
            }

            return HandleResult(result, 201);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Creating plot", command);
        }
    }

    /// <summary>
    /// Updates an existing plot
    /// </summary>
    /// <param name="id">Plot ID</param>
    /// <param name="command">Updated plot data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated plot</returns>
    /// <response code="200">Success - Returns the updated plot</response>
    /// <response code="400">Bad Request - Invalid data or ID mismatch</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="403">Forbidden - Insufficient permissions</response>
    /// <response code="404">Not Found - Plot not found</response>
    /// <response code="422">Unprocessable Entity - Validation errors</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Update plot",
        Description = "Updates an existing garden plot with the provided information. " +
                     "The ID in the URL must match the ID in the request body. " +
                     "Requires administrative privileges."
    )]
    [SwaggerResponse(200, "Success", typeof(ParzelleDto))]
    [SwaggerResponse(400, "Bad request - Invalid data or ID mismatch")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions")]
    [SwaggerResponse(404, "Plot not found")]
    [SwaggerResponse(422, "Unprocessable entity - Validation errors")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(ParzelleDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    [Authorize(Policy = "RequireSachbearbeiterRole")]
    public async Task<ActionResult<ParzelleDto>> UpdateParzelle(
        Guid id,
        [FromBody] UpdateParzelleCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(CreateProblemDetails("Invalid ID", "Plot ID cannot be empty", 400));
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
                Logger.LogInformation("Plot updated successfully by user {User}. ID: {Id}", 
                    CurrentUserName ?? CurrentUserId, id);
            }

            return HandleResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Updating plot", new { id, command });
        }
    }

    /// <summary>
    /// Assigns a plot to an applicant (special business operation)
    /// </summary>
    /// <param name="id">Plot ID</param>
    /// <param name="parameters">Assignment parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated plot with assignment information</returns>
    /// <response code="200">Success - Plot assigned successfully</response>
    /// <response code="400">Bad Request - Invalid data or plot not available</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="403">Forbidden - Insufficient permissions</response>
    /// <response code="404">Not Found - Plot not found</response>
    /// <response code="409">Conflict - Plot is not available for assignment</response>
    /// <response code="422">Unprocessable Entity - Validation errors</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut("{id:guid}/assign")]
    [SwaggerOperation(
        Summary = "Assign plot",
        Description = "Assigns a garden plot to an applicant. This is a special business operation " +
                     "that changes the plot status and creates assignment records. The plot must be available for assignment."
    )]
    [SwaggerResponse(200, "Success - Plot assigned", typeof(ParzelleDto))]
    [SwaggerResponse(400, "Bad request - Invalid data")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions")]
    [SwaggerResponse(404, "Plot not found")]
    [SwaggerResponse(409, "Conflict - Plot not available")]
    [SwaggerResponse(422, "Unprocessable entity - Validation errors")]
    [SwaggerResponse(500, "Internal server error")]
    [ProducesResponseType(typeof(ParzelleDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    [Authorize(Policy = "RequireSachbearbeiterRole")]
    public async Task<ActionResult<ParzelleDto>> AssignParzelle(
        Guid id,
        [FromBody] AssignParzelleParameters parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(CreateProblemDetails("Invalid ID", "Plot ID cannot be empty", 400));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new AssignParzelleCommand
            {
                ParzelleId = id,
                AssigneeId = parameters.AssigneeId,
                AssignmentDate = parameters.AssignmentDate,
                Notes = parameters.Notes,
                AssignmentPrice = parameters.AssignmentPrice,
                AssignedBy = CurrentUserName ?? CurrentUserId ?? "System"
            };

            var result = await Mediator.Send(command, cancellationToken);

            // No caching for assignment operations
            SetNoCacheHeaders();

            if (result.IsSuccess)
            {
                Logger.LogInformation("Plot assigned successfully by user {User}. PlotId: {PlotId}, AssigneeId: {AssigneeId}", 
                    CurrentUserName ?? CurrentUserId, id, parameters.AssigneeId);
            }

            return HandleResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Assigning plot", new { id, parameters });
        }
    }

    /// <summary>
    /// Deletes a plot
    /// </summary>
    /// <param name="id">Plot ID</param>
    /// <param name="force">Force deletion even if plot has related data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">No Content - Plot deleted successfully</response>
    /// <response code="400">Bad Request - Invalid ID or deletion not allowed</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="403">Forbidden - Insufficient permissions</response>
    /// <response code="404">Not Found - Plot not found</response>
    /// <response code="409">Conflict - Plot has related data and force is false</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Delete plot",
        Description = "Deletes a garden plot. By default, deletion is prevented if the plot has " +
                     "related data (assignments, applications). Use force=true to override. " +
                     "Requires administrative privileges and special rate limiting."
    )]
    [SwaggerResponse(204, "No content - Plot deleted successfully")]
    [SwaggerResponse(400, "Bad request - Invalid ID")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions")]
    [SwaggerResponse(404, "Plot not found")]
    [SwaggerResponse(409, "Conflict - Plot has related data")]
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
    public async Task<IActionResult> DeleteParzelle(
        Guid id,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(CreateProblemDetails("Invalid ID", "Plot ID cannot be empty", 400));
            }

            var command = new DeleteParzelleCommand(id, force)
            {
                DeletedBy = CurrentUserName ?? CurrentUserId ?? "System"
            };

            var result = await Mediator.Send(command, cancellationToken);

            // No caching for delete operations
            SetNoCacheHeaders();

            if (result.IsSuccess)
            {
                Logger.LogWarning("Plot deleted by user {User}. ID: {Id}, Force: {Force}", 
                    CurrentUserName ?? CurrentUserId, id, force);
            }

            return HandleResult(result, 204);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Deleting plot", new { id, force });
        }
    }
}

// Placeholder classes for queries that would need to be implemented in the Application layer
public class GetParzellenQuery : IRequest<Result<PaginatedResult<ParzelleListDto>>>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string? SearchTerm { get; set; }
    public Guid? BezirkId { get; set; }
    public ParzellenStatus? Status { get; set; }
    public bool? IsAvailable { get; set; }
    public bool? HasWasser { get; set; }
    public bool? HasStrom { get; set; }
    public decimal? MinFlaeche { get; set; }
    public decimal? MaxFlaeche { get; set; }
    public decimal? MinPreis { get; set; }
    public decimal? MaxPreis { get; set; }
    public int? MinPrioritaet { get; set; }
    public DateTime? VergebenFrom { get; set; }
    public DateTime? VergebenTo { get; set; }
    public string SortBy { get; set; } = "Nummer";
    public string SortDirection { get; set; } = "asc";
    public bool IncludeBezirk { get; set; } = true;
}

public class GetParzelleByIdQuery : IRequest<Result<ParzelleDto>>
{
    public Guid Id { get; }
    public bool IncludeBezirk { get; }

    public GetParzelleByIdQuery(Guid id, bool includeBezirk = true)
    {
        Id = id;
        IncludeBezirk = includeBezirk;
    }
}

public class GetAvailableParzellenQuery : IRequest<Result<List<ParzelleListDto>>>
{
    public Guid? BezirkId { get; set; }
    public decimal? MinArea { get; set; }
    public decimal? MaxArea { get; set; }
    public bool? HasWater { get; set; }
    public bool? HasElectricity { get; set; }
    public decimal? MaxPrice { get; set; }
    public int Limit { get; set; } = 50;
}

public class SearchParzellenQuery : IRequest<Result<List<ParzelleListDto>>>
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
    public Guid? BezirkId { get; set; }
    public bool AvailableOnly { get; set; }
    public bool FuzzyMatch { get; set; }
}

public class GetParzellenStatisticsQuery : IRequest<Result<ParzellenStatistics>>
{
    public Guid? BezirkId { get; set; }
    public bool IncludeHistory { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool GroupByBezirk { get; set; }
    public bool GroupByStatus { get; set; }
}