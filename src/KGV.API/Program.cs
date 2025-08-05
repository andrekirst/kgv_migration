using KGV.API.Configuration;
using KGV.API.Middleware;
using KGV.Application;
using KGV.Infrastructure;
using Microsoft.AspNetCore.Localization;
using Prometheus;
using Serilog;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "KGV.API")
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting KGV API application");

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Add custom services
    builder.Services.AddSwaggerConfiguration();
    builder.Services.AddAuthenticationConfiguration(builder.Configuration);
    builder.Services.AddAuthorizationConfiguration();
    builder.Services.AddCorsConfiguration();
    builder.Services.AddRateLimitingConfiguration();
    builder.Services.AddResponseHeaders();
    // builder.Services.AddHealthChecksConfiguration(builder.Configuration); // Temporarily disabled
    builder.Services.AddLocalizationConfiguration();

    // Add application layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add API versioning
    builder.Services.AddApiVersioning(opt =>
    {
        opt.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
        opt.AssumeDefaultVersionWhenUnspecified = true;
        opt.ApiVersionReader = Microsoft.AspNetCore.Mvc.Versioning.ApiVersionReader.Combine(
            new Microsoft.AspNetCore.Mvc.Versioning.QueryStringApiVersionReader("apiVersion"),
            new Microsoft.AspNetCore.Mvc.Versioning.HeaderApiVersionReader("X-Version"),
            new Microsoft.AspNetCore.Mvc.Versioning.UrlSegmentApiVersionReader());
    });

    builder.Services.AddVersionedApiExplorer(setup =>
    {
        setup.GroupNameFormat = "'v'VVV";
        setup.SubstituteApiVersionInUrl = true;
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "KGV API v1");
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.EnableFilter();
            c.EnableValidator();
        });
    }

    // Security headers
    app.UseSecurityHeaders();

    // Request localization
    var supportedCultures = new[]
    {
        new CultureInfo("de-DE"),
        new CultureInfo("en-US")
    };

    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture("de-DE"),
        SupportedCultures = supportedCultures,
        SupportedUICultures = supportedCultures
    });

    // Middleware pipeline
    // app.UseHttpsRedirection(); // Temporarily disabled for testing
    app.UseSerilogRequestLogging();
    app.UseResponseHeaders();
    app.UseRateLimiter();
    app.UseCors("KgvCorsPolicy");
    
    app.UseAuthentication();
    app.UseAuthorization();

    // Global exception handling
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Metrics
    app.UseHttpMetrics();

    // Health checks - temporarily disabled
    // app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
    // {
    //     ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    // });

    // app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
    // {
    //     Predicate = check => check.Tags.Contains("ready"),
    //     ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    // });

    // app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
    // {
    //     Predicate = _ => false,
    //     ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    // });

    // Prometheus metrics endpoint
    app.MapMetrics();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}