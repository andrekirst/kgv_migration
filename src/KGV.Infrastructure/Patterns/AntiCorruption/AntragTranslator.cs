using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using KGV.Infrastructure.Patterns.AntiCorruption.LegacyModels;
using KGV.Infrastructure.Patterns.AntiCorruption.ModernModels;

namespace KGV.Infrastructure.Patterns.AntiCorruption
{
    /// <summary>
    /// Anti-Corruption Layer implementation for Antrag (Application) translation
    /// Handles complex business logic translation between legacy and modern models
    /// </summary>
    public class AntragTranslator : ILegacyDataTranslator<LegacyAntrag, Application>
    {
        private readonly ILogger<AntragTranslator> _logger;
        private readonly TranslationMetrics _metrics;

        public AntragTranslator(ILogger<AntragTranslator> logger, TranslationMetrics metrics)
        {
            _logger = logger;
            _metrics = metrics;
        }

        public async Task<Application> TranslateToModernAsync(LegacyAntrag legacyModel)
        {
            if (legacyModel == null)
            {
                _logger.LogWarning("Attempted to translate null LegacyAntrag");
                return null;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var application = new Application
                {
                    Id = legacyModel.an_ID,
                    FileReference = legacyModel.an_Aktenzeichen,
                    WaitingListNumber32 = legacyModel.an_WartelistenNr32,
                    WaitingListNumber33 = legacyModel.an_WartelistenNr33,
                    Preferences = legacyModel.an_Wunsch,
                    Notes = legacyModel.an_Vermerk,
                    CreatedAt = DateTime.UtcNow, // Legacy doesn't track creation time
                    UpdatedAt = null,
                    DeactivatedAt = legacyModel.an_DeaktiviertAm,
                    
                    PrimaryContact = CreateContact(legacyModel, ContactType.Primary),
                    SecondaryContact = CreateContact(legacyModel, ContactType.Secondary),
                    Address = CreateAddress(legacyModel),
                    Dates = CreateApplicationDates(legacyModel),
                    Status = DetermineApplicationStatus(legacyModel)
                };

                stopwatch.Stop();
                _metrics.RecordTranslationTime("AntragToModern", stopwatch.ElapsedMilliseconds);
                _metrics.IncrementTranslationCount("AntragToModern", "Success");

                _logger.LogDebug("Successfully translated LegacyAntrag {Id} to modern Application", legacyModel.an_ID);
                
                return application;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementTranslationCount("AntragToModern", "Error");
                _logger.LogError(ex, "Failed to translate LegacyAntrag {Id}", legacyModel.an_ID);
                throw;
            }
        }

        public async Task<LegacyAntrag> TranslateToLegacyAsync(Application modernModel)
        {
            if (modernModel == null)
            {
                _logger.LogWarning("Attempted to translate null Application");
                return null;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var legacyAntrag = new LegacyAntrag
                {
                    an_ID = modernModel.Id,
                    an_Aktenzeichen = modernModel.FileReference,
                    an_WartelistenNr32 = modernModel.WaitingListNumber32,
                    an_WartelistenNr33 = modernModel.WaitingListNumber33,
                    an_Wunsch = modernModel.Preferences,
                    an_Vermerk = modernModel.Notes,
                    an_DeaktiviertAm = modernModel.DeactivatedAt,

                    // Primary contact mapping
                    an_Anrede = modernModel.PrimaryContact?.Salutation,
                    an_Titel = modernModel.PrimaryContact?.Title,
                    an_Vorname = modernModel.PrimaryContact?.FirstName,
                    an_Nachname = modernModel.PrimaryContact?.LastName,
                    an_Geburtstag = FormatDateForLegacy(modernModel.PrimaryContact?.DateOfBirth),
                    an_Telefon = modernModel.PrimaryContact?.ContactDetails?.Phone,
                    an_MobilTelefon = modernModel.PrimaryContact?.ContactDetails?.Mobile,
                    an_GeschTelefon = modernModel.PrimaryContact?.ContactDetails?.BusinessPhone,
                    an_EMail = modernModel.PrimaryContact?.ContactDetails?.Email,
                    an_Briefanrede = modernModel.PrimaryContact?.ContactDetails?.FormalSalutation,

                    // Secondary contact mapping
                    an_Anrede2 = modernModel.SecondaryContact?.Salutation,
                    an_Titel2 = modernModel.SecondaryContact?.Title,
                    an_Vorname2 = modernModel.SecondaryContact?.FirstName,
                    an_Nachname2 = modernModel.SecondaryContact?.LastName,
                    an_Geburtstag2 = FormatDateForLegacy(modernModel.SecondaryContact?.DateOfBirth),
                    an_MobilTelefon2 = modernModel.SecondaryContact?.ContactDetails?.Mobile,

                    // Address mapping
                    an_Strasse = modernModel.Address?.Street,
                    an_PLZ = modernModel.Address?.PostalCode,
                    an_Ort = modernModel.Address?.City,

                    // Dates mapping
                    an_Bewerbungsdatum = modernModel.Dates?.ApplicationDate,
                    an_Bestaetigungsdatum = modernModel.Dates?.ConfirmationDate,
                    an_AktuellesAngebot = modernModel.Dates?.CurrentOfferDate,
                    an_Loeschdatum = modernModel.Dates?.DeletionDate,

                    // Status mapping
                    an_Aktiv = MapApplicationStatusToLegacy(modernModel.Status)
                };

                stopwatch.Stop();
                _metrics.RecordTranslationTime("AntragToLegacy", stopwatch.ElapsedMilliseconds);
                _metrics.IncrementTranslationCount("AntragToLegacy", "Success");

                _logger.LogDebug("Successfully translated Application {Id} to LegacyAntrag", modernModel.Id);
                
                return legacyAntrag;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementTranslationCount("AntragToLegacy", "Error");
                _logger.LogError(ex, "Failed to translate Application {Id}", modernModel.Id);
                throw;
            }
        }

        public async Task<IEnumerable<Application>> TranslateBatchToModernAsync(IEnumerable<LegacyAntrag> legacyModels)
        {
            if (legacyModels == null)
                return Enumerable.Empty<Application>();

            var legacyList = legacyModels.ToList();
            _logger.LogInformation("Starting batch translation of {Count} legacy applications", legacyList.Count);

            var results = new List<Application>();
            var errors = 0;

            foreach (var legacy in legacyList)
            {
                try
                {
                    var modern = await TranslateToModernAsync(legacy);
                    if (modern != null)
                        results.Add(modern);
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Failed to translate legacy application {Id} in batch", legacy.an_ID);
                }
            }

            _logger.LogInformation("Batch translation completed: {Success} successful, {Errors} errors", 
                results.Count, errors);

            return results;
        }

        public async Task<bool> ValidateTranslationAsync(LegacyAntrag legacy, Application modern)
        {
            if (legacy == null || modern == null)
                return false;

            var validationErrors = new List<string>();

            // Core field validation
            if (legacy.an_ID != modern.Id)
                validationErrors.Add($"ID mismatch: {legacy.an_ID} != {modern.Id}");

            if (legacy.an_Aktenzeichen != modern.FileReference)
                validationErrors.Add($"FileReference mismatch: {legacy.an_Aktenzeichen} != {modern.FileReference}");

            // Contact validation
            ValidateContact(legacy, modern.PrimaryContact, ContactType.Primary, validationErrors);
            ValidateContact(legacy, modern.SecondaryContact, ContactType.Secondary, validationErrors);

            // Address validation
            ValidateAddress(legacy, modern.Address, validationErrors);

            // Dates validation
            ValidateDates(legacy, modern.Dates, validationErrors);

            if (validationErrors.Any())
            {
                _logger.LogWarning("Translation validation failed for {Id}: {Errors}", 
                    legacy.an_ID, string.Join("; ", validationErrors));
                return false;
            }

            _logger.LogDebug("Translation validation successful for {Id}", legacy.an_ID);
            return true;
        }

        #region Private Helper Methods

        private Contact CreateContact(LegacyAntrag legacy, ContactType type)
        {
            if (type == ContactType.Primary)
            {
                if (string.IsNullOrEmpty(legacy.an_Vorname) && string.IsNullOrEmpty(legacy.an_Nachname))
                    return null;

                return new Contact
                {
                    Type = ContactType.Primary,
                    Salutation = legacy.an_Anrede,
                    Title = legacy.an_Titel,
                    FirstName = legacy.an_Vorname,
                    LastName = legacy.an_Nachname,
                    DateOfBirth = ParseLegacyDate(legacy.an_Geburtstag),
                    ContactDetails = new ContactDetails
                    {
                        Phone = legacy.an_Telefon,
                        Mobile = legacy.an_MobilTelefon,
                        BusinessPhone = legacy.an_GeschTelefon,
                        Email = legacy.an_EMail,
                        FormalSalutation = legacy.an_Briefanrede
                    }
                };
            }
            else // Secondary
            {
                if (string.IsNullOrEmpty(legacy.an_Vorname2) && string.IsNullOrEmpty(legacy.an_Nachname2))
                    return null;

                return new Contact
                {
                    Type = ContactType.Secondary,
                    Salutation = legacy.an_Anrede2,
                    Title = legacy.an_Titel2,
                    FirstName = legacy.an_Vorname2,
                    LastName = legacy.an_Nachname2,
                    DateOfBirth = ParseLegacyDate(legacy.an_Geburtstag2),
                    ContactDetails = new ContactDetails
                    {
                        Mobile = legacy.an_MobilTelefon2
                    }
                };
            }
        }

        private Address CreateAddress(LegacyAntrag legacy)
        {
            if (string.IsNullOrEmpty(legacy.an_Strasse) && 
                string.IsNullOrEmpty(legacy.an_PLZ) && 
                string.IsNullOrEmpty(legacy.an_Ort))
                return null;

            return new Address
            {
                Street = legacy.an_Strasse,
                PostalCode = legacy.an_PLZ,
                City = legacy.an_Ort
            };
        }

        private ApplicationDates CreateApplicationDates(LegacyAntrag legacy)
        {
            return new ApplicationDates
            {
                ApplicationDate = legacy.an_Bewerbungsdatum,
                ConfirmationDate = legacy.an_Bestaetigungsdatum,
                CurrentOfferDate = legacy.an_AktuellesAngebot,
                DeletionDate = legacy.an_Loeschdatum
            };
        }

        private ApplicationStatus DetermineApplicationStatus(LegacyAntrag legacy)
        {
            if (legacy.an_DeaktiviertAm.HasValue)
                return ApplicationStatus.Archived;

            if (legacy.an_Loeschdatum.HasValue)
                return ApplicationStatus.Cancelled;

            if (legacy.an_Aktiv == '1' || legacy.an_Aktiv == 'Y')
                return ApplicationStatus.Active;

            if (legacy.an_Bestaetigungsdatum.HasValue)
                return ApplicationStatus.Approved;

            if (legacy.an_Bewerbungsdatum.HasValue)
                return ApplicationStatus.Submitted;

            return ApplicationStatus.Draft;
        }

        private char? MapApplicationStatusToLegacy(ApplicationStatus status)
        {
            return status switch
            {
                ApplicationStatus.Active => '1',
                ApplicationStatus.Approved => '1',
                ApplicationStatus.Submitted => '1',
                ApplicationStatus.Draft => '0',
                ApplicationStatus.Cancelled => '0',
                ApplicationStatus.Archived => '0',
                ApplicationStatus.Rejected => '0',
                _ => null
            };
        }

        private DateTime? ParseLegacyDate(string legacyDateString)
        {
            if (string.IsNullOrEmpty(legacyDateString))
                return null;

            // Legacy system stores dates as strings in various formats
            var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "dd/MM/yyyy" };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(legacyDateString, format, CultureInfo.InvariantCulture, 
                    DateTimeStyles.None, out var date))
                {
                    return date;
                }
            }

            // Try general parsing as fallback
            if (DateTime.TryParse(legacyDateString, out var parsedDate))
                return parsedDate;

            _logger.LogWarning("Could not parse legacy date string: {DateString}", legacyDateString);
            return null;
        }

        private string FormatDateForLegacy(DateTime? date)
        {
            return date?.ToString("dd.MM.yyyy");
        }

        private void ValidateContact(LegacyAntrag legacy, Contact modern, ContactType type, List<string> errors)
        {
            if (type == ContactType.Primary)
            {
                if (legacy.an_Vorname != modern?.FirstName)
                    errors.Add($"Primary FirstName mismatch: {legacy.an_Vorname} != {modern?.FirstName}");
                
                if (legacy.an_Nachname != modern?.LastName)
                    errors.Add($"Primary LastName mismatch: {legacy.an_Nachname} != {modern?.LastName}");
            }
            else
            {
                if (legacy.an_Vorname2 != modern?.FirstName)
                    errors.Add($"Secondary FirstName mismatch: {legacy.an_Vorname2} != {modern?.FirstName}");
                
                if (legacy.an_Nachname2 != modern?.LastName)
                    errors.Add($"Secondary LastName mismatch: {legacy.an_Nachname2} != {modern?.LastName}");
            }
        }

        private void ValidateAddress(LegacyAntrag legacy, Address modern, List<string> errors)
        {
            if (legacy.an_Strasse != modern?.Street)
                errors.Add($"Street mismatch: {legacy.an_Strasse} != {modern?.Street}");
            
            if (legacy.an_PLZ != modern?.PostalCode)
                errors.Add($"PostalCode mismatch: {legacy.an_PLZ} != {modern?.PostalCode}");
            
            if (legacy.an_Ort != modern?.City)
                errors.Add($"City mismatch: {legacy.an_Ort} != {modern?.City}");
        }

        private void ValidateDates(LegacyAntrag legacy, ApplicationDates modern, List<string> errors)
        {
            if (legacy.an_Bewerbungsdatum != modern?.ApplicationDate)
                errors.Add($"ApplicationDate mismatch: {legacy.an_Bewerbungsdatum} != {modern?.ApplicationDate}");
            
            if (legacy.an_Bestaetigungsdatum != modern?.ConfirmationDate)
                errors.Add($"ConfirmationDate mismatch: {legacy.an_Bestaetigungsdatum} != {modern?.ConfirmationDate}");
            
            if (legacy.an_AktuellesAngebot != modern?.CurrentOfferDate)
                errors.Add($"CurrentOfferDate mismatch: {legacy.an_AktuellesAngebot} != {modern?.CurrentOfferDate}");
            
            if (legacy.an_Loeschdatum != modern?.DeletionDate)
                errors.Add($"DeletionDate mismatch: {legacy.an_Loeschdatum} != {modern?.DeletionDate}");
        }

        #endregion
    }

    /// <summary>
    /// Metrics collection for translation operations
    /// </summary>
    public class TranslationMetrics
    {
        private readonly ILogger<TranslationMetrics> _logger;

        public TranslationMetrics(ILogger<TranslationMetrics> logger)
        {
            _logger = logger;
        }

        public void RecordTranslationTime(string operation, long milliseconds)
        {
            _logger.LogDebug("Translation operation {Operation} took {Milliseconds}ms", operation, milliseconds);
            // TODO: Send to metrics collector (Prometheus, App Insights, etc.)
        }

        public void IncrementTranslationCount(string operation, string result)
        {
            _logger.LogDebug("Translation operation {Operation} completed with result {Result}", operation, result);
            // TODO: Send to metrics collector
        }
    }
}