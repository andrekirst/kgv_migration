# Circuit Breaker Pattern - Polly v8 Integration

This document describes the Circuit Breaker implementation using Polly v8 for the KGV Migration project.

## What Was Fixed

The original code was written for Polly v7 but the project uses Polly v8.4.2. The following breaking changes were addressed:

### 1. Removed Deprecated APIs
- `HttpPolicyExtensions.HandleTransientHttpError()` - No longer available in Polly v8
- `AddPolicyHandler()` - Replaced with `AddStandardResilienceHandler()`
- `CircuitBreakerAsync()` - Replaced with new resilience strategy builder

### 2. Updated to New Polly v8 Syntax
- **Modern Resilience Strategies**: Using `AddStandardResilienceHandler()` with options configuration
- **Declarative Configuration**: Circuit Breaker and Retry policies configured through options
- **Predicate-based Error Handling**: Using `ShouldHandle` predicates instead of policy extensions
- **Strategy Builder Pattern**: For database circuit breaker using `ResilienceStrategyBuilder`

### 3. Added Required NuGet Package
- Added `Microsoft.Extensions.Http.Resilience` v8.9.1 for HTTP resilience extensions

## Usage

### Registration in DI Container
```csharp
services.AddCircuitBreakerPolicies(configuration);
```

### Configured Clients
- **ILegacySystemClient**: Higher tolerance (5 failures, 60s break) for known unstable legacy systems
- **IExternalApiClient**: Stricter policy (50% failure ratio, 30s break) for better performance
- **IDatabaseCircuitBreaker**: Database-specific circuit breaker for transient database failures

### Configuration Details

#### Legacy System Client
- Failure Ratio: 50%
- Sampling Duration: 30s
- Minimum Throughput: 5 requests
- Break Duration: 60s
- Retry: 3 attempts with exponential backoff

#### External API Client
- Failure Ratio: 50%
- Sampling Duration: 30s
- Minimum Throughput: 10 requests
- Break Duration: 30s
- Retry: 3 attempts with exponential backoff

#### Database Circuit Breaker
- Failure Ratio: 50%
- Sampling Duration: 30s
- Minimum Throughput: 3 requests
- Break Duration: 30s

## Implementation Details

### Transient Error Detection
The implementation includes intelligent transient error detection for HTTP requests:
- `HttpRequestException`
- `TaskCanceledException`
- HTTP 5xx status codes
- HTTP 408 (Request Timeout)
- HTTP 429 (Too Many Requests)

### Database Error Detection
For database operations, the following errors are considered transient:
- Connection timeouts
- Deadlocks
- Connection failures
- Too many connections

## Benefits of Polly v8

- **Better Performance**: More efficient strategy execution
- **Modern API**: Cleaner, more intuitive configuration
- **Better Observability**: Enhanced telemetry and monitoring support
- **Composition**: Easier to combine multiple resilience strategies
- **Type Safety**: Compile-time validation of configurations