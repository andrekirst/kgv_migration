using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using KGV.Domain.Enums;
using System.Text.Json;

namespace KGV.API.Filters;

/// <summary>
/// Swagger filter to enhance enum documentation with descriptions and examples
/// </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            schema.Type = "string";
            schema.Format = null;

            var enumValues = new List<IOpenApiAny>();
            var descriptions = new List<string>();

            foreach (var enumValue in Enum.GetValues(context.Type))
            {
                var enumName = enumValue.ToString();
                enumValues.Add(new OpenApiString(enumName));

                // Get description from Display attribute if available
                var fieldInfo = context.Type.GetField(enumName!);
                var displayAttribute = fieldInfo?.GetCustomAttribute<DisplayAttribute>();
                var description = displayAttribute?.Description ?? displayAttribute?.Name ?? enumName;
                descriptions.Add($"**{enumName}**: {description}");
            }

            schema.Enum = enumValues;
            schema.Description = $"{schema.Description}\n\nPossible values:\n{string.Join("\n", descriptions)}";

            // Add example value
            if (enumValues.Any())
            {
                schema.Example = enumValues.First();
            }
        }
    }
}

/// <summary>
/// Operation filter to add response headers documentation
/// </summary>
public class ResponseHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add pagination headers for GET operations that return collections
        if (context.MethodInfo.ReturnType.IsGenericType)
        {
            var genericType = context.MethodInfo.ReturnType.GetGenericTypeDefinition();
            if (genericType.Name.Contains("PaginatedResult") || 
                operation.OperationId?.Contains("Get") == true &&
                !operation.OperationId.EndsWith("ById"))
            {
                AddPaginationHeaders(operation);
            }
        }

        // Add cache headers for GET operations
        if (context.MethodInfo.Name.StartsWith("Get"))
        {
            AddCacheHeaders(operation);
        }

        // Add rate limiting headers
        AddRateLimitHeaders(operation);

        // Add API version headers
        AddApiVersionHeaders(operation);
    }

    private static void AddPaginationHeaders(OpenApiOperation operation)
    {
        if (operation.Responses.ContainsKey("200"))
        {
            var response = operation.Responses["200"];
            response.Headers ??= new Dictionary<string, OpenApiHeader>();

            response.Headers["X-Pagination-CurrentPage"] = new OpenApiHeader
            {
                Description = "The current page number",
                Schema = new OpenApiSchema { Type = "integer" }
            };

            response.Headers["X-Pagination-PageSize"] = new OpenApiHeader
            {
                Description = "The number of items per page",
                Schema = new OpenApiSchema { Type = "integer" }
            };

            response.Headers["X-Pagination-TotalCount"] = new OpenApiHeader
            {
                Description = "The total number of items across all pages",
                Schema = new OpenApiSchema { Type = "integer" }
            };

            response.Headers["X-Pagination-TotalPages"] = new OpenApiHeader
            {
                Description = "The total number of pages",
                Schema = new OpenApiSchema { Type = "integer" }
            };

            response.Headers["X-Pagination-HasNextPage"] = new OpenApiHeader
            {
                Description = "Whether there is a next page available",
                Schema = new OpenApiSchema { Type = "boolean" }
            };

            response.Headers["X-Pagination-HasPreviousPage"] = new OpenApiHeader
            {
                Description = "Whether there is a previous page available", 
                Schema = new OpenApiSchema { Type = "boolean" }
            };
        }
    }

    private static void AddCacheHeaders(OpenApiOperation operation)
    {
        foreach (var response in operation.Responses.Where(r => r.Key.StartsWith("2")))
        {
            response.Value.Headers ??= new Dictionary<string, OpenApiHeader>();

            response.Value.Headers["Cache-Control"] = new OpenApiHeader
            {
                Description = "Caching directives for the response",
                Schema = new OpenApiSchema { Type = "string" },
                Example = new OpenApiString("private, max-age=300")
            };

            response.Value.Headers["ETag"] = new OpenApiHeader
            {
                Description = "Entity tag for caching validation",
                Schema = new OpenApiSchema { Type = "string" },
                Example = new OpenApiString("\"abc123def456\"")
            };
        }
    }

    private static void AddRateLimitHeaders(OpenApiOperation operation)
    {
        foreach (var response in operation.Responses)
        {
            response.Value.Headers ??= new Dictionary<string, OpenApiHeader>();

            response.Value.Headers["X-RateLimit-Limit"] = new OpenApiHeader
            {
                Description = "The maximum number of requests allowed per time window",
                Schema = new OpenApiSchema { Type = "integer" },
                Example = new OpenApiInteger(100)
            };

            response.Value.Headers["X-RateLimit-Remaining"] = new OpenApiHeader
            {
                Description = "The number of requests remaining in the current time window",
                Schema = new OpenApiSchema { Type = "integer" },
                Example = new OpenApiInteger(95)
            };

            response.Value.Headers["X-RateLimit-Reset"] = new OpenApiHeader
            {
                Description = "The time when the rate limit window resets (Unix timestamp)",
                Schema = new OpenApiSchema { Type = "integer" },
                Example = new OpenApiInteger(1642784400)
            };
        }
    }

    private static void AddApiVersionHeaders(OpenApiOperation operation)
    {
        foreach (var response in operation.Responses)
        {
            response.Value.Headers ??= new Dictionary<string, OpenApiHeader>();

            response.Value.Headers["API-Version"] = new OpenApiHeader
            {
                Description = "The version of the API that processed this request",
                Schema = new OpenApiSchema { Type = "string" },
                Example = new OpenApiString("1.0")
            };

            response.Value.Headers["API-Supported-Versions"] = new OpenApiHeader
            {
                Description = "Comma-separated list of supported API versions",
                Schema = new OpenApiSchema { Type = "string" },
                Example = new OpenApiString("1.0")
            };
        }
    }
}

/// <summary>
/// Document filter to convert paths to lowercase
/// </summary>
public class LowercaseDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = swaggerDoc.Paths.ToDictionary(
            path => path.Key.ToLowerInvariant(),
            path => path.Value
        );

        swaggerDoc.Paths.Clear();
        foreach (var path in paths)
        {
            swaggerDoc.Paths.Add(path.Key, path.Value);
        }
    }
}

/// <summary>
/// Operation filter to add comprehensive examples for requests and responses
/// </summary>
public class ExampleOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        AddRequestExamples(operation, context);
        AddResponseExamples(operation, context);
    }

    private static void AddRequestExamples(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content == null) return;

        foreach (var content in operation.RequestBody.Content)
        {
            if (content.Key == "application/json")
            {
                // Add examples based on the operation and parameters
                if (context.MethodInfo.Name.Contains("Bezirk"))
                {
                    AddBezirkExamples(content.Value, context.MethodInfo.Name);
                }
                else if (context.MethodInfo.Name.Contains("Parzelle"))
                {
                    AddParzelleExamples(content.Value, context.MethodInfo.Name);
                }
            }
        }
    }

    private static void AddResponseExamples(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var response in operation.Responses.Where(r => r.Key.StartsWith("2")))
        {
            if (response.Value.Content?.ContainsKey("application/json") == true)
            {
                var content = response.Value.Content["application/json"];
                
                if (context.MethodInfo.Name.Contains("Bezirk"))
                {
                    AddBezirkResponseExamples(content, context.MethodInfo.Name);
                }
                else if (context.MethodInfo.Name.Contains("Parzelle"))
                {
                    AddParzelleResponseExamples(content, context.MethodInfo.Name);
                }
            }
        }
    }

    private static void AddBezirkExamples(OpenApiMediaType mediaType, string methodName)
    {
        if (methodName.Contains("Create"))
        {
            mediaType.Example = new OpenApiString(JsonSerializer.Serialize(new
            {
                name = "Bezirk-A",
                displayName = "Bezirk A - Nordost",
                description = "Nördlicher Bereich des Gartenvereins mit 45 Parzellen",
                isActive = true,
                sortOrder = 1
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else if (methodName.Contains("Update"))
        {
            mediaType.Example = new OpenApiString(JsonSerializer.Serialize(new
            {
                id = "550e8400-e29b-41d4-a716-446655440000",
                name = "Bezirk-A",
                displayName = "Bezirk A - Nordost",
                description = "Nördlicher Bereich des Gartenvereins mit 45 Parzellen",
                isActive = true,
                sortOrder = 1
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static void AddParzelleExamples(OpenApiMediaType mediaType, string methodName)
    {
        if (methodName.Contains("Create"))
        {
            mediaType.Example = new OpenApiString(JsonSerializer.Serialize(new
            {
                nummer = "A-15",
                bezirkId = "550e8400-e29b-41d4-a716-446655440000",
                flaeche = 350.5m,
                status = "Available",
                preis = 125.00m,
                beschreibung = "Sonnige Parzelle mit Obstbäumen",
                hasWasser = true,
                hasStrom = true,
                prioritaet = 1
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else if (methodName.Contains("Assign"))
        {
            mediaType.Example = new OpenApiString(JsonSerializer.Serialize(new
            {
                assigneeId = "123e4567-e89b-12d3-a456-426614174000",
                assignmentDate = DateTime.UtcNow,
                notes = "Vergabe nach Warteliste",
                assignmentPrice = 125.00m
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static void AddBezirkResponseExamples(OpenApiMediaType mediaType, string methodName)
    {
        if (methodName.Contains("GetBezirke") && !methodName.EndsWith("ById"))
        {
            // Paginated list example
            mediaType.Example = new OpenApiString(JsonSerializer.Serialize(new
            {
                data = new[]
                {
                    new
                    {
                        id = "550e8400-e29b-41d4-a716-446655440000",
                        name = "Bezirk-A",
                        anzeigeName = "Bezirk A - Nordost",
                        isActive = true,
                        sortOrder = 1,
                        anzahlKatasterbezirke = 3,
                        anzahlAntraege = 12
                    },
                    new
                    {
                        id = "550e8400-e29b-41d4-a716-446655440001",
                        name = "Bezirk-B",
                        anzeigeName = "Bezirk B - Südwest",
                        isActive = true,
                        sortOrder = 2,
                        anzahlKatasterbezirke = 2,
                        anzahlAntraege = 8
                    }
                },
                currentPage = 1,
                pageSize = 20,
                totalCount = 5,
                totalPages = 1,
                hasNextPage = false,
                hasPreviousPage = false
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else if (methodName.EndsWith("ById"))
        {
            // Single district example
            mediaType.Example = new OpenApiString(JsonSerializer.Serialize(new
            {
                id = "550e8400-e29b-41d4-a716-446655440000",
                name = "Bezirk-A",
                displayName = "Bezirk A - Nordost",
                description = "Nördlicher Bereich des Gartenvereins mit 45 Parzellen",
                isActive = true,
                sortOrder = 1,
                anzeigeName = "Bezirk A - Nordost",
                katasterbezirke = new[]
                {
                    new
                    {
                        id = "123e4567-e89b-12d3-a456-426614174000",
                        name = "Flur 1",
                        gemarkung = "Musterstadt"
                    }
                },
                anzahlAntraege = 12,
                createdAt = "2024-01-15T10:30:00Z",
                updatedAt = "2024-08-05T14:22:00Z",
                createdBy = "admin@kgv.de",
                updatedBy = "sachbearbeiter@kgv.de"
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static void AddParzelleResponseExamples(OpenApiMediaType mediaType, string methodName)
    {
        if (methodName.Contains("GetParzellen") && !methodName.EndsWith("ById"))
        {
            // Paginated list example
            mediaType.Example = new OpenApiString(JsonSerializer.Serialize(new
            {
                data = new[]
                {
                    new
                    {
                        id = "123e4567-e89b-12d3-a456-426614174000",
                        nummer = "A-15",
                        bezirkId = "550e8400-e29b-41d4-a716-446655440000",
                        bezirkName = "Bezirk-A",
                        flaeche = 350.5m,
                        status = "Available",
                        statusBeschreibung = "Verfügbar",
                        preis = 125.00m,
                        hasWasser = true,
                        hasStrom = true,
                        prioritaet = 1,
                        fullDisplayName = "Bezirk A - Parzelle A-15",
                        isAvailableForAssignment = true
                    }
                },
                currentPage = 1,
                pageSize = 20,
                totalCount = 145,
                totalPages = 8,
                hasNextPage = true,
                hasPreviousPage = false
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else if (methodName.EndsWith("ById"))
        {
            // Single plot example
            mediaType.Example = new OpenApiString(JsonSerializer.Serialize(new
            {
                id = "123e4567-e89b-12d3-a456-426614174000",
                nummer = "A-15",
                bezirkId = "550e8400-e29b-41d4-a716-446655440000",
                bezirk = new
                {
                    id = "550e8400-e29b-41d4-a716-446655440000",
                    name = "Bezirk-A",
                    anzeigeName = "Bezirk A - Nordost"
                },
                flaeche = 350.5m,
                status = "Available",
                statusBeschreibung = "Verfügbar",
                preis = 125.00m,
                beschreibung = "Sonnige Parzelle mit Obstbäumen",
                besonderheiten = "Apfelbaum, Kirschbaum, Gartenlaube",
                hasWasser = true,
                hasStrom = true,
                prioritaet = 1,
                fullDisplayName = "Bezirk A - Parzelle A-15",
                isAvailableForAssignment = true,
                annualCost = 125.00m,
                createdAt = "2024-01-15T10:30:00Z",
                updatedAt = "2024-08-05T14:22:00Z",
                createdBy = "admin@kgv.de",
                updatedBy = "sachbearbeiter@kgv.de"
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}