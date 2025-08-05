# CQRS Pattern Implementation

This directory contains a complete CQRS (Command Query Responsibility Segregation) pattern implementation for the KGV system.

## Fixed Issues

The following CQRS implementation errors have been resolved:

### 1. Missing Decorator Types (CS0246 errors)
- **Fixed**: All decorator classes were moved from placeholder implementations to fully functional separate files
- **Files Created**:
  - `Decorators/CachingQueryHandlerDecorator.cs` - Implements read-through caching for queries
  - `Decorators/LoggingCommandHandlerDecorator.cs` - Provides detailed logging for commands
  - `Decorators/LoggingQueryHandlerDecorator.cs` - Provides detailed logging for queries
  - `Decorators/RetryCommandHandlerDecorator.cs` - Implements exponential backoff retry logic
  - `Decorators/TransactionCommandHandlerDecorator.cs` - Wraps commands in transaction scopes

### 2. Type Inference Issues (CS0411 errors)
- **Fixed**: Updated `ValidateQuery` method calls in `CqrsMediator.cs` to explicitly specify generic type parameters
- **Change**: `await ValidateQuery(query)` → `await ValidateQuery<TQuery, TResult>(query)`

### 3. Generic Constraint Issues (CS0314 errors)
- **Fixed**: Removed overly restrictive constraint on `IQueryValidator<TQuery>` interface
- **Change**: `where TQuery : IQuery<object>` → removed constraint to allow proper generic type matching

### 4. Missing Dependencies
- **Added**: Required NuGet packages to `KGV.Infrastructure.csproj`:
  - `Microsoft.Extensions.Caching.Memory` (for caching decorator)
  - `Microsoft.Extensions.Options` (for retry configuration)

## Architecture Overview

### Core Components

#### 1. Interfaces (`ICqrsHandler.cs`)
- `ICommand` / `ICommand<TResult>` - Marker interfaces for commands
- `IQuery<TResult>` - Marker interface for queries
- `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResult>` - Command handlers
- `IQueryHandler<TQuery, TResult>` - Query handlers
- `ICqrsMediator` - Central mediator for dispatching commands and queries

#### 2. Mediator (`CqrsMediator.cs`)
- Central dispatcher for commands and queries
- Provides validation, metrics collection, and error handling
- Includes performance monitoring and logging

#### 3. Configuration (`CqrsConfiguration.cs`)
- Automatic registration of handlers, validators, and decorators
- Configurable cross-cutting concerns (caching, logging, retry, transactions)

#### 4. Decorators (`Decorators/`)
- **Caching**: Read-through caching for queries with configurable expiration
- **Logging**: Detailed execution logging with performance metrics
- **Retry**: Exponential backoff retry logic with configurable policies
- **Transaction**: Automatic transaction scope management for commands

### Cross-Cutting Concerns

#### Caching (Queries Only)
```csharp
// Enabled by default, configurable via appsettings.json
"CQRS": {
  "EnableQueryCaching": true
}
```

#### Logging (Commands and Queries)
```csharp
// Enabled by default
"CQRS": {
  "EnableLogging": true
}
```

#### Retry Logic (Commands Only)
```csharp
// Disabled by default
"CQRS": {
  "EnableRetry": false
}
```

#### Transactions (Commands Only)
```csharp
// Enabled by default
"CQRS": {
  "EnableTransactions": true
}
```

## Usage Examples

### Registering CQRS in DI Container
```csharp
services.AddCqrsPattern(configuration);
```

### Creating Commands
```csharp
public class CreateApplicationCommand : BaseCommand<OperationResult<Guid>>
{
    [Required]
    public string FileReference { get; set; }
    // ... other properties
}
```

### Creating Command Handlers
```csharp
public class CreateApplicationCommandHandler : ICommandHandler<CreateApplicationCommand, OperationResult<Guid>>
{
    public async Task<OperationResult<Guid>> HandleAsync(CreateApplicationCommand command, CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

### Creating Queries
```csharp
public class GetApplicationByIdQuery : BaseQuery<ApplicationDetailDto>
{
    [Required]
    public Guid ApplicationId { get; set; }
}
```

### Creating Query Handlers
```csharp
public class GetApplicationByIdQueryHandler : IQueryHandler<GetApplicationByIdQuery, ApplicationDetailDto>
{
    public async Task<ApplicationDetailDto> HandleAsync(GetApplicationByIdQuery query, CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

### Using the Mediator
```csharp
public class ApplicationController : ControllerBase
{
    private readonly ICqrsMediator _mediator;

    public ApplicationController(ICqrsMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateApplication(CreateApplicationCommand command)
    {
        var result = await _mediator.SendAsync(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApplication(Guid id)
    {
        var query = new GetApplicationByIdQuery { ApplicationId = id };
        var result = await _mediator.QueryAsync<GetApplicationByIdQuery, ApplicationDetailDto>(query);
        return Ok(result);
    }
}
```

## Validation

### Command Validation
```csharp
public class CreateApplicationCommandValidator : ICommandValidator<CreateApplicationCommand>
{
    public async Task<ValidationResult> ValidateAsync(CreateApplicationCommand command)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(command.FileReference))
            errors.Add("File reference is required");
            
        return errors.Any() 
            ? ValidationResult.Failure(errors.ToArray()) 
            : ValidationResult.Success();
    }
}
```

## Performance Considerations

- **Caching**: Queries are cached for 5 minutes by default with 2-minute sliding expiration
- **Retry**: Commands can be retried up to 3 times with exponential backoff
- **Transactions**: Commands are wrapped in 1-minute timeout transactions
- **Metrics**: All operations are tracked with execution times and success/failure rates

## Best Practices

1. **Commands should be imperative** (CreateApplication, UpdateApplication)
2. **Queries should be descriptive** (GetApplicationById, SearchApplications)
3. **Use validation for business rules** - implement `ICommandValidator<T>`
4. **Keep handlers focused** - one responsibility per handler
5. **Use DTOs for data transfer** - separate from domain models
6. **Include correlation IDs** - for distributed tracing
7. **Handle exceptions appropriately** - let decorators handle cross-cutting concerns

## Configuration

Add to `appsettings.json`:

```json
{
  "CQRS": {
    "EnableQueryCaching": true,
    "EnableLogging": true,
    "EnableRetry": false,
    "EnableTransactions": true
  }
}
```

## Troubleshooting

- **Handler not found**: Ensure handlers are registered in the same assembly or explicitly register them
- **Validation failures**: Check that validators implement the correct interface and are registered
- **Transaction issues**: Verify database supports transactions and connection strings are correct
- **Caching issues**: Ensure `IMemoryCache` is registered in DI container