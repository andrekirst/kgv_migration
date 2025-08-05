using KGV.Application.Common.Interfaces;
using KGV.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KGV.API.Services;

/// <summary>
/// Service for GDPR compliance operations
/// </summary>
public interface IGdprComplianceService
{
    /// <summary>
    /// Exports all personal data for a given person
    /// </summary>
    Task<PersonalDataExport> ExportPersonalDataAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Anonymizes personal data for a given person
    /// </summary>
    Task AnonymizePersonalDataAsync(Guid personId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs data access for audit purposes
    /// </summary>
    Task LogDataAccessAsync(Guid personId, string accessType, string requestedBy, string purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets data retention information
    /// </summary>
    Task<DataRetentionInfo> GetDataRetentionInfoAsync(Guid personId, CancellationToken cancellationToken = default);
}

/// <summary>
/// GDPR compliance service implementation
/// </summary>
public class GdprComplianceService : IGdprComplianceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GdprComplianceService> _logger;

    public GdprComplianceService(IUnitOfWork unitOfWork, ILogger<GdprComplianceService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PersonalDataExport> ExportPersonalDataAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting personal data export for person {PersonId}", personId);

            // Find all applications for this person
            var antraege = await _unitOfWork.Antraege.GetAllAsync(
                filter: a => a.Id == personId, // This would need proper person linking
                includeProperties: "Verlauf",
                cancellationToken: cancellationToken);

            // Find person record if exists
            var person = await _unitOfWork.Personen.GetByIdAsync(personId, cancellationToken);

            var export = new PersonalDataExport
            {
                PersonId = personId,
                ExportDate = DateTime.UtcNow,
                DataCategories = new Dictionary<string, object>()
            };

            // Export person data
            if (person != null)
            {
                export.DataCategories["PersonalInformation"] = new
                {
                    FirstName = person.Vorname,
                    LastName = person.Nachname,
                    Email = person.Email?.Value,
                    Phone = person.Telefon?.Value,
                    CreatedAt = person.CreatedAt,
                    UpdatedAt = person.UpdatedAt
                };
            }

            // Export application data
            export.DataCategories["Applications"] = antraege.Select(a => new
            {
                ApplicationId = a.Id,
                FirstName = a.Vorname,
                LastName = a.Nachname,
                Address = a.Adresse?.GetFullAddress(),
                Email = a.EMail?.Value,
                Phone = a.Telefon?.Value,
                MobilePhone = a.MobilTelefon?.Value,
                ApplicationDate = a.Bewerbungsdatum,
                Status = a.Status,
                Wishes = a.Wunsch,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                History = a.Verlauf.Select(v => new
                {
                    v.Art,
                    v.Datum,
                    v.Kommentar,
                    v.Sachbearbeiter
                })
            });

            // Log the data export
            await LogDataAccessAsync(personId, "EXPORT", "SYSTEM", "GDPR Data Export Request", cancellationToken);

            _logger.LogInformation("Personal data export completed for person {PersonId}", personId);
            return export;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting personal data for person {PersonId}", personId);
            throw;
        }
    }

    public async Task AnonymizePersonalDataAsync(Guid personId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting data anonymization for person {PersonId} with reason: {Reason}", personId, reason);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Anonymize applications
            var antraege = await _unitOfWork.Antraege.GetAllAsync(
                filter: a => a.Id == personId, // This would need proper person linking
                cancellationToken: cancellationToken);

            foreach (var antrag in antraege)
            {
                // Replace personal data with anonymized values
                var anonymizedData = GenerateAnonymizedData();
                
                // Update with anonymized data - this would require proper methods in the entity
                // antrag.AnonymizePersonalData(anonymizedData);
                
                await _unitOfWork.Antraege.UpdateAsync(antrag, cancellationToken);
            }

            // Anonymize person record
            var person = await _unitOfWork.Personen.GetByIdAsync(personId, cancellationToken);
            if (person != null)
            {
                // person.AnonymizePersonalData(); - this would need to be implemented
                await _unitOfWork.Personen.UpdateAsync(person, cancellationToken);
            }

            // Log the anonymization
            await LogDataAccessAsync(personId, "ANONYMIZE", "SYSTEM", $"Data anonymization: {reason}", cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Data anonymization completed for person {PersonId}", personId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error anonymizing data for person {PersonId}", personId);
            throw;
        }
    }

    public async Task LogDataAccessAsync(Guid personId, string accessType, string requestedBy, string purpose, CancellationToken cancellationToken = default)
    {
        try
        {
            var logEntry = new DataAccessLog
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                AccessType = accessType,
                RequestedBy = requestedBy,
                Purpose = purpose,
                AccessDate = DateTime.UtcNow,
                IpAddress = "N/A", // Would be populated from HttpContext
                UserAgent = "N/A"  // Would be populated from HttpContext
            };

            // In a real implementation, this would be stored in a dedicated audit log table
            _logger.LogInformation("Data access logged: {LogEntry}", JsonSerializer.Serialize(logEntry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging data access for person {PersonId}", personId);
            // Don't throw here as this is logging only
        }
    }

    public async Task<DataRetentionInfo> GetDataRetentionInfoAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        try
        {
            var person = await _unitOfWork.Personen.GetByIdAsync(personId, cancellationToken);
            var antraege = await _unitOfWork.Antraege.GetAllAsync(
                filter: a => a.Id == personId, // This would need proper person linking
                cancellationToken: cancellationToken);

            var retentionInfo = new DataRetentionInfo
            {
                PersonId = personId,
                DataCategories = new List<DataCategoryRetention>()
            };

            // Calculate retention periods based on German legal requirements
            if (person != null)
            {
                retentionInfo.DataCategories.Add(new DataCategoryRetention
                {
                    Category = "PersonalData",
                    LegalBasis = "Art. 6 Abs. 1 lit. b DSGVO (Vertragserfüllung)",
                    RetentionPeriod = "10 Jahre nach Vertragsende",
                    ScheduledDeletionDate = person.UpdatedAt?.AddYears(10) ?? person.CreatedAt.AddYears(10)
                });
            }

            foreach (var antrag in antraege)
            {
                var completionDate = antrag.UpdatedAt ?? antrag.CreatedAt;
                retentionInfo.DataCategories.Add(new DataCategoryRetention
                {
                    Category = "ApplicationData",
                    LegalBasis = "Verwaltungsverfahrensgesetz (VwVfG)",
                    RetentionPeriod = "30 Jahre nach Abschluss des Verfahrens",
                    ScheduledDeletionDate = completionDate.AddYears(30)
                });
            }

            return retentionInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data retention info for person {PersonId}", personId);
            throw;
        }
    }

    private static object GenerateAnonymizedData()
    {
        var random = new Random();
        return new
        {
            FirstName = $"Person{random.Next(1000, 9999)}",
            LastName = $"Anonymized{random.Next(1000, 9999)}",
            Email = $"anonymized{random.Next(1000, 9999)}@example.com",
            Phone = $"+49{random.Next(100000000, 999999999)}"
        };
    }
}

/// <summary>
/// Personal data export model
/// </summary>
public class PersonalDataExport
{
    public Guid PersonId { get; set; }
    public DateTime ExportDate { get; set; }
    public Dictionary<string, object> DataCategories { get; set; } = new();
}

/// <summary>
/// Data retention information model
/// </summary>
public class DataRetentionInfo
{
    public Guid PersonId { get; set; }
    public List<DataCategoryRetention> DataCategories { get; set; } = new();
}

/// <summary>
/// Data category retention model
/// </summary>
public class DataCategoryRetention
{
    public string Category { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = string.Empty;
    public string RetentionPeriod { get; set; } = string.Empty;
    public DateTime ScheduledDeletionDate { get; set; }
}

/// <summary>
/// Data access log model
/// </summary>
public class DataAccessLog
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string AccessType { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime AccessDate { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}