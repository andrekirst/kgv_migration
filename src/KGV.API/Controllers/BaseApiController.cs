using KGV.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KGV.API.Controllers;

/// <summary>
/// Base controller class with common functionality for all API controllers
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;
    protected readonly ILogger Logger;

    protected BaseApiController(IMediator mediator, ILogger logger)
    {
        Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current authenticated user ID
    /// </summary>
    protected string? CurrentUserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Gets the current authenticated user name
    /// </summary>
    protected string? CurrentUserName => User?.FindFirst(ClaimTypes.Name)?.Value;

    /// <summary>
    /// Gets the current authenticated user email
    /// </summary>
    protected string? CurrentUserEmail => User?.FindFirst(ClaimTypes.Email)?.Value;

    /// <summary>
    /// Handles MediatR result and returns appropriate HTTP response
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <param name="result">The MediatR result</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 200 OK)</param>
    /// <returns>ActionResult with appropriate HTTP status</returns>
    protected ActionResult<T> HandleResult<T>(Result<T> result, int successStatusCode = 200)
    {
        if (result.IsFailure)
        {
            Logger.LogWarning("Request failed with error: {Error}", result.Error);

            if (result.ValidationErrors != null && result.ValidationErrors.Any())
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

            // Check if error indicates not found
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ||
                result.Error?.Contains("nicht gefunden", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(CreateProblemDetails("Not Found", result.Error, 404));
            }

            return BadRequest(CreateProblemDetails("Bad Request", result.Error, 400));
        }

        return successStatusCode switch
        {
            200 => Ok(result.Value),
            201 => Created("", result.Value),
            204 => NoContent(),
            _ => StatusCode(successStatusCode, result.Value)
        };
    }

    /// <summary>
    /// Handles MediatR result for operations that don't return data
    /// </summary>
    /// <param name="result">The MediatR result</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 204 No Content)</param>
    /// <returns>ActionResult with appropriate HTTP status</returns>
    protected IActionResult HandleResult(Result result, int successStatusCode = 204)
    {
        if (result.IsFailure)
        {
            Logger.LogWarning("Request failed with error: {Error}", result.Error);

            if (result.ValidationErrors != null && result.ValidationErrors.Any())
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

            // Check if error indicates not found
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ||
                result.Error?.Contains("nicht gefunden", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(CreateProblemDetails("Not Found", result.Error, 404));
            }

            return BadRequest(CreateProblemDetails("Bad Request", result.Error, 400));
        }

        return successStatusCode switch
        {
            200 => Ok(),
            201 => Created("", null),
            204 => NoContent(),
            _ => StatusCode(successStatusCode)
        };
    }

    /// <summary>
    /// Handles paginated MediatR results with pagination headers
    /// </summary>
    /// <typeparam name="T">The result item type</typeparam>
    /// <param name="result">The paginated MediatR result</param>
    /// <returns>ActionResult with pagination headers</returns>
    protected ActionResult<PaginatedResult<T>> HandlePaginatedResult<T>(Result<PaginatedResult<T>> result)
    {
        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        var paginatedResult = result.Value!;
        
        // Add pagination headers
        Response.Headers.Append("X-Pagination-CurrentPage", paginatedResult.CurrentPage.ToString());
        Response.Headers.Append("X-Pagination-PageSize", paginatedResult.PageSize.ToString());
        Response.Headers.Append("X-Pagination-TotalCount", paginatedResult.TotalCount.ToString());
        Response.Headers.Append("X-Pagination-TotalPages", paginatedResult.TotalPages.ToString());
        Response.Headers.Append("X-Pagination-HasNextPage", paginatedResult.HasNextPage.ToString().ToLower());
        Response.Headers.Append("X-Pagination-HasPreviousPage", paginatedResult.HasPreviousPage.ToString().ToLower());

        return Ok(paginatedResult);
    }

    /// <summary>
    /// Creates a standardized ProblemDetails object
    /// </summary>
    /// <param name="title">The error title</param>
    /// <param name="detail">The error detail</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <returns>ProblemDetails object</returns>
    protected ProblemDetails CreateProblemDetails(string title, string? detail, int statusCode)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = HttpContext.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };
    }

    /// <summary>
    /// Logs and handles exceptions consistently
    /// </summary>
    /// <param name="ex">The exception</param>
    /// <param name="operation">The operation being performed</param>
    /// <param name="context">Optional context information</param>
    /// <returns>500 Internal Server Error response</returns>
    protected IActionResult HandleException(Exception ex, string operation, object? context = null)
    {
        Logger.LogError(ex, "Error during {Operation}. Context: {@Context}", operation, context);
        
        var problemDetails = CreateProblemDetails(
            "Internal Server Error",
            "An error occurred while processing your request.",
            500);

        return StatusCode(500, problemDetails);
    }

    /// <summary>
    /// Validates pagination parameters
    /// </summary>
    /// <param name="pageNumber">Page number (must be >= 1)</param>
    /// <param name="pageSize">Page size (must be between 1 and 100)</param>
    /// <returns>True if valid, false otherwise</returns>
    protected bool ValidatePaginationParameters(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            ModelState.AddModelError(nameof(pageNumber), "Page number must be greater than 0");
            return false;
        }

        if (pageSize < 1 || pageSize > 100)
        {
            ModelState.AddModelError(nameof(pageSize), "Page size must be between 1 and 100");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Sets cache control headers for responses
    /// </summary>
    /// <param name="maxAgeSeconds">Maximum age in seconds</param>
    /// <param name="isPublic">Whether the response can be cached publicly</param>
    protected void SetCacheHeaders(int maxAgeSeconds, bool isPublic = false)
    {
        var cacheControl = isPublic ? "public" : "private";
        Response.Headers.Append("Cache-Control", $"{cacheControl}, max-age={maxAgeSeconds}");
    }

    /// <summary>
    /// Sets no-cache headers for sensitive operations
    /// </summary>
    protected void SetNoCacheHeaders()
    {
        Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        Response.Headers.Append("Pragma", "no-cache");
        Response.Headers.Append("Expires", "0");
    }
}