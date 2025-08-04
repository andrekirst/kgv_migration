# KGV Management System - .NET 9 Web API

A comprehensive .NET 9 Web API for managing German allotment garden associations (Kleingartenvereine - KGV). This system provides a modern, scalable, and container-native solution for managing garden plot applications, districts, and administrative processes.

## Features

### Core Functionality
- **Application Management**: Complete CRUD operations for garden plot applications (Antraege)
- **District Management**: Administrative districts (Bezirke) and cadastral districts (Katasterbezirke)
- **History Tracking**: Comprehensive audit trail for all application changes
- **File Reference System**: German administrative file reference numbers (Aktenzeichen)
- **Entry Number Tracking**: Sequential entry numbers per district and year

### Architecture
- **Clean Architecture**: Domain-driven design with clear separation of concerns
- **CQRS Pattern**: Command Query Responsibility Segregation with MediatR
- **Repository Pattern**: Generic repository with Unit of Work
- **Domain-Driven Design**: Rich domain entities with value objects

### Technical Features
- **.NET 9**: Latest .NET framework with performance optimizations
- **Entity Framework Core**: PostgreSQL database with optimized queries
- **AutoMapper**: Object-to-object mapping
- **FluentValidation**: Comprehensive input validation
- **JWT Authentication**: Secure API authentication
- **Swagger/OpenAPI**: Comprehensive API documentation
- **Serilog**: Structured logging with multiple sinks

### Container & DevOps
- **Docker Support**: Multi-stage Dockerfile with Alpine base
- **Docker Compose**: Complete development environment
- **Health Checks**: Kubernetes-ready health endpoints
- **Prometheus Metrics**: Application and infrastructure monitoring
- **Grafana Dashboards**: Visualization and alerting

### German Localization & Compliance
- **German Localization**: Full German language support
- **GDPR Compliance**: Data export, anonymization, and retention
- **German Postal Code Validation**: Proper PLZ validation
- **German Phone Number Validation**: National and international formats
- **Administrative Terminology**: Proper KGV domain language

## Quick Start

### Prerequisites
- .NET 9 SDK
- Docker & Docker Compose
- PostgreSQL 16+ (or use Docker)
- Redis (optional, for caching)

### Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/andrekirst/kgv_migration.git
   cd kgv_migration
   ```

2. **Start the development environment**
   ```bash
   docker-compose up -d
   ```

3. **Run database migrations**
   ```bash
   cd src/KGV.API
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the API**
   - API: https://localhost:5001
   - Swagger UI: https://localhost:5001/swagger
   - Health Checks: https://localhost:5001/health
   - Metrics: https://localhost:5001/metrics

## API Documentation

### Authentication
The API uses JWT bearer token authentication. Include the token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

### Core Endpoints

#### Applications (Antraege)
- `GET /api/antraege` - Get paginated list of applications
- `GET /api/antraege/{id}` - Get specific application
- `POST /api/antraege` - Create new application
- `PUT /api/antraege/{id}` - Update application
- `PATCH /api/antraege/{id}/status` - Update application status
- `DELETE /api/antraege/{id}` - Soft delete application

#### Health & Monitoring
- `GET /health` - Application health status
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe
- `GET /metrics` - Prometheus metrics

### Request/Response Examples

#### Create Application
```json
POST /api/antraege
{
  "vorname": "Max",
  "nachname": "Mustermann",
  "strasse": "Musterstraße 123",
  "plz": "12345",
  "ort": "Berlin",
  "telefon": "030 12345678",
  "email": "max.mustermann@example.com",
  "bewerbungsdatum": "2024-08-04T10:00:00Z"
}
```

## Project Structure

```
src/
├── KGV.API/                 # Web API layer
│   ├── Controllers/         # API controllers
│   ├── Middleware/          # Custom middleware
│   ├── Configuration/       # Service configuration
│   └── Resources/           # Localization resources
├── KGV.Application/         # Application layer
│   ├── Features/            # CQRS commands/queries
│   ├── DTOs/               # Data transfer objects
│   ├── Common/             # Shared interfaces/models
│   └── Mappings/           # AutoMapper profiles
├── KGV.Domain/             # Domain layer
│   ├── Entities/           # Domain entities
│   ├── ValueObjects/       # Value objects
│   ├── Enums/             # Domain enumerations
│   └── Common/            # Base classes
└── KGV.Infrastructure/     # Infrastructure layer
    ├── Data/              # EF Core context/configurations
    ├── Repositories/      # Repository implementations
    └── Patterns/          # Architecture patterns
```

## Deployment

### Docker
```bash
# Build image
docker build -t kgv-api:latest .

# Run container
docker run -p 8080:8080 -e ConnectionStrings__DefaultConnection="..." kgv-api:latest
```

### Production Deployment
```bash
docker-compose -f docker-compose.yml up -d
```

## License

This project is licensed under the MIT License.