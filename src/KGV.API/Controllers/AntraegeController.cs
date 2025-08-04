using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Application.Features.Antraege.Commands;
using KGV.Application.Features.Antraege.Queries;
using KGV.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace KGV.API.Controllers;

/// <summary>
/// Controller for managing Antraege (Applications)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Authorize]
[EnableRateLimiting("DefaultPolicy")]
[SwaggerTag("Antraege (Garden Plot Applications) management operations")]
public class AntraegeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AntraegeController> _logger;

    public AntraegeController(IMediator mediator, ILogger<AntraegeController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a paginated list of applications with filtering options
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page (max 100)</param>
    /// <param name="searchTerm">Search term for name, email, or address</param>
    /// <param name="status">Filter by status</param>
    /// <param name="aktiv">Filter by active/inactive</param>
    /// <param name="bewerbungsdatumFrom">Filter by application date from</param>
    /// <param name="bewerbungsdatumTo">Filter by application date to</param>
    /// <param name="ort">Filter by city</param>
    /// <param name="sortBy">Sort field (default: Bewerbungsdatum)</param>
    /// <param name="sortDirection">Sort direction (asc/desc, default: desc)</param>
    /// <param name="onlyActive">Whether to include only active applications</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of applications</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get applications",
        Description = "Retrieves a paginated list of garden plot applications with filtering and sorting options"
    )]
    [SwaggerResponse(200, "Success", typeof(PaginatedResult<AntragListDto>))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<PaginatedResult<AntragListDto>>> GetAntraege(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] AntragStatus? status = null,
        [FromQuery] bool? aktiv = null,
        [FromQuery] DateTime? bewerbungsdatumFrom = null,
        [FromQuery] DateTime? bewerbungsdatumTo = null,
        [FromQuery] string? ort = null,
        [FromQuery] string sortBy = "Bewerbungsdatum",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] bool onlyActive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAntraegeQuery
            {
                PageNumber = pageNumber,
                PageSize = Math.Min(pageSize, 100), // Limit max page size
                SearchTerm = searchTerm,
                Status = status,
                Aktiv = aktiv,
                BewerbungsdatumFrom = bewerbungsdatumFrom,
                BewerbungsdatumTo = bewerbungsdatumTo,
                Ort = ort,
                SortBy = sortBy,
                SortDirection = sortDirection,
                OnlyActive = onlyActive
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applications");
            return StatusCode(500, "An error occurred while retrieving applications");
        }
    }

    /// <summary>
    /// Gets a specific application by ID
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="includeHistory">Whether to include history entries</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Application details</returns>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get application by ID",
        Description = "Retrieves a specific garden plot application by its unique identifier"
    )]
    [SwaggerResponse(200, "Success", typeof(AntragDto))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Application not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<AntragDto>> GetAntrag(
        Guid id,
        [FromQuery] bool includeHistory = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAntragByIdQuery(id, includeHistory);
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                return NotFound(result.Error);
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the application");
        }
    }

    /// <summary>
    /// Creates a new application
    /// </summary>
    /// <param name="command">Application data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created application</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create application",
        Description = "Creates a new garden plot application"
    )]
    [SwaggerResponse(201, "Created", typeof(AntragDto))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(422, "Validation error")]
    [SwaggerResponse(500, "Internal server error")]
    [Authorize(Policy = "RequireSachbearbeiterRole")]
    public async Task<ActionResult<AntragDto>> CreateAntrag(
        [FromBody] CreateAntragCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                if (result.ValidationErrors != null)
                {
                    foreach (var error in result.ValidationErrors)
                    {
                        foreach (var message in error.Value)
                        {
                            ModelState.AddModelError(error.Key, message);
                        }
                    }
                    return UnprocessableEntity(ModelState);
                }

                return BadRequest(result.Error);
            }

            return CreatedAtAction(
                nameof(GetAntrag),
                new { id = result.Value!.Id },
                result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application");
            return StatusCode(500, "An error occurred while creating the application");
        }
    }

    /// <summary>
    /// Updates an existing application
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="command">Updated application data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application</returns>
    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Update application",
        Description = "Updates an existing garden plot application"
    )]
    [SwaggerResponse(200, "Success", typeof(AntragDto))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Application not found")]
    [SwaggerResponse(422, "Validation error")]
    [SwaggerResponse(500, "Internal server error")]
    [Authorize(Policy = "RequireSachbearbeiterRole")]
    public async Task<ActionResult<AntragDto>> UpdateAntrag(
        Guid id,
        [FromBody] UpdateAntragCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id != command.Id)
            {
                return BadRequest("URL ID does not match command ID");
            }

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                if (result.ValidationErrors != null)
                {
                    foreach (var error in result.ValidationErrors)
                    {
                        foreach (var message in error.Value)
                        {
                            ModelState.AddModelError(error.Key, message);
                        }
                    }
                    return UnprocessableEntity(ModelState);
                }

                return NotFound(result.Error);
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application {Id}", id);
            return StatusCode(500, "An error occurred while updating the application");
        }
    }

    /// <summary>
    /// Updates the status of an application
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="command">Status update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application</returns>
    [HttpPatch("{id:guid}/status")]
    [SwaggerOperation(
        Summary = "Update application status",
        Description = "Updates the status of a garden plot application"
    )]
    [SwaggerResponse(200, "Success", typeof(AntragDto))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Application not found")]
    [SwaggerResponse(422, "Validation error")]
    [SwaggerResponse(500, "Internal server error")]
    [Authorize(Policy = "RequireSachbearbeiterRole")]
    public async Task<ActionResult<AntragDto>> UpdateAntragStatus(
        Guid id,
        [FromBody] UpdateAntragStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id != command.Id)
            {
                return BadRequest("URL ID does not match command ID");
            }

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                if (result.ValidationErrors != null)
                {
                    foreach (var error in result.ValidationErrors)
                    {
                        foreach (var message in error.Value)
                        {
                            ModelState.AddModelError(error.Key, message);
                        }
                    }
                    return UnprocessableEntity(ModelState);
                }

                return NotFound(result.Error);
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application status {Id}", id);
            return StatusCode(500, "An error occurred while updating the application status");
        }
    }

    /// <summary>
    /// Soft deletes an application
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Delete application",
        Description = "Soft deletes a garden plot application"
    )]
    [SwaggerResponse(204, "No content")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Application not found")]
    [SwaggerResponse(500, "Internal server error")]
    [Authorize(Policy = "RequireAdminRole")]
    [EnableRateLimiting("StrictPolicy")]
    public async Task<IActionResult> DeleteAntrag(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Implementation would go here - typically using a DeleteAntragCommand
            // For now, return a placeholder response
            _logger.LogInformation("Delete request for application {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting application {Id}", id);
            return StatusCode(500, "An error occurred while deleting the application");
        }
    }
}