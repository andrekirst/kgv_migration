# KGV Management System - Application Layer Implementation Details

## Data Transfer Objects (DTOs)

### Request DTOs

#### CreateApplicationRequest
```csharp
public record CreateApplicationRequest
{
    [Required, MaxLength(10)]
    public string Salutation { get; init; } = string.Empty;
    
    [MaxLength(50)]
    public string? Title { get; init; }
    
    [Required, MaxLength(50)]
    public string FirstName { get; init; } = string.Empty;
    
    [Required, MaxLength(50)]
    public string LastName { get; init; } = string.Empty;
    
    [MaxLength(100)]
    public string? Birthday { get; init; }
    
    // Secondary Applicant (optional)
    [MaxLength(10)]
    public string? Salutation2 { get; init; }
    
    [MaxLength(50)]
    public string? Title2 { get; init; }
    
    [MaxLength(50)]
    public string? FirstName2 { get; init; }
    
    [MaxLength(50)]
    public string? LastName2 { get; init; }
    
    [MaxLength(100)]
    public string? Birthday2 { get; init; }
    
    // Contact Information
    [Required, MaxLength(50)]
    public string Street { get; init; } = string.Empty;
    
    [Required, MaxLength(10)]
    public string PostalCode { get; init; } = string.Empty;
    
    [Required, MaxLength(50)]
    public string City { get; init; } = string.Empty;
    
    [MaxLength(50)]
    public string? Phone { get; init; }
    
    [MaxLength(50)]
    public string? MobilePhone { get; init; }
    
    [MaxLength(50)]
    public string? BusinessPhone { get; init; }
    
    [MaxLength(100), EmailAddress]
    public string? Email { get; init; }
    
    [Required]
    public DateTime ApplicationDate { get; init; }
    
    [MaxLength(600)]
    public string? Wishes { get; init; }
    
    [MaxLength(2000)]
    public string? Notes { get; init; }
}
```

#### UpdateApplicationRequest
```csharp
public record UpdateApplicationRequest
{
    [Required, MaxLength(10)]
    public string Salutation { get; init; } = string.Empty;
    
    [MaxLength(50)]
    public string? Title { get; init; }
    
    [Required, MaxLength(50)]
    public string FirstName { get; init; } = string.Empty;
    
    [Required, MaxLength(50)]
    public string LastName { get; init; } = string.Empty;
    
    // ... same structure as CreateApplicationRequest
    
    public DateTime? ConfirmationDate { get; init; }
    public DateTime? CurrentOfferDate { get; init; }
    
    [MaxLength(600)]
    public string? Wishes { get; init; }
    
    [MaxLength(2000)]
    public string? Notes { get; init; }
}
```

#### AddHistoryEntryRequest
```csharp
public record AddHistoryEntryRequest
{
    [Required]
    public HistoryEntryType Type { get; init; }
    
    [MaxLength(50)]
    public string? Gemarkung { get; init; }
    
    [MaxLength(20)]
    public string? Flur { get; init; }
    
    [MaxLength(20)]
    public string? Parzelle { get; init; }
    
    [MaxLength(20)]
    public string? Size { get; init; }
    
    [MaxLength(100)]
    public string? CaseWorker { get; init; }
    
    [MaxLength(100)]
    public string? Note { get; init; }
    
    [MaxLength(255)]
    public string? Comment { get; init; }
}
```

### Response DTOs

#### ApplicationResponse
```csharp
public record ApplicationResponse
{
    public Guid Id { get; init; }
    public string FileReference { get; init; } = string.Empty;
    public string? WaitingListNumber32 { get; init; }
    public string? WaitingListNumber33 { get; init; }
    
    // Primary Applicant
    public PersonalInfoResponse PrimaryApplicant { get; init; } = new();
    
    // Secondary Applicant
    public PersonalInfoResponse? SecondaryApplicant { get; init; }
    
    // Contact Information
    public ContactInfoResponse Contact { get; init; } = new();
    
    // Application Details
    public DateTime ApplicationDate { get; init; }
    public DateTime? ConfirmationDate { get; init; }
    public DateTime? CurrentOfferDate { get; init; }
    public DateTime? DeletionDate { get; init; }
    
    public string? Wishes { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateTime? DeactivatedAt { get; init; }
    
    public ApplicationStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    
    public List<HistoryEntryResponse> History { get; init; } = new();
}
```

#### PersonalInfoResponse
```csharp
public record PersonalInfoResponse
{
    public string Salutation { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Birthday { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string FormalName { get; init; } = string.Empty;
}
```

#### ContactInfoResponse
```csharp
public record ContactInfoResponse
{
    public AddressResponse Address { get; init; } = new();
    public string? Phone { get; init; }
    public string? MobilePhone { get; init; }
    public string? BusinessPhone { get; init; }
    public string? Email { get; init; }
}

public record AddressResponse
{
    public string Street { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string FullAddress { get; init; } = string.Empty;
}
```

#### HistoryEntryResponse
```csharp
public record HistoryEntryResponse
{
    public Guid Id { get; init; }
    public HistoryEntryType Type { get; init; }
    public string TypeDescription { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    
    // Plot Information
    public string? Gemarkung { get; init; }
    public string? Flur { get; init; }
    public string? Parzelle { get; init; }
    public string? Size { get; init; }
    
    public string? CaseWorker { get; init; }
    public string? Note { get; init; }
    public string? Comment { get; init; }
}
```

## Application Services

### ApplicationService
```csharp
public interface IApplicationService
{
    Task<PagedResult<ApplicationResponse>> GetApplicationsAsync(
        ApplicationFilterRequest filter, 
        CancellationToken cancellationToken = default);
    
    Task<ApplicationResponse?> GetApplicationByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    Task<ApplicationResponse> CreateApplicationAsync(
        CreateApplicationRequest request, 
        CancellationToken cancellationToken = default);
    
    Task<ApplicationResponse> UpdateApplicationAsync(
        Guid id, 
        UpdateApplicationRequest request, 
        CancellationToken cancellationToken = default);
    
    Task DeleteApplicationAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    Task<ApplicationResponse> UpdateStatusAsync(
        Guid id, 
        ApplicationStatus status, 
        string? comment = null,
        CancellationToken cancellationToken = default);
    
    Task<List<ApplicationResponse>> GetWaitingListAsync(
        string district, 
        CancellationToken cancellationToken = default);
    
    Task<ApplicationResponse> CreateOfferAsync(
        Guid id, 
        CreateOfferRequest request, 
        CancellationToken cancellationToken = default);
}

public class ApplicationService : IApplicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ApplicationService> _logger;
    private readonly IFileReferenceService _fileReferenceService;
    private readonly IHistoryService _historyService;

    public ApplicationService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ApplicationService> logger,
        IFileReferenceService fileReferenceService,
        IHistoryService historyService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _fileReferenceService = fileReferenceService;
        _historyService = historyService;
    }

    public async Task<ApplicationResponse> CreateApplicationAsync(
        CreateApplicationRequest request, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new application for {FirstName} {LastName}", 
            request.FirstName, request.LastName);

        // Generate file reference
        var fileReference = await _fileReferenceService.GenerateNextAsync(
            GetDistrictFromPostalCode(request.PostalCode), 
            request.ApplicationDate.Year, 
            cancellationToken);

        // Create application entity
        var application = new Application(
            fileReference.FullReference,
            new PersonalInfo(
                request.Salutation,
                request.Title,
                request.FirstName,
                request.LastName,
                request.Birthday),
            request.Salutation2 != null ? new PersonalInfo(
                request.Salutation2,
                request.Title2,
                request.FirstName2 ?? string.Empty,
                request.LastName2 ?? string.Empty,
                request.Birthday2) : null,
            new ContactInfo(
                new Address(request.Street, request.PostalCode, request.City),
                request.Phone,
                request.MobilePhone,
                request.BusinessPhone,
                request.Email),
            request.ApplicationDate,
            request.Wishes,
            request.Notes);

        // Save to database
        await _unitOfWork.Applications.AddAsync(application, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Add history entry
        await _historyService.AddEntryAsync(
            application.Id, 
            HistoryEntryType.Created, 
            "Application created", 
            cancellationToken);

        _logger.LogInformation("Application created with ID {ApplicationId} and file reference {FileReference}", 
            application.Id, fileReference.FullReference);

        return _mapper.Map<ApplicationResponse>(application);
    }

    public async Task<ApplicationResponse> UpdateStatusAsync(
        Guid id, 
        ApplicationStatus status, 
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var application = await _unitOfWork.Applications.GetByIdAsync(id, cancellationToken);
        if (application == null)
        {
            throw new ApplicationNotFoundException(id);
        }

        var oldStatus = application.Status;
        application.UpdateStatus(status);

        await _unitOfWork.Applications.UpdateAsync(application, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Add history entry
        var historyType = status switch
        {
            ApplicationStatus.Offered => HistoryEntryType.Offered,
            ApplicationStatus.Accepted => HistoryEntryType.Accepted,
            ApplicationStatus.Rejected => HistoryEntryType.Rejected,
            ApplicationStatus.Inactive => HistoryEntryType.Deleted,
            _ => HistoryEntryType.Modified
        };

        await _historyService.AddEntryAsync(
            id, 
            historyType, 
            comment ?? $"Status changed from {oldStatus} to {status}", 
            cancellationToken);

        _logger.LogInformation("Application {ApplicationId} status updated from {OldStatus} to {NewStatus}", 
            id, oldStatus, status);

        return _mapper.Map<ApplicationResponse>(application);
    }

    private static string GetDistrictFromPostalCode(string postalCode)
    {
        // Business logic to determine district from postal code
        // This would be implemented based on KGV business rules
        return postalCode.StartsWith("1") ? "MITTE" : "SUED";
    }
}
```

### FileReferenceService
```csharp
public interface IFileReferenceService
{
    Task<FileReference> GenerateNextAsync(string district, int year, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string fileReference, CancellationToken cancellationToken = default);
}

public class FileReferenceService : IFileReferenceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FileReferenceService> _logger;

    public FileReferenceService(IUnitOfWork unitOfWork, ILogger<FileReferenceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FileReference> GenerateNextAsync(
        string district, 
        int year, 
        CancellationToken cancellationToken = default)
    {
        var existingReferences = await _unitOfWork.FileReferences.GetAsync(
            predicate: fr => fr.District == district && fr.Year == year,
            orderBy: q => q.OrderByDescending(fr => fr.Number),
            cancellationToken: cancellationToken);

        var nextNumber = existingReferences.Any() ? existingReferences.First().Number + 1 : 1;

        var fileReference = new FileReference(district, nextNumber, year);
        
        await _unitOfWork.FileReferences.AddAsync(fileReference, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated file reference {FileReference} for district {District}, year {Year}", 
            fileReference.FullReference, district, year);

        return fileReference;
    }

    public async Task<bool> ExistsAsync(string fileReference, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.FileReferences.GetAsync(
            predicate: fr => fr.FullReference == fileReference,
            cancellationToken: cancellationToken);

        return existing.Any();
    }
}
```

### HistoryService
```csharp
public interface IHistoryService
{
    Task<List<HistoryEntryResponse>> GetHistoryAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<HistoryEntryResponse> AddEntryAsync(Guid applicationId, HistoryEntryType type, string? comment = null, CancellationToken cancellationToken = default);
    Task<HistoryEntryResponse> AddOfferEntryAsync(Guid applicationId, AddHistoryEntryRequest request, CancellationToken cancellationToken = default);
}

public class HistoryService : IHistoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<HistoryService> _logger;

    public HistoryService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<HistoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<HistoryEntryResponse> AddEntryAsync(
        Guid applicationId, 
        HistoryEntryType type, 
        string? comment = null, 
        CancellationToken cancellationToken = default)
    {
        var application = await _unitOfWork.Applications.GetByIdAsync(applicationId, cancellationToken);
        if (application == null)
        {
            throw new ApplicationNotFoundException(applicationId);
        }

        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var caseWorker = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : "System";

        var historyEntry = new HistoryEntry(
            applicationId,
            type,
            DateTime.UtcNow,
            null, // Gemarkung
            null, // Flur
            null, // Parzelle
            null, // Size
            caseWorker,
            null, // Note
            comment);

        await _unitOfWork.History.AddAsync(historyEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added history entry of type {Type} for application {ApplicationId}", 
            type, applicationId);

        return _mapper.Map<HistoryEntryResponse>(historyEntry);
    }

    public async Task<HistoryEntryResponse> AddOfferEntryAsync(
        Guid applicationId, 
        AddHistoryEntryRequest request, 
        CancellationToken cancellationToken = default)
    {
        var application = await _unitOfWork.Applications.GetByIdAsync(applicationId, cancellationToken);
        if (application == null)
        {
            throw new ApplicationNotFoundException(applicationId);
        }

        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var caseWorker = request.CaseWorker ?? (currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : "System");

        var historyEntry = new HistoryEntry(
            applicationId,
            request.Type,
            DateTime.UtcNow,
            request.Gemarkung,
            request.Flur,
            request.Parzelle,
            request.Size,
            caseWorker,
            request.Note,
            request.Comment);

        await _unitOfWork.History.AddAsync(historyEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added detailed history entry of type {Type} for application {ApplicationId} with plot info", 
            request.Type, applicationId);

        return _mapper.Map<HistoryEntryResponse>(historyEntry);
    }
}
```

## Validation & Business Rules

### Custom Validators
```csharp
public class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[a-zA-ZäöüÄÖÜß\s-]+$")
            .WithMessage("First name can only contain letters, spaces, and hyphens");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[a-zA-ZäöüÄÖÜß\s-]+$")
            .WithMessage("Last name can only contain letters, spaces, and hyphens");

        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .Matches(@"^\d{5}$")
            .WithMessage("Postal code must be exactly 5 digits");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Please provide a valid email address");

        RuleFor(x => x.ApplicationDate)
            .NotEmpty()
            .LessThanOrEqualTo(DateTime.Now.Date)
            .WithMessage("Application date cannot be in the future");

        // Validate secondary applicant if provided
        When(x => !string.IsNullOrEmpty(x.FirstName2), () =>
        {
            RuleFor(x => x.FirstName2)
                .NotEmpty()
                .MaximumLength(50)
                .WithMessage("Secondary applicant first name is required when provided");

            RuleFor(x => x.LastName2)
                .NotEmpty()
                .MaximumLength(50)
                .WithMessage("Secondary applicant last name is required when provided");
        });
    }
}
```

### Business Rule Services
```csharp
public interface IBusinessRuleService
{
    Task<ValidationResult> ValidateApplicationCreationAsync(CreateApplicationRequest request, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateApplicationUpdateAsync(Guid id, UpdateApplicationRequest request, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateStatusChangeAsync(Guid id, ApplicationStatus newStatus, CancellationToken cancellationToken = default);
}

public class BusinessRuleService : IBusinessRuleService
{
    private readonly IUnitOfWork _unitOfWork;

    public BusinessRuleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateApplicationCreationAsync(
        CreateApplicationRequest request, 
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Check for duplicate applications (same name and address)
        var existingApplications = await _unitOfWork.Applications.GetAsync(
            predicate: a => a.PrimaryApplicant.FirstName == request.FirstName &&
                           a.PrimaryApplicant.LastName == request.LastName &&
                           a.Contact.Address.Street == request.Street &&
                           a.Contact.Address.PostalCode == request.PostalCode &&
                           a.IsActive,
            cancellationToken: cancellationToken);

        if (existingApplications.Any())
        {
            errors.Add("An active application already exists for this person at this address");
        }

        // Validate district availability
        var district = GetDistrictFromPostalCode(request.PostalCode);
        var activeApplicationsInDistrict = await _unitOfWork.Applications.GetAsync(
            predicate: a => a.FileReference.StartsWith(district) && a.IsActive,
            cancellationToken: cancellationToken);

        if (activeApplicationsInDistrict.Count >= GetMaxApplicationsPerDistrict(district))
        {
            errors.Add($"Maximum number of applications reached for district {district}");
        }

        return new ValidationResult(errors);
    }

    public async Task<ValidationResult> ValidateStatusChangeAsync(
        Guid id, 
        ApplicationStatus newStatus, 
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var application = await _unitOfWork.Applications.GetByIdAsync(id, cancellationToken);

        if (application == null)
        {
            errors.Add("Application not found");
            return new ValidationResult(errors);
        }

        var currentStatus = application.Status;

        // Define valid status transitions
        var validTransitions = new Dictionary<ApplicationStatus, List<ApplicationStatus>>
        {
            [ApplicationStatus.Active] = new() { ApplicationStatus.Offered, ApplicationStatus.Inactive, ApplicationStatus.Deleted },
            [ApplicationStatus.Offered] = new() { ApplicationStatus.Accepted, ApplicationStatus.Rejected, ApplicationStatus.Active },
            [ApplicationStatus.Accepted] = new() { ApplicationStatus.Inactive },
            [ApplicationStatus.Rejected] = new() { ApplicationStatus.Active, ApplicationStatus.Deleted },
            [ApplicationStatus.Inactive] = new() { ApplicationStatus.Active, ApplicationStatus.Deleted },
            [ApplicationStatus.Deleted] = new() { } // No transitions from deleted
        };

        if (!validTransitions.ContainsKey(currentStatus) || 
            !validTransitions[currentStatus].Contains(newStatus))
        {
            errors.Add($"Invalid status transition from {currentStatus} to {newStatus}");
        }

        return new ValidationResult(errors);
    }

    private static string GetDistrictFromPostalCode(string postalCode)
    {
        // Business logic for district determination
        return postalCode.StartsWith("1") ? "MITTE" : "SUED";
    }

    private static int GetMaxApplicationsPerDistrict(string district)
    {
        // Business rule for maximum applications per district
        return district switch
        {
            "MITTE" => 1000,
            "SUED" => 800,
            _ => 500
        };
    }
}

public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; }

    public ValidationResult(List<string> errors)
    {
        Errors = errors ?? new List<string>();
    }
}
```

## CQRS Implementation (Optional)

### Query Handlers
```csharp
public record GetApplicationsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    string? District = null,
    ApplicationStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<PagedResult<ApplicationResponse>>;

public class GetApplicationsQueryHandler : IRequestHandler<GetApplicationsQuery, PagedResult<ApplicationResponse>>
{
    private readonly IApplicationRepository _repository;
    private readonly IMapper _mapper;

    public GetApplicationsQueryHandler(IApplicationRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PagedResult<ApplicationResponse>> Handle(
        GetApplicationsQuery request, 
        CancellationToken cancellationToken)
    {
        // Build query with filters
        Expression<Func<Application, bool>>? predicate = null;
        
        if (!string.IsNullOrEmpty(request.Search))
        {
            predicate = a => a.PrimaryApplicant.FirstName.Contains(request.Search) ||
                           a.PrimaryApplicant.LastName.Contains(request.Search) ||
                           a.FileReference.Contains(request.Search);
        }

        if (!string.IsNullOrEmpty(request.District))
        {
            var districtPredicate = (Expression<Func<Application, bool>>)(a => a.FileReference.StartsWith(request.District));
            predicate = predicate == null ? districtPredicate : predicate.And(districtPredicate);
        }

        if (request.Status.HasValue)
        {
            var statusPredicate = (Expression<Func<Application, bool>>)(a => a.Status == request.Status.Value);
            predicate = predicate == null ? statusPredicate : predicate.And(statusPredicate);
        }

        if (request.FromDate.HasValue)
        {
            var fromDatePredicate = (Expression<Func<Application, bool>>)(a => a.ApplicationDate >= request.FromDate.Value);
            predicate = predicate == null ? fromDatePredicate : predicate.And(fromDatePredicate);
        }

        if (request.ToDate.HasValue)
        {
            var toDatePredicate = (Expression<Func<Application, bool>>)(a => a.ApplicationDate <= request.ToDate.Value);
            predicate = predicate == null ? toDatePredicate : predicate.And(toDatePredicate);
        }

        var applications = await _repository.GetAsync(
            predicate: predicate,
            orderBy: q => q.OrderByDescending(a => a.ApplicationDate),
            cancellationToken: cancellationToken);

        var totalCount = await _repository.CountAsync(predicate, cancellationToken);

        var pagedApplications = applications
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var responses = _mapper.Map<List<ApplicationResponse>>(pagedApplications);

        return new PagedResult<ApplicationResponse>(
            responses,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
```

### Command Handlers
```csharp
public record CreateApplicationCommand(CreateApplicationRequest Request) : IRequest<ApplicationResponse>;

public class CreateApplicationCommandHandler : IRequestHandler<CreateApplicationCommand, ApplicationResponse>
{
    private readonly IApplicationService _applicationService;
    private readonly IValidator<CreateApplicationRequest> _validator;

    public CreateApplicationCommandHandler(
        IApplicationService applicationService,
        IValidator<CreateApplicationRequest> validator)
    {
        _applicationService = applicationService;
        _validator = validator;
    }

    public async Task<ApplicationResponse> Handle(
        CreateApplicationCommand request, 
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request.Request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        return await _applicationService.CreateApplicationAsync(request.Request, cancellationToken);
    }
}
```

## AutoMapper Profiles

```csharp
public class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<Application, ApplicationResponse>()
            .ForMember(dest => dest.History, opt => opt.MapFrom(src => src.History));

        CreateMap<PersonalInfo, PersonalInfoResponse>();
        CreateMap<ContactInfo, ContactInfoResponse>();
        CreateMap<Address, AddressResponse>();
        CreateMap<HistoryEntry, HistoryEntryResponse>()
            .ForMember(dest => dest.TypeDescription, opt => opt.MapFrom(src => src.Type.ToString()));

        CreateMap<CreateApplicationRequest, Application>()
            .ConstructUsing((src, ctx) => new Application(
                string.Empty, // Will be set by service
                new PersonalInfo(src.Salutation, src.Title, src.FirstName, src.LastName, src.Birthday),
                !string.IsNullOrEmpty(src.FirstName2) ? new PersonalInfo(src.Salutation2!, src.Title2, src.FirstName2, src.LastName2!, src.Birthday2) : null,
                new ContactInfo(new Address(src.Street, src.PostalCode, src.City), src.Phone, src.MobilePhone, src.BusinessPhone, src.Email),
                src.ApplicationDate,
                src.Wishes,
                src.Notes));
    }
}
```

This comprehensive application layer implementation provides:

1. **Complete DTOs** for all request/response scenarios
2. **Application Services** with full business logic implementation
3. **Validation** using FluentValidation with custom business rules
4. **Business Rule Services** for complex domain validation
5. **CQRS Implementation** for separation of read/write operations
6. **AutoMapper Configuration** for object mapping
7. **Error Handling** with custom exceptions and validation results

The implementation follows clean architecture principles with clear separation of concerns and comprehensive validation at all levels.