using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace KGV.API.Middleware;

/// <summary>
/// Middleware to add consistent response headers for API metadata, performance, and compliance
/// </summary>
public class ResponseHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseHeadersMiddleware> _logger;
    private readonly ResponseHeadersOptions _options;

    public ResponseHeadersMiddleware(
        RequestDelegate next,
        ILogger<ResponseHeadersMiddleware> logger,
        IOptions<ResponseHeadersOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new ResponseHeadersOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Add request ID for tracing
        var requestId = context.TraceIdentifier;
        if (!context.Response.Headers.ContainsKey("X-Request-ID"))
        {
            context.Response.Headers["X-Request-ID"] = requestId;
        }

        // Add correlation ID if available
        if (context.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId?.ToString() ?? requestId;
        }

        // Add API version headers
        AddApiVersionHeaders(context);

        // Add security headers
        AddSecurityHeaders(context);

        // Add performance headers
        AddPerformanceHeaders(context, stopwatch);

        // Add CORS headers if needed
        AddCorsHeaders(context);

        // Continue with the pipeline
        await _next(context);

        stopwatch.Stop();

        // Add response time header
        context.Response.Headers["X-Response-Time"] = $"{stopwatch.ElapsedMilliseconds}ms";

        // Add server information
        if (_options.IncludeServerInfo)
        {
            context.Response.Headers["X-Powered-By"] = "KGV API v1.0";
            context.Response.Headers["Server"] = "KGV-Server";
        }

        // Add rate limiting headers (if rate limiting is active)
        AddRateLimitingHeaders(context);

        // Log response metrics
        LogResponseMetrics(context, stopwatch.ElapsedMilliseconds);
    }

    private static void AddApiVersionHeaders(HttpContext context)
    {
        context.Response.Headers["API-Version"] = "1.0";
        context.Response.Headers["API-Supported-Versions"] = "1.0";
        context.Response.Headers["API-Deprecated-Versions"] = "";
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        if (_options.IncludeSecurityHeaders)
        {
            // Content security policy
            context.Response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'";

            // Frame options
            context.Response.Headers["X-Frame-Options"] = "DENY";

            // Content type options
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // XSS protection
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

            // Referrer policy
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Permissions policy
            context.Response.Headers["Permissions-Policy"] = 
                "geolocation=(), microphone=(), camera=()";
        }
    }

    private static void AddPerformanceHeaders(HttpContext context, Stopwatch stopwatch)
    {
        // Add ETag for GET requests (if not already present)
        if (context.Request.Method == "GET" && !context.Response.Headers.ContainsKey("ETag"))
        {
            var etag = GenerateETag(context);
            if (!string.IsNullOrEmpty(etag))
            {
                context.Response.Headers["ETag"] = etag;
            }
        }

        // Add vary header for content negotiation
        if (!context.Response.Headers.ContainsKey("Vary"))
        {
            context.Response.Headers["Vary"] = "Accept, Accept-Language, Accept-Encoding";
        }

        // Add timing allow origin for CORS timing
        context.Response.Headers["Timing-Allow-Origin"] = "*";
    }

    private static void AddCorsHeaders(HttpContext context)
    {
        // These might be overridden by CORS middleware, but provide defaults
        if (!context.Response.Headers.ContainsKey("Access-Control-Expose-Headers"))
        {
            context.Response.Headers["Access-Control-Expose-Headers"] = 
                "X-Request-ID, X-Correlation-ID, X-Response-Time, X-Pagination-CurrentPage, " +
                "X-Pagination-PageSize, X-Pagination-TotalCount, X-Pagination-TotalPages, " +
                "X-Pagination-HasNextPage, X-Pagination-HasPreviousPage, X-RateLimit-Limit, " +
                "X-RateLimit-Remaining, X-RateLimit-Reset, API-Version";
        }
    }

    private static void AddRateLimitingHeaders(HttpContext context)
    {
        // These would typically be set by rate limiting middleware
        // We add them here as placeholders if not already present
        if (!context.Response.Headers.ContainsKey("X-RateLimit-Limit"))
        {
            // Default values - in real implementation, these would come from rate limiting middleware
            context.Response.Headers["X-RateLimit-Limit"] = "100";
            context.Response.Headers["X-RateLimit-Remaining"] = "99";
            context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString();
        }
    }

    private static string GenerateETag(HttpContext context)
    {
        // Simple ETag generation based on URL and query parameters
        // In production, this would be more sophisticated
        var url = context.Request.Path + context.Request.QueryString;
        var hash = url.GetHashCode();
        return $"\"{hash:X}\"";
    }

    private void LogResponseMetrics(HttpContext context, long responseTimeMs)
    {
        var statusCode = context.Response.StatusCode;
        var method = context.Request.Method;
        var path = context.Request.Path;
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";

        _logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {ResponseTime}ms. UserAgent: {UserAgent}",
            method, path, statusCode, responseTimeMs, userAgent);

        // Log performance warnings for slow requests
        if (responseTimeMs > _options.SlowRequestThresholdMs)
        {
            _logger.LogWarning(
                "Slow request detected: {Method} {Path} took {ResponseTime}ms (threshold: {Threshold}ms)",
                method, path, responseTimeMs, _options.SlowRequestThresholdMs);
        }

        // Log errors
        if (statusCode >= 400)
        {
            var logLevel = statusCode >= 500 ? LogLevel.Error : LogLevel.Warning;
            _logger.Log(logLevel,
                "HTTP error response: {Method} {Path} returned {StatusCode}",
                method, path, statusCode);
        }
    }
}

/// <summary>
/// Configuration options for response headers middleware
/// </summary>
public class ResponseHeadersOptions
{
    /// <summary>
    /// Whether to include server information headers
    /// </summary>
    public bool IncludeServerInfo { get; set; } = true;

    /// <summary>
    /// Whether to include security headers
    /// </summary>
    public bool IncludeSecurityHeaders { get; set; } = true;

    /// <summary>
    /// Threshold in milliseconds for considering a request slow
    /// </summary>
    public long SlowRequestThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Custom headers to add to all responses
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}

/// <summary>
/// Extension methods for adding response headers middleware
/// </summary>
public static class ResponseHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds response headers middleware to the pipeline
    /// </summary>
    /// <param name="builder">Application builder</param>
    /// <param name="configureOptions">Optional configuration</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseResponseHeaders(
        this IApplicationBuilder builder,
        Action<ResponseHeadersOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            builder.ApplicationServices.GetRequiredService<IServiceCollection>()
                .Configure(configureOptions);
        }

        return builder.UseMiddleware<ResponseHeadersMiddleware>();
    }

    /// <summary>
    /// Adds response headers middleware configuration to services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddResponseHeaders(
        this IServiceCollection services,
        Action<ResponseHeadersOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<ResponseHeadersOptions>(options => { });
        }

        return services;
    }
}

/// <summary>
/// Security headers middleware extension
/// </summary>
public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Adds security headers to the response
    /// </summary>
    /// <param name="builder">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.Use(async (context, next) =>
        {
            // Security headers that should be added early in the pipeline
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                // Remove server header for security
                headers.Remove("Server");

                // Add security headers
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["X-XSS-Protection"] = "1; mode=block";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Content-Security-Policy"] = "default-src 'self'";

                return Task.CompletedTask;
            });

            await next();
        });
    }
}