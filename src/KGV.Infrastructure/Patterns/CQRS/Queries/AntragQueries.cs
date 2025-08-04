using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using KGV.Infrastructure.Patterns.AntiCorruption.ModernModels;

namespace KGV.Infrastructure.Patterns.CQRS.Queries
{
    /// <summary>
    /// Queries for Application (Antrag) entity operations
    /// Following CQRS pattern for read operations with optimized data access
    /// </summary>

    #region Get Application Queries

    public class GetApplicationByIdQuery : BaseQuery<ApplicationDetailDto>
    {
        [Required]
        public Guid ApplicationId { get; set; }
        
        public bool IncludeHistory { get; set; } = false;
        public bool IncludeMetadata { get; set; } = false;
    }

    public class GetApplicationByIdQueryHandler : IQueryHandler<GetApplicationByIdQuery, ApplicationDetailDto>
    {
        private readonly IApplicationReadRepository _readRepository;
        private readonly ILogger<GetApplicationByIdQueryHandler> _logger;

        public GetApplicationByIdQueryHandler(
            IApplicationReadRepository readRepository,
            ILogger<GetApplicationByIdQueryHandler> logger)
        {
            _readRepository = readRepository;
            _logger = logger;
        }

        public async Task<ApplicationDetailDto> HandleAsync(
            GetApplicationByIdQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving application {ApplicationId}", query.ApplicationId);

                var application = await _readRepository.GetByIdAsync(query.ApplicationId, query.IncludeHistory);
                
                if (application == null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found", query.ApplicationId);
                    return null;
                }

                var dto = MapToDetailDto(application, query.IncludeHistory, query.IncludeMetadata);

                _logger.LogDebug("Application {ApplicationId} retrieved successfully", query.ApplicationId);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve application {ApplicationId}", query.ApplicationId);
                throw;
            }
        }

        private ApplicationDetailDto MapToDetailDto(Application application, bool includeHistory, bool includeMetadata)
        {
            var dto = new ApplicationDetailDto
            {
                Id = application.Id,
                FileReference = application.FileReference,
                WaitingListNumber32 = application.WaitingListNumber32,
                WaitingListNumber33 = application.WaitingListNumber33,
                Status = application.Status.ToString(),
                Preferences = application.Preferences,
                Notes = application.Notes,
                CreatedAt = application.CreatedAt,
                UpdatedAt = application.UpdatedAt,
                DeactivatedAt = application.DeactivatedAt,
                
                PrimaryContact = MapContactDto(application.PrimaryContact),
                SecondaryContact = MapContactDto(application.SecondaryContact),
                Address = MapAddressDto(application.Address),
                Dates = MapApplicationDatesDto(application.Dates)
            };

            if (includeHistory && application.History?.Any() == true)
            {
                dto.History = application.History.Select(MapHistoryDto).ToList();
            }

            if (includeMetadata)
            {
                dto.Metadata = new ApplicationMetadataDto
                {
                    HasSecondaryContact = application.SecondaryContact != null,
                    DaysSinceCreation = (DateTime.UtcNow - application.CreatedAt).Days,
                    DaysSinceLastUpdate = application.UpdatedAt.HasValue 
                        ? (DateTime.UtcNow - application.UpdatedAt.Value).Days 
                        : null,
                    HistoryEntryCount = application.History?.Count ?? 0
                };
            }

            return dto;
        }

        private ContactDto MapContactDto(Contact contact)
        {
            if (contact == null) return null;

            return new ContactDto
            {
                Salutation = contact.Salutation,
                Title = contact.Title,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                FullName = contact.FullName,
                DateOfBirth = contact.DateOfBirth,
                Phone = contact.ContactDetails?.Phone,
                Mobile = contact.ContactDetails?.Mobile,
                BusinessPhone = contact.ContactDetails?.BusinessPhone,
                Email = contact.ContactDetails?.Email,
                FormalSalutation = contact.ContactDetails?.FormalSalutation
            };
        }

        private AddressDto MapAddressDto(Address address)
        {
            if (address == null) return null;

            return new AddressDto
            {
                Street = address.Street,
                PostalCode = address.PostalCode,
                City = address.City,
                FullAddress = address.FullAddress
            };
        }

        private ApplicationDatesDto MapApplicationDatesDto(ApplicationDates dates)
        {
            if (dates == null) return null;

            return new ApplicationDatesDto
            {
                ApplicationDate = dates.ApplicationDate,
                ConfirmationDate = dates.ConfirmationDate,
                CurrentOfferDate = dates.CurrentOfferDate,
                DeletionDate = dates.DeletionDate
            };
        }

        private ApplicationHistoryDto MapHistoryDto(ApplicationHistory history)
        {
            return new ApplicationHistoryDto
            {
                Id = history.Id,
                Type = history.Type,
                Date = history.Date,
                CaseWorker = history.CaseWorker,
                Note = history.Note,
                Comment = history.Comment,
                PlotInfo = history.PlotInfo != null ? new PlotInfoDto
                {
                    Gemarkung = history.PlotInfo.Gemarkung,
                    Flur = history.PlotInfo.Flur,
                    Parzelle = history.PlotInfo.Parzelle,
                    Size = history.PlotInfo.Size,
                    FullDescription = history.PlotInfo.FullDescription
                } : null
            };
        }
    }

    #endregion

    #region Search Applications Query

    public class SearchApplicationsQuery : BaseQuery<PagedResultDto<ApplicationSummaryDto>>
    {
        public string SearchTerm { get; set; }
        public string Status { get; set; }
        public string District { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? UpdatedFrom { get; set; }
        public DateTime? UpdatedTo { get; set; }
        
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
    }

    public class SearchApplicationsQueryHandler : IQueryHandler<SearchApplicationsQuery, PagedResultDto<ApplicationSummaryDto>>
    {
        private readonly IApplicationReadRepository _readRepository;
        private readonly ILogger<SearchApplicationsQueryHandler> _logger;

        public SearchApplicationsQueryHandler(
            IApplicationReadRepository readRepository,
            ILogger<SearchApplicationsQueryHandler> logger)
        {
            _readRepository = readRepository;
            _logger = logger;
        }

        public async Task<PagedResultDto<ApplicationSummaryDto>> HandleAsync(
            SearchApplicationsQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Searching applications with term '{SearchTerm}', page {Page}, size {PageSize}",
                    query.SearchTerm, query.Page, query.PageSize);

                var searchCriteria = new ApplicationSearchCriteria
                {
                    SearchTerm = query.SearchTerm,
                    Status = query.Status,
                    District = query.District,
                    CreatedFrom = query.CreatedFrom,
                    CreatedTo = query.CreatedTo,
                    UpdatedFrom = query.UpdatedFrom,
                    UpdatedTo = query.UpdatedTo,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    SortBy = query.SortBy,
                    SortDescending = query.SortDescending
                };

                var result = await _readRepository.SearchApplicationsAsync(searchCriteria);

                var dto = new PagedResultDto<ApplicationSummaryDto>
                {
                    Items = result.Items.Select(MapToSummaryDto).ToList(),
                    TotalCount = result.TotalCount,
                    Page = result.Page,
                    PageSize = result.PageSize,
                    TotalPages = result.TotalPages
                };

                _logger.LogDebug("Found {Count} applications (page {Page} of {TotalPages})",
                    dto.Items.Count, dto.Page, dto.TotalPages);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search applications");
                throw;
            }
        }

        private ApplicationSummaryDto MapToSummaryDto(Application application)
        {
            return new ApplicationSummaryDto
            {
                Id = application.Id,
                FileReference = application.FileReference,
                Status = application.Status.ToString(),
                PrimaryContactName = application.PrimaryContact?.FullName,
                Address = application.Address?.FullAddress,
                CreatedAt = application.CreatedAt,
                UpdatedAt = application.UpdatedAt
            };
        }
    }

    #endregion

    #region Get Application Statistics Query

    public class GetApplicationStatisticsQuery : BaseQuery<ApplicationStatisticsDto>
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string District { get; set; }
    }

    public class GetApplicationStatisticsQueryHandler : IQueryHandler<GetApplicationStatisticsQuery, ApplicationStatisticsDto>
    {
        private readonly IApplicationReadRepository _readRepository;
        private readonly ILogger<GetApplicationStatisticsQueryHandler> _logger;

        public GetApplicationStatisticsQueryHandler(
            IApplicationReadRepository readRepository,
            ILogger<GetApplicationStatisticsQueryHandler> logger)
        {
            _readRepository = readRepository;
            _logger = logger;
        }

        public async Task<ApplicationStatisticsDto> HandleAsync(
            GetApplicationStatisticsQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving application statistics");

                var statistics = await _readRepository.GetStatisticsAsync(
                    query.FromDate, 
                    query.ToDate, 
                    query.District);

                var dto = new ApplicationStatisticsDto
                {
                    TotalApplications = statistics.TotalApplications,
                    StatusCounts = statistics.StatusCounts,
                    ApplicationsByMonth = statistics.ApplicationsByMonth,
                    AverageProcessingDays = statistics.AverageProcessingDays,
                    PendingApplications = statistics.PendingApplications,
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogDebug("Application statistics retrieved successfully");

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve application statistics");
                throw;
            }
        }
    }

    #endregion

    #region Get Applications by Status Query

    public class GetApplicationsByStatusQuery : BaseQuery<IEnumerable<ApplicationSummaryDto>>
    {
        [Required]
        public ApplicationStatus Status { get; set; }
        
        public int? Limit { get; set; }
        public DateTime? CreatedSince { get; set; }
    }

    public class GetApplicationsByStatusQueryHandler : IQueryHandler<GetApplicationsByStatusQuery, IEnumerable<ApplicationSummaryDto>>
    {
        private readonly IApplicationReadRepository _readRepository;
        private readonly ILogger<GetApplicationsByStatusQueryHandler> _logger;

        public GetApplicationsByStatusQueryHandler(
            IApplicationReadRepository readRepository,
            ILogger<GetApplicationsByStatusQueryHandler> logger)
        {
            _readRepository = readRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<ApplicationSummaryDto>> HandleAsync(
            GetApplicationsByStatusQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving applications with status {Status}", query.Status);

                var applications = await _readRepository.GetByStatusAsync(
                    query.Status, 
                    query.Limit, 
                    query.CreatedSince);

                var dtos = applications.Select(MapToSummaryDto).ToList();

                _logger.LogDebug("Retrieved {Count} applications with status {Status}", 
                    dtos.Count, query.Status);

                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve applications by status {Status}", query.Status);
                throw;
            }
        }

        private ApplicationSummaryDto MapToSummaryDto(Application application)
        {
            return new ApplicationSummaryDto
            {
                Id = application.Id,
                FileReference = application.FileReference,
                Status = application.Status.ToString(),
                PrimaryContactName = application.PrimaryContact?.FullName,
                Address = application.Address?.FullAddress,
                CreatedAt = application.CreatedAt,
                UpdatedAt = application.UpdatedAt
            };
        }
    }

    #endregion

    #region Data Transfer Objects

    public class ApplicationDetailDto
    {
        public Guid Id { get; set; }
        public string FileReference { get; set; }
        public string WaitingListNumber32 { get; set; }
        public string WaitingListNumber33 { get; set; }
        public string Status { get; set; }
        public string Preferences { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeactivatedAt { get; set; }
        
        public ContactDto PrimaryContact { get; set; }
        public ContactDto SecondaryContact { get; set; }
        public AddressDto Address { get; set; }
        public ApplicationDatesDto Dates { get; set; }
        public List<ApplicationHistoryDto> History { get; set; }
        public ApplicationMetadataDto Metadata { get; set; }
    }

    public class ApplicationSummaryDto
    {
        public Guid Id { get; set; }
        public string FileReference { get; set; }
        public string Status { get; set; }
        public string PrimaryContactName { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ContactDto
    {
        public string Salutation { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string BusinessPhone { get; set; }
        public string Email { get; set; }
        public string FormalSalutation { get; set; }
    }

    public class AddressDto
    {
        public string Street { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string FullAddress { get; set; }
    }

    public class ApplicationDatesDto
    {
        public DateTime? ApplicationDate { get; set; }
        public DateTime? ConfirmationDate { get; set; }
        public DateTime? CurrentOfferDate { get; set; }
        public DateTime? DeletionDate { get; set; }
    }

    public class ApplicationHistoryDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public string CaseWorker { get; set; }
        public string Note { get; set; }
        public string Comment { get; set; }
        public PlotInfoDto PlotInfo { get; set; }
    }

    public class PlotInfoDto
    {
        public string Gemarkung { get; set; }
        public string Flur { get; set; }
        public string Parzelle { get; set; }
        public string Size { get; set; }
        public string FullDescription { get; set; }
    }

    public class ApplicationMetadataDto
    {
        public bool HasSecondaryContact { get; set; }
        public int DaysSinceCreation { get; set; }
        public int? DaysSinceLastUpdate { get; set; }
        public int HistoryEntryCount { get; set; }
    }

    public class ApplicationStatisticsDto
    {
        public int TotalApplications { get; set; }
        public Dictionary<string, int> StatusCounts { get; set; }
        public Dictionary<string, int> ApplicationsByMonth { get; set; }
        public double AverageProcessingDays { get; set; }
        public int PendingApplications { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    #endregion

    #region Repository Interfaces and Models (placeholders)

    public interface IApplicationReadRepository
    {
        Task<Application> GetByIdAsync(Guid id, bool includeHistory = false);
        Task<PagedResult<Application>> SearchApplicationsAsync(ApplicationSearchCriteria criteria);
        Task<ApplicationStatistics> GetStatisticsAsync(DateTime? fromDate, DateTime? toDate, string district);
        Task<IEnumerable<Application>> GetByStatusAsync(ApplicationStatus status, int? limit, DateTime? createdSince);
    }

    public class ApplicationSearchCriteria
    {
        public string SearchTerm { get; set; }
        public string Status { get; set; }
        public string District { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? UpdatedFrom { get; set; }
        public DateTime? UpdatedTo { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string SortBy { get; set; }
        public bool SortDescending { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class ApplicationStatistics
    {
        public int TotalApplications { get; set; }
        public Dictionary<string, int> StatusCounts { get; set; }
        public Dictionary<string, int> ApplicationsByMonth { get; set; }
        public double AverageProcessingDays { get; set; }
        public int PendingApplications { get; set; }
    }

    #endregion
}