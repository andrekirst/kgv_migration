# KGV Migration - Critical Architecture Patterns Implementation

This directory contains the implementation of critical architecture patterns for the KGV (Kleingartenverein) migration project. These patterns provide the foundation for a resilient, scalable, and maintainable system.

## Overview

The KGV migration system implements five critical architecture patterns to ensure robust operation during the transition from legacy SQL Server to modern PostgreSQL infrastructure:

### Implemented Patterns

1. **Anti-Corruption Layer** - Legacy Integration Protection
2. **Health Endpoint Monitoring** - Container-native health checks
3. **CQRS Pattern** - Command Query Responsibility Segregation
4. **Cache-Aside Pattern** - Redis cache integration
5. **Queue-Based Load Leveling** - Asynchronous processing with message queues

## Architecture Patterns

### 1. Anti-Corruption Layer (`AntiCorruption/`)

**Purpose**: Protects the modern domain model from legacy system complexity and data inconsistencies.

**Key Components**:
- `ILegacyDataTranslator<TLegacy, TModern>` - Translation interface
- `AntragTranslator` - Application-specific translator
- `LegacyDatabaseContext` - Legacy SQL Server access layer
- `LegacyDataFacade` - High-level coordination API

**Usage**:
```csharp
// Configure in Startup.cs
services.AddAntiCorruptionLayer(configuration);

// Use translator
var modernApplication = await _antragTranslator.TranslateToModernAsync(legacyAntrag);
```

**Benefits**:
- Isolates legacy system complexity
- Enables gradual migration
- Maintains data integrity during transition
- Provides validation and error handling

### 2. Health Endpoint Monitoring (`HealthChecks/`)

**Purpose**: Provides comprehensive health monitoring for container orchestration and system observability.

**Key Components**:
- `ApplicationHealthCheck` - Core application health
- `LegacySystemHealthCheck` - Legacy connectivity
- `DataConsistencyHealthCheck` - Data integrity validation
- `ContainerHealthCheck` - Container-specific checks
- `PrometheusHealthCheckPublisher` - Metrics integration

**Usage**:
```csharp
// Configure in Startup.cs  
services.AddKgvHealthChecks(configuration);

// Endpoints available:
// GET /health - Overall health
// GET /health/ready - Readiness probe
// GET /health/live - Liveness probe
```

**Health Check Categories**:
- **Live**: Basic application functionality
- **Ready**: External dependencies (database, cache, legacy system)
- **Degraded**: Non-critical failures that don't stop operation

### 3. CQRS Pattern (`CQRS/`)

**Purpose**: Separates read and write operations for better scalability and maintainability.

**Key Components**:
- `ICqrsMediator` - Central command/query dispatcher
- `ICommandHandler<T>` / `IQueryHandler<T>` - Handler interfaces
- `CqrsMetrics` - Performance monitoring
- Decorator pattern for cross-cutting concerns

**Usage**:
```csharp
// Configure in Startup.cs
services.AddCqrsPattern(configuration);

// Commands (write operations)
var result = await _mediator.SendAsync(new CreateApplicationCommand 
{
    FileReference = "A-2024-001",
    PrimaryContact = contactDto,
    Address = addressDto
});

// Queries (read operations)
var application = await _mediator.QueryAsync(new GetApplicationByIdQuery 
{
    ApplicationId = applicationId,
    IncludeHistory = true
});
```

**Command Examples**:
- `CreateApplicationCommand`
- `UpdateApplicationCommand`  
- `ChangeApplicationStatusCommand`
- `DeleteApplicationCommand`

**Query Examples**:
- `GetApplicationByIdQuery`
- `SearchApplicationsQuery`
- `GetApplicationStatisticsQuery`
- `GetApplicationsByStatusQuery`

### 4. Cache-Aside Pattern (`Caching/`)

**Purpose**: Improves performance through intelligent caching with Redis backend.

**Key Components**:
- `ICacheService` - Generic cache operations
- `ApplicationCacheService` - Domain-specific caching
- `CacheKeyBuilder` - Consistent key generation
- `ICacheInvalidationStrategy` - Cache coherency

**Usage**:
```csharp
// Configure in Startup.cs
services.AddCacheAsidePattern(configuration);

// Get or set cached data
var application = await _applicationCache.GetOrSetAsync(
    applicationId.ToString(),
    () => _repository.GetByIdAsync(applicationId),
    TimeSpan.FromMinutes(30)
);

// Cache search results
var results = await _applicationCache.GetSearchResultsAsync(
    searchQuery,
    () => _repository.SearchAsync(searchQuery)
);
```

**Cache Strategies**:
- **Application Data**: 30-minute TTL
- **Person Data**: 1-hour TTL
- **District Data**: 24-hour TTL (static reference data)
- **Statistics**: 5-minute TTL (frequently changing)
- **Search Results**: 5-minute TTL

### 5. Queue-Based Load Leveling (`Messaging/`)

**Purpose**: Handles load spikes and provides reliable asynchronous processing.

**Key Components**:
- `IMessageQueue<T>` - Queue operations interface
- `RedisMessageQueue<T>` - Redis-based implementation
- `LoadLevelingService` - Adaptive throttling
- `MessageProcessor<T>` - Background processing
- Circuit breaker and retry policies

**Usage**:
```csharp
// Configure in Startup.cs
services.AddQueueBasedLoadLeveling(configuration);

// Publish events
await _messagePublisher.PublishAsync("kgv.application.created", new ApplicationCreatedEvent
{
    ApplicationId = application.Id,
    FileReference = application.FileReference,
    CreatedAt = DateTime.UtcNow
});

// Messages are automatically processed by background services
```

**Domain Events**:
- `ApplicationCreatedEvent`
- `ApplicationUpdatedEvent`
- `ApplicationStatusChangedEvent`
- `DataMigrationEvent`
- `NotificationEvent`
- `AuditEvent`

## Metrics and Monitoring

### Prometheus Integration (`Metrics/`)

The system exposes comprehensive metrics for monitoring:

**Application Metrics**:
- `kgv_applications_created_total`
- `kgv_applications_active`
- `kgv_application_processing_duration_seconds`

**Cache Metrics**:
- `kgv_cache_operations_total`
- `kgv_cache_hit_ratio`
- `kgv_cache_operation_duration_seconds`

**Queue Metrics**:
- `kgv_messages_processed_total`
- `kgv_queue_length`
- `kgv_message_processing_duration_seconds`

**Health Metrics**:
- `kgv_health_check_status`
- `kgv_health_check_duration_seconds`

**System Metrics**:
- `kgv_memory_usage_bytes`
- `kgv_cpu_usage_percent`
- `kgv_database_operations_total`

## Configuration

### Required Configuration Sections

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=kgv_migration;...",
    "LegacyDatabase": "Server=legacy-sql;Database=KGV_Legacy;...",
    "Redis": "localhost:6379"
  },
  "StranglerFig": {
    "Enabled": true,
    "Routes": [...]
  },
  "CircuitBreaker": {
    "LegacySystem": {
      "FailureThreshold": 5,
      "DurationOfBreak": 60
    }
  },
  "Caching": {
    "Redis": {
      "DefaultExpiration": "00:20:00",
      "KeyPrefix": "kgv:",
      "DatabaseNumber": 0
    }
  },
  "Messaging": {
    "Redis": {
      "DatabaseNumber": 1,
      "MaxDeliveryCount": 5
    },
    "LoadLeveling": {
      "MaxConcurrentLoad": 100,
      "MaxErrorRateThreshold": 0.1
    }
  },
  "Monitoring": {
    "HealthChecks": {
      "Database": { "Enabled": true, "Interval": 30 },
      "Redis": { "Enabled": true, "Interval": 30 },
      "LegacySystem": { "Enabled": true, "Interval": 60 }
    }
  },
  "Prometheus": {
    "MetricsPath": "/metrics"
  }
}
```

## Docker Integration

### Health Check Configuration

```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost/health/ready || exit 1
```

### Kubernetes Deployment

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 80
  initialDelaySeconds: 5
  periodSeconds: 5
```

## Performance Characteristics

### Target Metrics

- **Coupling (CBO)**: < 4.0 ✅
- **Cohesion (LCOM)**: > 0.7 ✅
- **Cache Hit Ratio**: > 80%
- **Queue Processing**: < 1 second average
- **Health Check Response**: < 100ms
- **Database Query Time**: < 500ms (95th percentile)

### Scalability Features

- **Horizontal Scaling**: Stateless design with Redis for shared state
- **Load Balancing**: Queue-based processing distributes load
- **Circuit Breakers**: Prevent cascade failures
- **Caching**: Reduces database load
- **Async Processing**: Non-blocking operations

## Error Handling and Resilience

### Resilience Patterns

1. **Circuit Breaker**: Prevents cascade failures
2. **Retry with Exponential Backoff**: Handles transient failures
3. **Timeout Protection**: Prevents hanging operations
4. **Graceful Degradation**: Continues operation with reduced functionality
5. **Dead Letter Queues**: Handles poison messages

### Error Categories

- **Transient Errors**: Automatically retried
- **Business Logic Errors**: Logged and reported
- **System Errors**: Circuit breaker activation
- **Data Validation Errors**: Immediate failure with detailed messages

## Development Guidelines

### Adding New Patterns

1. Create interface in appropriate namespace
2. Implement with proper logging and metrics
3. Add configuration options
4. Include health checks
5. Write comprehensive tests
6. Update documentation

### Testing Strategy

- **Unit Tests**: Individual pattern components
- **Integration Tests**: Pattern interactions
- **Performance Tests**: Load and stress testing
- **Health Check Tests**: Container orchestration compatibility

## Migration Strategy

### Phase 1: Foundation (Weeks 1-4)
- ✅ Health monitoring implementation
- ✅ Basic caching setup
- ✅ CQRS read operations

### Phase 2: Core Migration (Weeks 5-8)
- ✅ Anti-corruption layer deployment
- ✅ Queue-based processing
- ✅ Write operation migration

### Phase 3: Optimization (Weeks 9-12)
- ✅ Performance tuning
- ✅ Cache optimization
- ✅ Load testing validation

### Phase 4: Production (Weeks 13-16)
- Production deployment
- Monitoring and alerting
- Performance optimization
- Legacy system decommission planning

## Troubleshooting

### Common Issues

1. **Cache Miss Ratio High**
   - Check TTL settings
   - Verify key generation consistency
   - Monitor cache eviction patterns

2. **Queue Backlog Building**
   - Check consumer health
   - Verify load leveling settings
   - Scale consumer instances

3. **Health Checks Failing**
   - Check dependency availability
   - Verify timeout settings
   - Review connectivity issues

4. **High Memory Usage**
   - Monitor cache size
   - Check for memory leaks
   - Optimize object lifecycle

### Monitoring and Alerting

Essential alerts to configure:

- Health check failures
- Cache hit ratio < 70%
- Queue length > 100 messages
- Error rate > 5%
- Response time > 2 seconds
- Memory usage > 80%

## References

- [Microsoft Azure Architecture Patterns](https://docs.microsoft.com/en-us/azure/architecture/patterns/)
- [Prometheus Monitoring](https://prometheus.io/docs/)
- [Redis Best Practices](https://redis.io/docs/manual/patterns/)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Circuit Breaker Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker)