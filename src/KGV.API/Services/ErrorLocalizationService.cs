using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace KGV.API.Services;

/// <summary>
/// Service for localizing error messages and creating standardized error responses
/// </summary>
public interface IErrorLocalizationService
{
    /// <summary>
    /// Creates a localized ProblemDetails object
    /// </summary>
    /// <param name="title">Error title key</param>
    /// <param name="detail">Error detail key or message</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="instance">Request instance path</param>
    /// <param name="parameters">Parameters for string formatting</param>
    /// <returns>Localized ProblemDetails</returns>
    ProblemDetails CreateLocalizedProblemDetails(string title, string? detail, int statusCode, string? instance = null, params object[] parameters);

    /// <summary>
    /// Creates a localized ValidationProblemDetails object
    /// </summary>
    /// <param name="modelState">Model state with validation errors</param>
    /// <param name="instance">Request instance path</param>
    /// <returns>Localized ValidationProblemDetails</returns>
    ValidationProblemDetails CreateLocalizedValidationProblemDetails(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, string? instance = null);

    /// <summary>
    /// Gets a localized error message
    /// </summary>
    /// <param name="key">Localization key</param>
    /// <param name="parameters">Parameters for string formatting</param>
    /// <returns>Localized message</returns>
    string GetLocalizedMessage(string key, params object[] parameters);

    /// <summary>
    /// Gets a localized business rule error message
    /// </summary>
    /// <param name="businessRuleType">Type of business rule</param>
    /// <param name="context">Context information</param>
    /// <returns>Localized business rule error message</returns>
    string GetBusinessRuleErrorMessage(string businessRuleType, object? context = null);
}

/// <summary>
/// Implementation of error localization service
/// </summary>
public class ErrorLocalizationService : IErrorLocalizationService
{
    private readonly IStringLocalizer<ErrorLocalizationService> _localizer;
    private readonly ILogger<ErrorLocalizationService> _logger;

    public ErrorLocalizationService(
        IStringLocalizer<ErrorLocalizationService> localizer,
        ILogger<ErrorLocalizationService> logger)
    {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ProblemDetails CreateLocalizedProblemDetails(string title, string? detail, int statusCode, string? instance = null, params object[] parameters)
    {
        var localizedTitle = GetLocalizedMessage(title, parameters);
        var localizedDetail = string.IsNullOrEmpty(detail) ? null : GetLocalizedMessage(detail, parameters);

        var problemDetails = new ProblemDetails
        {
            Title = localizedTitle,
            Detail = localizedDetail,
            Status = statusCode,
            Instance = instance,
            Type = GetProblemTypeUri(statusCode)
        };

        // Add additional properties for better debugging
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        problemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id;

        // Add localization information
        problemDetails.Extensions["locale"] = System.Globalization.CultureInfo.CurrentCulture.Name;

        return problemDetails;
    }

    public ValidationProblemDetails CreateLocalizedValidationProblemDetails(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, string? instance = null)
    {
        var localizedErrors = new Dictionary<string, string[]>();

        foreach (var kvp in modelState)
        {
            var errors = kvp.Value?.Errors?.Select(e => 
                string.IsNullOrEmpty(e.ErrorMessage) 
                    ? GetLocalizedMessage("Validation.Generic") 
                    : GetLocalizedMessage(e.ErrorMessage))
                .ToArray() ?? Array.Empty<string>();

            if (errors.Any())
            {
                localizedErrors[kvp.Key] = errors;
            }
        }

        var validationProblemDetails = new ValidationProblemDetails(localizedErrors)
        {
            Title = GetLocalizedMessage("Validation.Title"),
            Status = 422,
            Instance = instance,
            Type = GetProblemTypeUri(422)
        };

        // Add additional properties
        validationProblemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        validationProblemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id;
        validationProblemDetails.Extensions["locale"] = System.Globalization.CultureInfo.CurrentCulture.Name;

        return validationProblemDetails;
    }

    public string GetLocalizedMessage(string key, params object[] parameters)
    {
        try
        {
            var localizedString = _localizer[key, parameters];
            
            // If localization key is not found, return the key itself as fallback
            if (localizedString.ResourceNotFound)
            {
                _logger.LogWarning("Localization key '{Key}' not found. Using key as fallback.", key);
                return parameters.Any() ? string.Format(key, parameters) : key;
            }

            return localizedString.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error localizing message with key '{Key}'", key);
            return parameters.Any() ? string.Format(key, parameters) : key;
        }
    }

    public string GetBusinessRuleErrorMessage(string businessRuleType, object? context = null)
    {
        var contextInfo = context != null ? JsonSerializer.Serialize(context) : "{}";
        
        return businessRuleType switch
        {
            "BezirkNotFound" => GetLocalizedMessage("BusinessRule.BezirkNotFound"),
            "BezirkHasApplications" => GetLocalizedMessage("BusinessRule.BezirkHasApplications"),
            "BezirkNameExists" => GetLocalizedMessage("BusinessRule.BezirkNameExists"),
            "ParzelleNotFound" => GetLocalizedMessage("BusinessRule.ParzelleNotFound"),
            "ParzelleNotAvailable" => GetLocalizedMessage("BusinessRule.ParzelleNotAvailable"),
            "ParzelleAlreadyAssigned" => GetLocalizedMessage("BusinessRule.ParzelleAlreadyAssigned"),
            "ParzelleNumberExists" => GetLocalizedMessage("BusinessRule.ParzelleNumberExists", context),
            "AntragNotFound" => GetLocalizedMessage("BusinessRule.AntragNotFound"),
            "AntragStatusInvalid" => GetLocalizedMessage("BusinessRule.AntragStatusInvalid"),
            "InsufficientPermissions" => GetLocalizedMessage("BusinessRule.InsufficientPermissions"),
            "RateLimitExceeded" => GetLocalizedMessage("BusinessRule.RateLimitExceeded"),
            "InvalidDateRange" => GetLocalizedMessage("BusinessRule.InvalidDateRange"),
            "RequiredFieldMissing" => GetLocalizedMessage("BusinessRule.RequiredFieldMissing", context),
            "InvalidFormat" => GetLocalizedMessage("BusinessRule.InvalidFormat", context),
            "ValueOutOfRange" => GetLocalizedMessage("BusinessRule.ValueOutOfRange", context),
            _ => GetLocalizedMessage("BusinessRule.Generic", businessRuleType)
        };
    }

    private static string GetProblemTypeUri(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
            429 => "https://tools.ietf.org/html/rfc6585#section-4",
            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            _ => "https://tools.ietf.org/html/rfc7231"
        };
    }
}

/// <summary>
/// Static class containing error message keys for localization
/// </summary>
public static class ErrorKeys
{
    public const string BadRequest = "Error.BadRequest";
    public const string Unauthorized = "Error.Unauthorized";
    public const string Forbidden = "Error.Forbidden";
    public const string NotFound = "Error.NotFound";
    public const string Conflict = "Error.Conflict";
    public const string UnprocessableEntity = "Error.UnprocessableEntity";
    public const string TooManyRequests = "Error.TooManyRequests";
    public const string InternalServerError = "Error.InternalServerError";

    public static class Validation
    {
        public const string Title = "Validation.Title";
        public const string Generic = "Validation.Generic";
        public const string Required = "Validation.Required";
        public const string Range = "Validation.Range";
        public const string StringLength = "Validation.StringLength";
        public const string Email = "Validation.Email";
        public const string RegularExpression = "Validation.RegularExpression";
    }

    public static class BusinessRule
    {
        public const string BezirkNotFound = "BusinessRule.BezirkNotFound";
        public const string BezirkHasApplications = "BusinessRule.BezirkHasApplications";
        public const string BezirkNameExists = "BusinessRule.BezirkNameExists";
        public const string ParzelleNotFound = "BusinessRule.ParzelleNotFound";
        public const string ParzelleNotAvailable = "BusinessRule.ParzelleNotAvailable";
        public const string ParzelleAlreadyAssigned = "BusinessRule.ParzelleAlreadyAssigned";
        public const string ParzelleNumberExists = "BusinessRule.ParzelleNumberExists";
        public const string AntragNotFound = "BusinessRule.AntragNotFound";
        public const string AntragStatusInvalid = "BusinessRule.AntragStatusInvalid";
        public const string InsufficientPermissions = "BusinessRule.InsufficientPermissions";
        public const string RateLimitExceeded = "BusinessRule.RateLimitExceeded";
        public const string InvalidDateRange = "BusinessRule.InvalidDateRange";
        public const string RequiredFieldMissing = "BusinessRule.RequiredFieldMissing";
        public const string InvalidFormat = "BusinessRule.InvalidFormat";
        public const string ValueOutOfRange = "BusinessRule.ValueOutOfRange";
        public const string Generic = "BusinessRule.Generic";
    }

    public static class Operations
    {
        public const string Create = "Operation.Create";
        public const string Update = "Operation.Update";
        public const string Delete = "Operation.Delete";
        public const string Get = "Operation.Get";
        public const string Search = "Operation.Search";
        public const string Assign = "Operation.Assign";
    }
}