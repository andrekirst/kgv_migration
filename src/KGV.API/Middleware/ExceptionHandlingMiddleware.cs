using FluentValidation;
using System.Net;
using System.Text.Json;

namespace KGV.API.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Validation failed",
                validationEx.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message, null),
            ArgumentNullException => (HttpStatusCode.BadRequest, "Required parameter is missing", null),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access", null),
            NotImplementedException => (HttpStatusCode.NotImplemented, "Feature not implemented", null),
            TimeoutException => (HttpStatusCode.RequestTimeout, "Request timeout", null),
            _ => (HttpStatusCode.InternalServerError, "An error occurred while processing your request", null)
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = new
            {
                message,
                statusCode = (int)statusCode,
                timestamp = DateTime.UtcNow,
                path = context.Request.Path,
                method = context.Request.Method,
                errors
            }
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Security headers middleware
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'");

        // Remove server header
        context.Response.Headers.Remove("Server");

        await _next(context);
    }
}

/// <summary>
/// Extension methods for middleware registration
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds security headers middleware
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}