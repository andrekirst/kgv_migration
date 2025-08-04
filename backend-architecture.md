# KGV Management System - Backend Architecture Specification

## Project Overview

Comprehensive backend architecture for modernizing the KGV (Kleingartenverein) management application "Frank" from legacy Windows Forms to modern .NET 9 Web API with PostgreSQL.

## Architecture Overview

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    API Layer                            │
│  Controllers, Middleware, Authentication, Swagger      │
├─────────────────────────────────────────────────────────┤
│                Application Layer                        │
│  Use Cases, Commands/Queries, DTOs, Services          │
├─────────────────────────────────────────────────────────┤
│                  Domain Layer                          │
│  Entities, Value Objects, Domain Services, Events     │
├─────────────────────────────────────────────────────────┤
│               Infrastructure Layer                      │
│  EF Core, Repositories, External Services, Logging    │
└─────────────────────────────────────────────────────────┘
```

## Project Structure

```
KGV.Management.Api/
├── src/
│   ├── KGV.Management.Api/                 # API Layer
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Extensions/
│   │   └── Program.cs
│   ├── KGV.Management.Application/         # Application Layer
│   │   ├── Common/
│   │   ├── Features/
│   │   │   ├── Applications/
│   │   │   ├── FileReferences/
│   │   │   ├── Districts/
│   │   │   ├── Personnel/
│   │   │   └── History/
│   │   ├── Interfaces/
│   │   └── Services/
│   ├── KGV.Management.Domain/              # Domain Layer
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Enums/
│   │   ├── Events/
│   │   └── Interfaces/
│   └── KGV.Management.Infrastructure/      # Infrastructure Layer
│       ├── Data/
│       ├── Repositories/
│       ├── Services/
│       └── Migrations/
└── tests/
    ├── KGV.Management.Api.Tests/
    ├── KGV.Management.Application.Tests/
    ├── KGV.Management.Domain.Tests/
    └── KGV.Management.Infrastructure.Tests/
```

## Domain Models

### Core Entities

#### 1. Application (Antrag)
```csharp
public class Application : BaseEntity
{
    public Guid Id { get; private set; }
    public string FileReference { get; private set; }
    public string WaitingListNumber32 { get; private set; }
    public string WaitingListNumber33 { get; private set; }
    
    // Primary Applicant
    public PersonalInfo PrimaryApplicant { get; private set; }
    
    // Secondary Applicant (optional)
    public PersonalInfo? SecondaryApplicant { get; private set; }
    
    // Contact Information
    public ContactInfo Contact { get; private set; }
    
    // Application Details
    public DateTime ApplicationDate { get; private set; }
    public DateTime? ConfirmationDate { get; private set; }
    public DateTime? CurrentOfferDate { get; private set; }
    public DateTime? DeletionDate { get; private set; }
    
    public string? Wishes { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }
    
    // Navigation Properties
    public List<HistoryEntry> History { get; private set; } = new();
    
    public ApplicationStatus Status => GetCurrentStatus();
}
```

#### 2. FileReference (Aktenzeichen)
```csharp
public class FileReference : BaseEntity
{
    public Guid Id { get; private set; }
    public string District { get; private set; }
    public int Number { get; private set; }
    public int Year { get; private set; }
    
    public string FullReference => $"{District}-{Number:D4}/{Year}";
    
    public static FileReference Generate(string district, int year);
}
```

#### 3. District (Bezirk)
```csharp
public class District : BaseEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    
    // Navigation Properties
    public List<CadastralDistrict> CadastralDistricts { get; private set; } = new();
}
```

#### 4. CadastralDistrict (Katasterbezirk)
```csharp
public class CadastralDistrict : BaseEntity
{
    public Guid Id { get; private set; }
    public Guid DistrictId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    
    // Navigation Properties
    public District District { get; private set; }
}
```

#### 5. Personnel (Personen)
```csharp
public class Personnel : BaseEntity
{
    public Guid Id { get; private set; }
    public PersonalInfo PersonalInfo { get; private set; }
    public string PersonnelNumber { get; private set; }
    public string OrganizationalUnit { get; private set; }
    public string Room { get; private set; }
    public ContactInfo Contact { get; private set; }
    public string DictationSign { get; private set; }
    public string Signature { get; private set; }
    public string JobTitle { get; private set; }
    public Guid? GroupId { get; private set; }
    
    // Permissions
    public PermissionSet Permissions { get; private set; }
    public bool IsActive { get; private set; }
}
```

#### 6. HistoryEntry (Verlauf)
```csharp
public class HistoryEntry : BaseEntity
{
    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public HistoryEntryType Type { get; private set; }
    public DateTime Date { get; private set; }
    
    // Plot Information
    public string? Gemarkung { get; private set; }
    public string? Flur { get; private set; }
    public string? Parzelle { get; private set; }
    public string? Size { get; private set; }
    
    public string? CaseWorker { get; private set; }
    public string? Note { get; private set; }
    public string? Comment { get; private set; }
    
    // Navigation Properties
    public Application Application { get; private set; }
}
```

### Value Objects

#### PersonalInfo
```csharp
public record PersonalInfo(
    string Salutation,
    string? Title,
    string FirstName,
    string LastName,
    string? Birthday
)
{
    public string FullName => $"{FirstName} {LastName}";
    public string FormalName => string.IsNullOrEmpty(Title) 
        ? $"{Salutation} {LastName}" 
        : $"{Salutation} {Title} {LastName}";
}
```

#### ContactInfo
```csharp
public record ContactInfo(
    Address Address,
    string? Phone,
    string? MobilePhone,
    string? BusinessPhone,
    string? Email
);

public record Address(
    string Street,
    string PostalCode,
    string City
)
{
    public string FullAddress => $"{Street}, {PostalCode} {City}";
}
```

#### PermissionSet
```csharp
public record PermissionSet(
    bool IsAdmin,
    bool CanAccessAdministration,
    bool CanAccessServiceGroups,
    bool CanAccessPriorityAndSLA,
    bool CanAccessCustomers
);
```

### Enums

```csharp
public enum ApplicationStatus
{
    Active,
    Pending,
    Offered,
    Accepted,
    Rejected,
    Inactive,
    Deleted
}

public enum HistoryEntryType
{
    Created,
    Modified,
    Offered,
    Accepted,
    Rejected,
    Deleted,
    Reactivated
}
```

## Repository Pattern & Entity Framework Core

### Base Repository Interface
```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        string? includeString = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);
    
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
}
```

### Specific Repository Interfaces
```csharp
public interface IApplicationRepository : IRepository<Application>
{
    Task<Application?> GetByFileReferenceAsync(string fileReference, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Application>> GetActiveApplicationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Application>> GetByWaitingListAsync(string district, CancellationToken cancellationToken = default);
    Task<int> GetNextWaitingListNumberAsync(string district, CancellationToken cancellationToken = default);
}

public interface IFileReferenceRepository : IRepository<FileReference>
{
    Task<FileReference> GenerateNextAsync(string district, int year, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string district, int number, int year, CancellationToken cancellationToken = default);
}

public interface IHistoryRepository : IRepository<HistoryEntry>
{
    Task<IReadOnlyList<HistoryEntry>> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task AddHistoryEntryAsync(Guid applicationId, HistoryEntryType type, string? comment = null, CancellationToken cancellationToken = default);
}
```

### Unit of Work Pattern
```csharp
public interface IUnitOfWork : IDisposable
{
    IApplicationRepository Applications { get; }
    IFileReferenceRepository FileReferences { get; }
    IDistrictRepository Districts { get; }
    ICadastralDistrictRepository CadastralDistricts { get; }
    IPersonnelRepository Personnel { get; }
    IHistoryRepository History { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

## REST API Endpoints

### Application Management
```
GET    /api/v1/applications                    # Get all applications (with filtering/paging)
GET    /api/v1/applications/{id}               # Get application by ID
POST   /api/v1/applications                    # Create new application
PUT    /api/v1/applications/{id}               # Update application
DELETE /api/v1/applications/{id}               # Delete application
PATCH  /api/v1/applications/{id}/status        # Update application status
GET    /api/v1/applications/{id}/history       # Get application history
POST   /api/v1/applications/{id}/history       # Add history entry
GET    /api/v1/applications/waiting-list       # Get waiting list (by district)
POST   /api/v1/applications/{id}/offer         # Create offer for application
```

### File Reference Management
```
GET    /api/v1/file-references                 # Get all file references
POST   /api/v1/file-references/generate        # Generate new file reference
GET    /api/v1/file-references/next/{district} # Get next available number
```

### District & Cadastral Management
```
GET    /api/v1/districts                       # Get all districts
GET    /api/v1/districts/{id}                  # Get district by ID
POST   /api/v1/districts                       # Create district
PUT    /api/v1/districts/{id}                  # Update district
DELETE /api/v1/districts/{id}                  # Delete district
GET    /api/v1/districts/{id}/cadastral        # Get cadastral districts for district
```

### Personnel Management
```
GET    /api/v1/personnel                       # Get all personnel
GET    /api/v1/personnel/{id}                  # Get personnel by ID
POST   /api/v1/personnel                       # Create personnel
PUT    /api/v1/personnel/{id}                  # Update personnel
DELETE /api/v1/personnel/{id}                  # Delete personnel
PATCH  /api/v1/personnel/{id}/permissions      # Update permissions
```

### Authentication & Authorization
```
POST   /api/v1/auth/login                      # Login
POST   /api/v1/auth/logout                     # Logout
POST   /api/v1/auth/refresh                    # Refresh token
GET    /api/v1/auth/me                         # Get current user info
```

### Reports & Documents
```
GET    /api/v1/reports/waiting-list/{district} # Generate waiting list report
GET    /api/v1/reports/applications/summary    # Application summary report
POST   /api/v1/documents/generate              # Generate document (PDF)
```

## Authentication & Authorization Strategy

### JWT-Based Authentication
```csharp
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
```

### Role-Based Authorization
```csharp
public static class Roles
{
    public const string Administrator = "Administrator";
    public const string CaseWorker = "CaseWorker";
    public const string Viewer = "Viewer";
}

public static class Permissions
{
    public const string ViewApplications = "applications:view";
    public const string CreateApplications = "applications:create";
    public const string UpdateApplications = "applications:update";
    public const string DeleteApplications = "applications:delete";
    public const string ManagePersonnel = "personnel:manage";
    public const string ViewReports = "reports:view";
    public const string GenerateDocuments = "documents:generate";
}

// Usage in controllers
[Authorize(Policy = Permissions.ViewApplications)]
[HttpGet]
public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetApplicationsAsync()
{
    // Implementation
}
```

## PostgreSQL Migration Strategy

### Migration from SQL Server to PostgreSQL

#### 1. Data Type Mappings
```sql
-- SQL Server -> PostgreSQL
uniqueidentifier -> UUID
varchar(n) -> VARCHAR(n)
char(1) -> CHAR(1)
int -> INTEGER
datetime -> TIMESTAMP
```

#### 2. PostgreSQL Schema
```sql
-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create tables with proper constraints and indexes
CREATE TABLE applications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    file_reference VARCHAR(20),
    waiting_list_number_32 VARCHAR(20),
    waiting_list_number_33 VARCHAR(20),
    -- ... other fields
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX idx_applications_file_reference ON applications(file_reference);
CREATE INDEX idx_applications_active ON applications(is_active) WHERE is_active = true;
CREATE INDEX idx_applications_application_date ON applications(application_date);
```

#### 3. Migration Script Structure
```csharp
public class InitialMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create all tables
        // Add indexes
        // Add foreign key constraints
        // Seed initial data
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop tables in reverse order
    }
}
```

## Background Services & Automated Processes

### Background Service Examples
```csharp
public class WaitingListRankingService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await UpdateWaitingListRankingsAsync();
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
    
    private async Task UpdateWaitingListRankingsAsync()
    {
        // Complex business logic for ranking calculation
        // Based on Gemarkungen (cadastral areas)
    }
}

public class DocumentCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupOldDocumentsAsync();
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
```

## Error Handling Strategy

### Global Exception Handling
```csharp
public class GlobalExceptionHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            NotFoundException => new ErrorResponse(StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => new ErrorResponse(StatusCodes.Status400BadRequest, "Validation failed"),
            UnauthorizedException => new ErrorResponse(StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => new ErrorResponse(StatusCodes.Status500InternalServerError, "Internal server error")
        };
        
        context.Response.StatusCode = response.StatusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### Custom Exceptions
```csharp
public class KgvException : Exception
{
    public string ErrorCode { get; }
    
    public KgvException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class ApplicationNotFoundException : KgvException
{
    public ApplicationNotFoundException(Guid id) 
        : base("APPLICATION_NOT_FOUND", $"Application with ID {id} was not found")
    {
    }
}
```

## Performance & Scalability Considerations

### Database Optimization
```csharp
// Configure Entity Framework for performance
services.AddDbContext<KgvDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
    });
    
    // Enable connection pooling
    options.EnableServiceProviderCaching();
    options.EnableSensitiveDataLogging(false);
});

// Use read replicas for queries
services.AddDbContext<KgvReadOnlyDbContext>(options =>
{
    options.UseNpgsql(readOnlyConnectionString);
});
```

### Caching Strategy
```csharp
// Distributed caching for session data
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
});

// Memory caching for reference data
services.AddMemoryCache(options =>
{
    options.SizeLimit = 100_000;
});

// Cache implementation
public class CachedDistrictService : IDistrictService
{
    public async Task<IEnumerable<District>> GetAllAsync()
    {
        return await _cache.GetOrCreateAsync("districts", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await _districtService.GetAllAsync();
        });
    }
}
```

### API Rate Limiting
```csharp
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
});
```

## Technology Stack Summary

### Core Technologies
- **.NET 9** - Latest LTS framework
- **ASP.NET Core Web API** - REST API framework
- **PostgreSQL 16** - Primary database
- **Entity Framework Core 9** - ORM
- **Redis** - Distributed caching and session storage

### Authentication & Security
- **JWT Bearer Tokens** - Stateless authentication
- **BCrypt** - Password hashing
- **HTTPS Enforcement** - TLS 1.3
- **CORS Policy** - Cross-origin resource sharing

### Monitoring & Logging
- **Serilog** - Structured logging
- **Application Insights** - Telemetry and monitoring
- **Health Checks** - Endpoint monitoring

### Development & Testing
- **Swagger/OpenAPI** - API documentation
- **xUnit** - Unit testing framework
- **FluentAssertions** - Test assertions
- **Testcontainers** - Integration testing with PostgreSQL

### Deployment
- **Docker** - Containerization
- **Docker Compose** - Local development
- **Kubernetes** - Production orchestration (optional)

## Next Steps

1. Set up the project structure and base classes
2. Implement Entity Framework Core configuration and migrations
3. Create repository implementations with PostgreSQL
4. Develop the REST API controllers with proper validation
5. Implement JWT authentication and authorization
6. Add comprehensive unit and integration tests
7. Set up CI/CD pipeline with automated testing
8. Create data migration scripts from SQL Server to PostgreSQL
9. Implement monitoring and logging infrastructure
10. Deploy to staging environment for testing

This architecture provides a solid foundation for modernizing the KGV management system while maintaining all existing functionality and preparing for future scalability needs.