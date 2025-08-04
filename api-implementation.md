# KGV Management System - API Layer Implementation

## Program.cs - Application Startup

```csharp
using KGV.Management.Api.Extensions;
using KGV.Management.Application.Extensions;
using KGV.Management.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/kgv-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting KGV Management API");

    // Add services to the container
    builder.Services.AddApiServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    var app = builder.Build();

    // Configure the HTTP request pipeline
    await app.ConfigureApiPipelineAsync();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

## Service Registration Extensions

### API Services Extension
```csharp
public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Add controllers
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
            options.Filters.Add<GlobalExceptionFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Add API versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Version"),
                new QueryStringApiVersionReader("version")
            );
        });

        services.AddVersionedApiExplorer(setup =>
        {
            setup.GroupNameFormat = "'v'VVV";
            setup.SubstituteApiVersionInUrl = true;
        });

        // Add Swagger/OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "KGV Management API", 
                Version = "v1",
                Description = "API for KGV (Kleingartenverein) Management System",
                Contact = new OpenApiContact
                {
                    Name = "KGV Team",
                    Email = "admin@kgv.de"
                }
            });

            // Add JWT Authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        // Add CORS
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                policy.WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        // Add Authentication & Authorization
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("Token validated for user: {UserId}", 
                            context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Authentication failed: {Exception}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Permissions.ViewApplications, policy => 
                policy.RequireAuthenticatedUser().RequireClaim("permission", Permissions.ViewApplications));
            
            options.AddPolicy(Permissions.CreateApplications, policy => 
                policy.RequireAuthenticatedUser().RequireClaim("permission", Permissions.CreateApplications));
            
            options.AddPolicy(Permissions.UpdateApplications, policy => 
                policy.RequireAuthenticatedUser().RequireClaim("permission", Permissions.UpdateApplications));
            
            options.AddPolicy(Permissions.DeleteApplications, policy => 
                policy.RequireAuthenticatedUser().RequireClaim("permission", Permissions.DeleteApplications));
            
            options.AddPolicy(Permissions.ManagePersonnel, policy => 
                policy.RequireAuthenticatedUser().RequireClaim("permission", Permissions.ManagePersonnel));
        });

        // Add Rate Limiting
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Too many requests", token);
            };
        });

        // Add Health Checks
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!)
            .AddRedis(configuration.GetConnectionString("Redis")!);

        // Add Response Compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        // Add Memory Cache
        services.AddMemoryCache();

        // Add Distributed Cache (Redis)
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "KGV";
        });

        // Add Current User Service
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Add FluentValidation
        services.AddValidatorsFromAssemblyContaining<CreateApplicationRequestValidator>();

        return services;
    }
}
```

### Pipeline Configuration Extension
```csharp
public static class ApiPipelineExtensions
{
    public static async Task<WebApplication> ConfigureApiPipelineAsync(this WebApplication app)
    {
        // Apply pending migrations
        await app.ApplyMigrationsAsync();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "KGV Management API v1");
                c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
            });
        }

        // Security headers
        app.UseSecurityHeaders();

        // Request/Response logging
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
                diagnosticContext.Set("UserId", httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            };
        });

        // Exception handling
        app.UseGlobalExceptionHandler();

        // HTTPS redirection
        app.UseHttpsRedirection();

        // Response compression
        app.UseResponseCompression();

        // CORS
        app.UseCors("DefaultPolicy");

        // Rate limiting
        app.UseRateLimiter();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Health checks
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Map controllers
        app.MapControllers();

        return app;
    }

    private static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KgvDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying database migrations");
            throw;
        }
    }
}
```

## Controllers

### ApplicationsController
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/applications")]
[ApiVersion("1.0")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(
        IApplicationService applicationService,
        ILogger<ApplicationsController> logger)
    {
        _applicationService = applicationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all applications with optional filtering and pagination
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of applications</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.ViewApplications)]
    [ProducesResponseType(typeof(PagedResult<ApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ApplicationResponse>>> GetApplicationsAsync(
        [FromQuery] ApplicationFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting applications with filter: {@Filter}", filter);
        
        var result = await _applicationService.GetApplicationsAsync(filter, cancellationToken);
        
        _logger.LogInformation("Retrieved {Count} applications out of {Total}", 
            result.Items.Count, result.TotalCount);
        
        return Ok(result);
    }

    /// <summary>
    /// Get application by ID
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Application details</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ViewApplications)]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApplicationResponse>> GetApplicationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting application with ID: {ApplicationId}", id);
        
        var application = await _applicationService.GetApplicationByIdAsync(id, cancellationToken);
        
        if (application == null)
        {
            _logger.LogWarning("Application with ID {ApplicationId} not found", id);
            return NotFound($"Application with ID {id} not found");
        }

        return Ok(application);
    }

    /// <summary>
    /// Create a new application
    /// </summary>
    /// <param name="request">Application creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created application</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.CreateApplications)]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApplicationResponse>> CreateApplicationAsync(
        [FromBody] CreateApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new application for {FirstName} {LastName}", 
            request.FirstName, request.LastName);
        
        var application = await _applicationService.CreateApplicationAsync(request, cancellationToken);
        
        _logger.LogInformation("Created application with ID {ApplicationId} and file reference {FileReference}", 
            application.Id, application.FileReference);
        
        return CreatedAtAction(
            nameof(GetApplicationByIdAsync), 
            new { id = application.Id }, 
            application);
    }

    /// <summary>
    /// Update an existing application
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Application update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.UpdateApplications)]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApplicationResponse>> UpdateApplicationAsync(
        Guid id,
        [FromBody] UpdateApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating application with ID: {ApplicationId}", id);
        
        var application = await _applicationService.UpdateApplicationAsync(id, request, cancellationToken);
        
        _logger.LogInformation("Updated application with ID: {ApplicationId}", id);
        
        return Ok(application);
    }

    /// <summary>
    /// Delete an application
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.DeleteApplications)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteApplicationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting application with ID: {ApplicationId}", id);
        
        await _applicationService.DeleteApplicationAsync(id, cancellationToken);
        
        _logger.LogInformation("Deleted application with ID: {ApplicationId}", id);
        
        return NoContent();
    }

    /// <summary>
    /// Update application status
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Status update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application</returns>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.UpdateApplications)]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApplicationResponse>> UpdateStatusAsync(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating status for application {ApplicationId} to {Status}", 
            id, request.Status);
        
        var application = await _applicationService.UpdateStatusAsync(
            id, request.Status, request.Comment, cancellationToken);
        
        _logger.LogInformation("Updated status for application {ApplicationId} to {Status}", 
            id, request.Status);
        
        return Ok(application);
    }

    /// <summary>
    /// Get application history
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of history entries</returns>
    [HttpGet("{id:guid}/history")]
    [Authorize(Policy = Permissions.ViewApplications)]
    [ProducesResponseType(typeof(List<HistoryEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<HistoryEntryResponse>>> GetHistoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting history for application: {ApplicationId}", id);
        
        var history = await _applicationService.GetHistoryAsync(id, cancellationToken);
        
        return Ok(history);
    }

    /// <summary>
    /// Add history entry to application
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">History entry request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created history entry</returns>
    [HttpPost("{id:guid}/history")]
    [Authorize(Policy = Permissions.UpdateApplications)]
    [ProducesResponseType(typeof(HistoryEntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HistoryEntryResponse>> AddHistoryEntryAsync(
        Guid id,
        [FromBody] AddHistoryEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding history entry of type {Type} to application {ApplicationId}", 
            request.Type, id);
        
        var historyEntry = await _applicationService.AddHistoryEntryAsync(id, request, cancellationToken);
        
        _logger.LogInformation("Added history entry {HistoryId} to application {ApplicationId}", 
            historyEntry.Id, id);
        
        return CreatedAtAction(
            nameof(GetHistoryAsync), 
            new { id }, 
            historyEntry);
    }

    /// <summary>
    /// Get waiting list for a district
    /// </summary>
    /// <param name="district">District name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of applications in waiting list order</returns>
    [HttpGet("waiting-list")]
    [Authorize(Policy = Permissions.ViewApplications)]
    [ProducesResponseType(typeof(List<ApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ApplicationResponse>>> GetWaitingListAsync(
        [FromQuery, Required] string district,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting waiting list for district: {District}", district);
        
        var waitingList = await _applicationService.GetWaitingListAsync(district, cancellationToken);
        
        _logger.LogInformation("Retrieved waiting list with {Count} applications for district {District}", 
            waitingList.Count, district);
        
        return Ok(waitingList);
    }

    /// <summary>
    /// Create offer for application
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Offer creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application with offer</returns>
    [HttpPost("{id:guid}/offer")]
    [Authorize(Policy = Permissions.UpdateApplications)]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApplicationResponse>> CreateOfferAsync(
        Guid id,
        [FromBody] CreateOfferRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating offer for application {ApplicationId}", id);
        
        var application = await _applicationService.CreateOfferAsync(id, request, cancellationToken);
        
        _logger.LogInformation("Created offer for application {ApplicationId}", id);
        
        return Ok(application);
    }
}
```

### AuthController
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authenticationService,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and get JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with JWT token</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResult>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);
        
        var result = await _authenticationService.LoginAsync(request, cancellationToken);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Successful login for user: {Username}", request.Username);
            return Ok(result);
        }
        
        _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
        return Unauthorized("Invalid credentials");
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New authentication result</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResult>> RefreshTokenAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Token refresh attempt");
        
        var result = await _authenticationService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Token refreshed successfully");
            return Ok(result);
        }
        
        _logger.LogWarning("Failed token refresh attempt");
        return Unauthorized("Invalid refresh token");
    }

    /// <summary>
    /// Logout user and invalidate tokens
    /// </summary>
    /// <param name="request">Logout request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAsync(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("Logout for user: {UserId}", userId);
        
        await _authenticationService.LogoutAsync(request.RefreshToken, cancellationToken);
        
        _logger.LogInformation("User {UserId} logged out successfully", userId);
        
        return NoContent();
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var permissions = User.FindAll("permission").Select(c => c.Value).ToList();

        var response = new CurrentUserResponse
        {
            Id = userId,
            Username = username,
            Email = email,
            Permissions = permissions
        };

        return Ok(response);
    }
}
```

## Middleware

### Global Exception Handling Middleware
```csharp
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
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
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            ApplicationNotFoundException ex => new ErrorResponse(
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                ex.Message),
            
            ValidationException ex => new ErrorResponse(
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                ex.Message,
                ex.Errors?.Select(e => e.ErrorMessage).ToList()),
            
            UnauthorizedAccessException => new ErrorResponse(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "You are not authorized to access this resource"),
            
            InvalidOperationException ex => new ErrorResponse(
                StatusCodes.Status400BadRequest,
                "Invalid Operation",
                ex.Message),
            
            _ => new ErrorResponse(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An error occurred while processing your request")
        };

        response.StatusCode = errorResponse.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}

public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
```

### Security Headers Middleware
```csharp
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
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' https:; connect-src 'self'; frame-ancestors 'none';");
        
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
```

## Filters

### Validation Filter
```csharp
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            var validationProblem = new ValidationProblemDetails(errors)
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Instance = context.HttpContext.Request.Path
            };

            context.Result = new BadRequestObjectResult(validationProblem);
            return;
        }

        await next();
    }
}
```

### Global Exception Filter
```csharp
public class GlobalExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "An exception occurred: {Message}", context.Exception.Message);

        var response = context.Exception switch
        {
            ApplicationNotFoundException ex => new ErrorResponse(
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                ex.Message),
            
            ValidationException ex => new ErrorResponse(
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                ex.Message,
                ex.Errors?.Select(e => e.ErrorMessage).ToList()),
            
            _ => new ErrorResponse(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An error occurred while processing your request")
        };

        context.Result = new ObjectResult(response)
        {
            StatusCode = response.StatusCode
        };

        context.ExceptionHandled = true;

        return Task.CompletedTask;
    }
}
```

## Configuration Files

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=kgv_management;Username=kgv_user;Password=your_password_here",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-that-is-at-least-32-characters-long",
    "Issuer": "KGV.Management.Api",
    "Audience": "KGV.Management.Client",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://localhost:3001"
    ]
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/kgv-api-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=kgv_management_dev;Username=kgv_dev;Password=dev_password",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "development-secret-key-for-testing-only-not-for-production-use",
    "ExpirationMinutes": 120
  }
}
```

### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "KGV.Management": "Information"
    }
  },
  "AllowedHosts": "your-production-domain.com",
  "ConnectionStrings": {
    "DefaultConnection": "Host=prod-db-host;Database=kgv_management;Username=kgv_prod;Password=secure_production_password;SSL Mode=Require;Trust Server Certificate=true",
    "Redis": "prod-redis-host:6379"
  },
  "JwtSettings": {
    "SecretKey": "your-production-secret-key-256-bits-minimum-length-required-for-security",
    "ExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 1
  },
  "Cors": {
    "AllowedOrigins": [
      "https://your-frontend-domain.com"
    ]
  }
}
```

## Docker Configuration

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/KGV.Management.Api/KGV.Management.Api.csproj", "src/KGV.Management.Api/"]
COPY ["src/KGV.Management.Application/KGV.Management.Application.csproj", "src/KGV.Management.Application/"]
COPY ["src/KGV.Management.Domain/KGV.Management.Domain.csproj", "src/KGV.Management.Domain/"]
COPY ["src/KGV.Management.Infrastructure/KGV.Management.Infrastructure.csproj", "src/KGV.Management.Infrastructure/"]
RUN dotnet restore "./src/KGV.Management.Api/KGV.Management.Api.csproj"
COPY . .
WORKDIR "/src/src/KGV.Management.Api"
RUN dotnet build "./KGV.Management.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./KGV.Management.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KGV.Management.Api.dll"]
```

### docker-compose.yml
```yaml
version: '3.8'

services:
  kgv-api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:8080"
      - "5001:8081"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080;https://+:8081
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=kgv_management;Username=kgv_user;Password=kgv_password
      - ConnectionStrings__Redis=redis:6379
    depends_on:
      - postgres
      - redis
    volumes:
      - ./logs:/app/logs

  postgres:
    image: postgres:16
    environment:
      - POSTGRES_DB=kgv_management
      - POSTGRES_USER=kgv_user
      - POSTGRES_PASSWORD=kgv_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

volumes:
  postgres_data:
  redis_data:
```

This comprehensive API implementation provides:

1. **Complete ASP.NET Core Web API** with .NET 9 features
2. **JWT Authentication & Authorization** with role-based access control
3. **Comprehensive Controllers** with full CRUD operations and business logic
4. **Advanced Middleware** for security, exception handling, and logging
5. **Input Validation** using FluentValidation and model validation
6. **API Documentation** with Swagger/OpenAPI integration
7. **Performance Features** including rate limiting, caching, and compression
8. **Production-Ready Configuration** with environment-specific settings
9. **Docker Support** for containerized deployment
10. **Health Checks** for monitoring and observability

The implementation follows REST API best practices and provides a solid foundation for the modernized KGV management system.