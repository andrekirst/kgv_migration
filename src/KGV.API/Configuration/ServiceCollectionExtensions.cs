using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using KGV.API.Filters;

namespace KGV.API.Configuration;

/// <summary>
/// Extension methods for configuring services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Swagger/OpenAPI configuration with comprehensive documentation
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "KGV API",
                Version = "v1.0",
                Description = @"
# KGV (Kleingartenverein) Management API

A comprehensive REST API for German allotment garden association management, providing complete CRUD operations for:

## Core Features
- **🏡 Bezirke (Districts)**: Manage garden districts with cadastral area relationships
- **🌱 Parzellen (Garden Plots)**: Handle plot assignments, availability, and specifications
- **📝 Anträge (Applications)**: Process membership and plot assignment requests
- **📊 Statistics**: Generate comprehensive reports and analytics

## API Characteristics
- **REST Architecture**: Resource-based URLs with proper HTTP verbs
- **HATEOAS Support**: Hypermedia links for resource navigation
- **Comprehensive Filtering**: Advanced query parameters for all endpoints
- **Pagination**: Efficient handling of large datasets
- **Localization**: German language support with English fallback
- **Security**: JWT-based authentication with role-based authorization
- **Rate Limiting**: Protection against abuse with configurable limits
- **Caching**: Optimized performance with appropriate cache headers
- **Error Handling**: RFC 7807 Problem Details for consistent error responses

## Authentication
Use JWT Bearer tokens in the Authorization header:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Rate Limits
- **Default Policy**: 100 requests per minute
- **Strict Policy**: 10 requests per minute (for sensitive operations)

## Response Formats
All responses follow consistent patterns with appropriate HTTP status codes and headers.
",
                TermsOfService = new Uri("https://kgv.de/terms"),
                Contact = new OpenApiContact
                {
                    Name = "KGV API Development Team",
                    Email = "api-support@kgv.de",
                    Url = new Uri("https://kgv.de/contact")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Add servers for different environments
            c.AddServer(new OpenApiServer
            {
                Url = "https://api.kgv.de",
                Description = "Production Server"
            });
            
            c.AddServer(new OpenApiServer
            {
                Url = "https://staging-api.kgv.de",
                Description = "Staging Server"
            });
            
            c.AddServer(new OpenApiServer
            {
                Url = "https://localhost:5000",
                Description = "Development Server"
            });

            // Add JWT authentication to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = @"JWT Authorization header using the Bearer scheme.
                
**Enter your token in the text input below.**

Example: `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`

You can obtain a token by calling the `/auth/login` endpoint with valid credentials.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
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
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            // Include XML comments from other projects
            var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, "KGV.Application.xml");
            if (File.Exists(applicationXmlPath))
            {
                c.IncludeXmlComments(applicationXmlPath, includeControllerXmlComments: true);
            }

            var domainXmlPath = Path.Combine(AppContext.BaseDirectory, "KGV.Domain.xml");
            if (File.Exists(domainXmlPath))
            {
                c.IncludeXmlComments(domainXmlPath, includeControllerXmlComments: true);
            }

            // Enable annotations for enhanced documentation
            c.EnableAnnotations(enableAnnotationsForInheritance: true, enableAnnotationsForPolymorphism: true);

            // Configure schema generation
            c.SchemaFilter<EnumSchemaFilter>();
            c.OperationFilter<ResponseHeadersOperationFilter>();
            c.DocumentFilter<LowercaseDocumentFilter>();

            // Configure example generation
            c.OperationFilter<ExampleOperationFilter>();

            // Use full type names for schema IDs to avoid conflicts
            c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

            // Configure for proper API versioning
            c.DocInclusionPredicate((name, api) => true);
            
            // Order actions by relative path
            c.OrderActionsBy(apiDesc => 
                $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.RelativePath}");
        });

        return services;
    }

    /// <summary>
    /// Adds JWT authentication configuration
    /// </summary>
    public static IServiceCollection AddAuthenticationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured"));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(context.Exception, "Authentication failed");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("Token validated for user: {User}", context.Principal?.Identity?.Name);
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Adds authorization configuration
    /// </summary>
    public static IServiceCollection AddAuthorizationConfiguration(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdminRole", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("RequireUserRole", policy =>
                policy.RequireRole("User", "Admin"));

            options.AddPolicy("RequireSachbearbeiterRole", policy =>
                policy.RequireRole("Sachbearbeiter", "Admin"));

            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    /// <summary>
    /// Adds CORS configuration
    /// </summary>
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("KgvCorsPolicy", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }

    /// <summary>
    /// Adds rate limiting configuration
    /// </summary>
    public static IServiceCollection AddRateLimitingConfiguration(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("DefaultPolicy", opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
            });

            options.AddFixedWindowLimiter("StrictPolicy", opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 2;
            });
        });

        return services;
    }

    /// <summary>
    /// Adds health checks configuration
    /// </summary>
    public static IServiceCollection AddHealthChecksConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Database health check
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddNpgSql(
                connectionString,
                name: "postgresql",
                tags: new[] { "ready", "database" });
        }

        // Redis health check is handled by Infrastructure layer

        return services;
    }

    /// <summary>
    /// Adds localization configuration with error localization service
    /// </summary>
    public static IServiceCollection AddLocalizationConfiguration(this IServiceCollection services)
    {
        services.AddLocalization(options =>
        {
            options.ResourcesPath = "Resources";
        });

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("de-DE"),
                new CultureInfo("en-US")
            };

            options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("de-DE");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            options.RequestCultureProviders.Insert(0, new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider());
        });

        // Register error localization service
        services.AddScoped<KGV.API.Services.IErrorLocalizationService, KGV.API.Services.ErrorLocalizationService>();

        return services;
    }
}