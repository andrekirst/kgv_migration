using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.StranglerFig
{
    /// <summary>
    /// Strangler Fig Pattern Implementation für progressive Migration
    /// Routet Requests basierend auf Konfiguration zwischen Legacy und New System
    /// </summary>
    public class StranglerFigMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StranglerFigMiddleware> _logger;
        private readonly IMetricsCollector _metrics;
        private readonly MigrationRouteConfig[] _routes;
        private readonly Random _random = new Random();

        public StranglerFigMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<StranglerFigMiddleware> logger,
            IMetricsCollector metrics)
        {
            _next = next;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _metrics = metrics;
            _routes = LoadMigrationRoutes();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            var method = context.Request.Method;
            
            _logger.LogDebug("Processing request: {Method} {Path}", method, path);

            var routingDecision = DetermineRouting(path, method);
            
            // Log routing decision for monitoring
            _metrics.RecordRoutingDecision(path, routingDecision);

            switch (routingDecision.Target)
            {
                case RoutingTarget.NewSystem:
                    await RouteToNewSystem(context);
                    break;
                    
                case RoutingTarget.LegacySystem:
                    await RouteToLegacySystem(context);
                    break;
                    
                case RoutingTarget.DualWrite:
                    await ExecuteDualWriteStrategy(context);
                    break;
                    
                case RoutingTarget.Shadow:
                    await ExecuteShadowStrategy(context);
                    break;
            }
        }

        private RoutingDecision DetermineRouting(string path, string method)
        {
            foreach (var route in _routes)
            {
                if (!Regex.IsMatch(path, route.Pattern))
                    continue;

                // Check if specific HTTP method is configured
                if (route.Methods?.Any() == true && !route.Methods.Contains(method))
                    continue;

                // Special handling for data modification operations
                if (IsDataModificationOperation(method) && route.EnableDualWrite)
                {
                    return new RoutingDecision 
                    { 
                        Target = RoutingTarget.DualWrite,
                        Route = route
                    };
                }

                // Shadow mode for validation
                if (route.EnableShadowMode)
                {
                    return new RoutingDecision
                    {
                        Target = RoutingTarget.Shadow,
                        Route = route
                    };
                }

                // Percentage-based routing
                if (route.MigrationPercentage >= 100)
                {
                    return new RoutingDecision 
                    { 
                        Target = RoutingTarget.NewSystem,
                        Route = route
                    };
                }

                if (route.MigrationPercentage <= 0)
                {
                    return new RoutingDecision 
                    { 
                        Target = RoutingTarget.LegacySystem,
                        Route = route
                    };
                }

                // A/B testing based on percentage
                var randomValue = _random.Next(100);
                var target = randomValue < route.MigrationPercentage 
                    ? RoutingTarget.NewSystem 
                    : RoutingTarget.LegacySystem;

                return new RoutingDecision 
                { 
                    Target = target,
                    Route = route
                };
            }

            // Default to legacy for safety
            _logger.LogWarning("No routing rule found for {Path}, defaulting to legacy", path);
            return new RoutingDecision 
            { 
                Target = RoutingTarget.LegacySystem,
                Route = null
            };
        }

        private async Task RouteToNewSystem(HttpContext context)
        {
            _logger.LogInformation("Routing to new system: {Path}", context.Request.Path);
            
            // Add correlation header for tracing
            context.Request.Headers.Add("X-Migration-Source", "strangler-fig");
            context.Request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString());
            
            // Continue to next middleware (new system)
            await _next(context);
        }

        private async Task RouteToLegacySystem(HttpContext context)
        {
            _logger.LogInformation("Routing to legacy system: {Path}", context.Request.Path);
            
            var client = _httpClientFactory.CreateClient("LegacySystem");
            
            try
            {
                // Create proxy request
                var proxyRequest = await CreateProxyRequest(context, client.BaseAddress);
                
                // Send to legacy system
                var response = await client.SendAsync(proxyRequest);
                
                // Copy response back to client
                await CopyProxyResponse(context, response);
                
                _metrics.RecordLegacyCall(context.Request.Path.Value, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to proxy to legacy system");
                context.Response.StatusCode = 502; // Bad Gateway
                await context.Response.WriteAsync("Legacy system unavailable");
            }
        }

        private async Task ExecuteDualWriteStrategy(HttpContext context)
        {
            _logger.LogInformation("Executing dual-write strategy for: {Path}", context.Request.Path);
            
            // Buffer the request body for dual use
            var originalBody = await BufferRequestBody(context);
            
            // Execute on new system first (primary)
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(originalBody));
            await _next(context);
            
            var newSystemStatus = context.Response.StatusCode;
            
            // Only proceed with legacy write if new system succeeded
            if (newSystemStatus >= 200 && newSystemStatus < 300)
            {
                // Async write to legacy (fire and forget with monitoring)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var client = _httpClientFactory.CreateClient("LegacySystem");
                        var legacyRequest = CreateLegacyRequest(context, originalBody);
                        var legacyResponse = await client.SendAsync(legacyRequest);
                        
                        if (!legacyResponse.IsSuccessStatusCode)
                        {
                            _logger.LogWarning(
                                "Dual write to legacy failed: {Path}, Status: {Status}",
                                context.Request.Path,
                                legacyResponse.StatusCode);
                            
                            _metrics.RecordDualWriteFailure(context.Request.Path.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Dual write to legacy system failed");
                        _metrics.RecordDualWriteFailure(context.Request.Path.Value);
                    }
                });
            }
        }

        private async Task ExecuteShadowStrategy(HttpContext context)
        {
            _logger.LogInformation("Executing shadow strategy for: {Path}", context.Request.Path);
            
            // Buffer request for comparison
            var originalBody = await BufferRequestBody(context);
            
            // Execute on primary system (legacy)
            var legacyTask = CallLegacySystem(context, originalBody);
            
            // Execute on new system (shadow)
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(originalBody));
            await _next(context);
            var newSystemResponse = context.Response;
            
            // Wait for legacy response
            var legacyResponse = await legacyTask;
            
            // Compare responses for validation
            await CompareResponses(
                context.Request.Path.Value,
                legacyResponse,
                newSystemResponse);
            
            // Return legacy response to client (shadow mode doesn't affect users)
            await CopyProxyResponse(context, legacyResponse);
        }

        private async Task CompareResponses(
            string path,
            HttpResponseMessage legacyResponse,
            HttpResponse newResponse)
        {
            var comparison = new ResponseComparison
            {
                Path = path,
                Timestamp = DateTime.UtcNow,
                LegacyStatus = (int)legacyResponse.StatusCode,
                NewSystemStatus = newResponse.StatusCode,
                StatusMatch = legacyResponse.StatusCode == (System.Net.HttpStatusCode)newResponse.StatusCode
            };

            // Log comparison for analysis
            _logger.LogInformation(
                "Shadow comparison - Path: {Path}, Legacy: {LegacyStatus}, New: {NewStatus}, Match: {Match}",
                comparison.Path,
                comparison.LegacyStatus,
                comparison.NewSystemStatus,
                comparison.StatusMatch);

            _metrics.RecordShadowComparison(comparison);

            // Store detailed comparison for later analysis
            await StoreComparisonResult(comparison);
        }

        private async Task<string> BufferRequestBody(HttpContext context)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            return body;
        }

        private async Task<HttpRequestMessage> CreateProxyRequest(HttpContext context, Uri baseAddress)
        {
            var targetUrl = new Uri(baseAddress, context.Request.Path + context.Request.QueryString);
            var requestMessage = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = targetUrl
            };

            // Copy headers
            foreach (var header in context.Request.Headers)
            {
                if (!header.Key.StartsWith("Host", StringComparison.OrdinalIgnoreCase))
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            // Copy body if present
            if (context.Request.ContentLength > 0)
            {
                var body = await BufferRequestBody(context);
                requestMessage.Content = new StringContent(body, Encoding.UTF8, context.Request.ContentType);
            }

            return requestMessage;
        }

        private HttpRequestMessage CreateLegacyRequest(HttpContext context, string body)
        {
            var client = _httpClientFactory.CreateClient("LegacySystem");
            var targetUrl = new Uri(client.BaseAddress, context.Request.Path + context.Request.QueryString);
            
            var request = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = targetUrl,
                Content = new StringContent(body, Encoding.UTF8, context.Request.ContentType)
            };

            // Copy relevant headers
            foreach (var header in context.Request.Headers)
            {
                if (!header.Key.StartsWith("Host", StringComparison.OrdinalIgnoreCase))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            return request;
        }

        private async Task<HttpResponseMessage> CallLegacySystem(HttpContext context, string body)
        {
            var client = _httpClientFactory.CreateClient("LegacySystem");
            var request = CreateLegacyRequest(context, body);
            return await client.SendAsync(request);
        }

        private async Task CopyProxyResponse(HttpContext context, HttpResponseMessage response)
        {
            context.Response.StatusCode = (int)response.StatusCode;

            // Copy headers
            foreach (var header in response.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in response.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            // Copy body
            await response.Content.CopyToAsync(context.Response.Body);
        }

        private bool IsDataModificationOperation(string method)
        {
            return method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                   method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                   method.Equals("PATCH", StringComparison.OrdinalIgnoreCase) ||
                   method.Equals("DELETE", StringComparison.OrdinalIgnoreCase);
        }

        private MigrationRouteConfig[] LoadMigrationRoutes()
        {
            var routes = _configuration
                .GetSection("StranglerFig:Routes")
                .Get<MigrationRouteConfig[]>() ?? Array.Empty<MigrationRouteConfig>();

            _logger.LogInformation("Loaded {Count} migration routes", routes.Length);
            
            foreach (var route in routes)
            {
                _logger.LogInformation(
                    "Route: {Pattern} -> {Percentage}% to new system",
                    route.Pattern,
                    route.MigrationPercentage);
            }

            return routes;
        }

        private async Task StoreComparisonResult(ResponseComparison comparison)
        {
            // Store in database or file for analysis
            var json = JsonSerializer.Serialize(comparison);
            _logger.LogDebug("Comparison result: {Json}", json);
            
            // TODO: Implement persistent storage for comparison analysis
        }
    }

    // Supporting classes
    public enum RoutingTarget
    {
        NewSystem,
        LegacySystem,
        DualWrite,
        Shadow
    }

    public class RoutingDecision
    {
        public RoutingTarget Target { get; set; }
        public MigrationRouteConfig Route { get; set; }
    }

    public class MigrationRouteConfig
    {
        public string Pattern { get; set; }
        public int MigrationPercentage { get; set; }
        public string[] Methods { get; set; }
        public bool EnableDualWrite { get; set; }
        public bool EnableShadowMode { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }

    public class ResponseComparison
    {
        public string Path { get; set; }
        public DateTime Timestamp { get; set; }
        public int LegacyStatus { get; set; }
        public int NewSystemStatus { get; set; }
        public bool StatusMatch { get; set; }
        public string LegacyBody { get; set; }
        public string NewSystemBody { get; set; }
        public Dictionary<string, object> Differences { get; set; }
    }

    public interface IMetricsCollector
    {
        void RecordRoutingDecision(string path, RoutingDecision decision);
        void RecordLegacyCall(string path, System.Net.HttpStatusCode status);
        void RecordDualWriteFailure(string path);
        void RecordShadowComparison(ResponseComparison comparison);
    }

    // Dummy implementation
    public class MetricsCollector : IMetricsCollector
    {
        private readonly ILogger<MetricsCollector> _logger;

        public MetricsCollector(ILogger<MetricsCollector> logger)
        {
            _logger = logger;
        }

        public void RecordRoutingDecision(string path, RoutingDecision decision)
        {
            _logger.LogDebug("Routing decision for {Path}: {Target}", path, decision.Target);
        }

        public void RecordLegacyCall(string path, System.Net.HttpStatusCode status)
        {
            _logger.LogDebug("Legacy call to {Path}: {Status}", path, status);
        }

        public void RecordDualWriteFailure(string path)
        {
            _logger.LogWarning("Dual write failure for {Path}", path);
        }

        public void RecordShadowComparison(ResponseComparison comparison)
        {
            _logger.LogInformation("Shadow comparison recorded for {Path}", comparison.Path);
        }
    }
}