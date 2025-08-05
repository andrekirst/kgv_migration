using KGV.Domain.Entities;
using KGV.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KGV.API.Controllers;

/// <summary>
/// Controller für Bezirke (Districts) - Simplified implementation without CQRS
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BezirkeController : ControllerBase
{
    private readonly KgvDbContext _context;
    private readonly ILogger<BezirkeController> _logger;

    public BezirkeController(KgvDbContext context, ILogger<BezirkeController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all districts
    /// </summary>
    /// <returns>List of all districts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BezirkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BezirkDto>>> GetAllBezirke()
    {
        try
        {
            _logger.LogInformation("Retrieving all districts");
            
            var bezirke = await _context.Bezirke
                .Where(b => b.IsActive)
                .OrderBy(b => b.SortOrder)
                .ThenBy(b => b.Name)
                .ToListAsync();

            var result = bezirke.Select(b => new BezirkDto
            {
                Id = b.Id,
                Name = b.Name,
                DisplayName = b.DisplayName,
                Description = b.Description,
                IsActive = b.IsActive,
                SortOrder = b.SortOrder,
                Flaeche = b.Flaeche,
                AnzahlParzellen = b.AnzahlParzellen,
                Status = b.Status.ToString(),
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving districts");
            return StatusCode(StatusCodes.Status500InternalServerError, "Fehler beim Abrufen der Bezirke");
        }
    }

    /// <summary>
    /// Retrieves a specific district by ID
    /// </summary>
    /// <param name="id">District ID</param>
    /// <returns>District details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BezirkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BezirkDto>> GetBezirk(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving district with ID: {DistrictId}", id);
            
            var bezirk = await _context.Bezirke
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bezirk == null)
            {
                _logger.LogWarning("District with ID {DistrictId} not found", id);
                return NotFound($"Bezirk mit ID {id} wurde nicht gefunden");
            }

            var result = new BezirkDto
            {
                Id = bezirk.Id,
                Name = bezirk.Name,
                DisplayName = bezirk.DisplayName,
                Description = bezirk.Description,
                IsActive = bezirk.IsActive,
                SortOrder = bezirk.SortOrder,
                Flaeche = bezirk.Flaeche,
                AnzahlParzellen = bezirk.AnzahlParzellen,
                Status = bezirk.Status.ToString(),
                CreatedAt = bezirk.CreatedAt,
                UpdatedAt = bezirk.UpdatedAt
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving district with ID: {DistrictId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Fehler beim Abrufen des Bezirks");
        }
    }

    /// <summary>
    /// Creates a new district
    /// </summary>
    /// <param name="createDto">District creation data</param>
    /// <returns>Created district</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BezirkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BezirkDto>> CreateBezirk([FromBody] CreateBezirkDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating new district: {DistrictName}", createDto.Name);

            // Check if district with same name already exists
            var existingBezirk = await _context.Bezirke
                .FirstOrDefaultAsync(b => b.Name.ToUpper() == createDto.Name.ToUpper());

            if (existingBezirk != null)
            {
                return BadRequest($"Ein Bezirk mit dem Namen '{createDto.Name}' existiert bereits");
            }

            var bezirk = Bezirk.Create(
                createDto.Name,
                createDto.DisplayName,
                createDto.Description,
                createDto.SortOrder,
                createDto.Flaeche);

            _context.Bezirke.Add(bezirk);
            await _context.SaveChangesAsync();

            var result = new BezirkDto
            {
                Id = bezirk.Id,
                Name = bezirk.Name,
                DisplayName = bezirk.DisplayName,
                Description = bezirk.Description,
                IsActive = bezirk.IsActive,
                SortOrder = bezirk.SortOrder,
                Flaeche = bezirk.Flaeche,
                AnzahlParzellen = bezirk.AnzahlParzellen,
                Status = bezirk.Status.ToString(),
                CreatedAt = bezirk.CreatedAt,
                UpdatedAt = bezirk.UpdatedAt
            };

            return CreatedAtAction(nameof(GetBezirk), new { id = bezirk.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating district: {DistrictName}", createDto.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, "Fehler beim Erstellen des Bezirks");
        }
    }

    /// <summary>
    /// Updates an existing district
    /// </summary>
    /// <param name="id">District ID</param>
    /// <param name="updateDto">District update data</param>
    /// <returns>Updated district</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BezirkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BezirkDto>> UpdateBezirk(Guid id, [FromBody] UpdateBezirkDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Updating district with ID: {DistrictId}", id);

            var bezirk = await _context.Bezirke
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bezirk == null)
            {
                return NotFound($"Bezirk mit ID {id} wurde nicht gefunden");
            }

            bezirk.Update(
                updateDto.DisplayName,
                updateDto.Description,
                updateDto.SortOrder,
                updateDto.Flaeche);

            await _context.SaveChangesAsync();

            var result = new BezirkDto
            {
                Id = bezirk.Id,
                Name = bezirk.Name,
                DisplayName = bezirk.DisplayName,
                Description = bezirk.Description,
                IsActive = bezirk.IsActive,
                SortOrder = bezirk.SortOrder,
                Flaeche = bezirk.Flaeche,
                AnzahlParzellen = bezirk.AnzahlParzellen,
                Status = bezirk.Status.ToString(),
                CreatedAt = bezirk.CreatedAt,
                UpdatedAt = bezirk.UpdatedAt
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating district with ID: {DistrictId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Fehler beim Aktualisieren des Bezirks");
        }
    }

    /// <summary>
    /// Deletes a district (soft delete)
    /// </summary>
    /// <param name="id">District ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteBezirk(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting district with ID: {DistrictId}", id);

            var bezirk = await _context.Bezirke
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bezirk == null)
            {
                return NotFound($"Bezirk mit ID {id} wurde nicht gefunden");
            }

            bezirk.Deactivate();
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting district with ID: {DistrictId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Fehler beim Löschen des Bezirks");
        }
    }
}

/// <summary>
/// DTO for returning district information
/// </summary>
public class BezirkDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public decimal? Flaeche { get; set; }
    public int AnzahlParzellen { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new district
/// </summary>
public class CreateBezirkDto
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public decimal? Flaeche { get; set; }
}

/// <summary>
/// DTO for updating a district
/// </summary>
public class UpdateBezirkDto
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public decimal? Flaeche { get; set; }
}