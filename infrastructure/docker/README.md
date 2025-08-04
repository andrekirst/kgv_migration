# KGV Migration Docker Infrastructure

This directory contains the complete Docker infrastructure for the KGV Migration project, supporting both development and production environments.

## Quick Start

### Development Environment

1. Copy environment configuration:
   ```bash
   cp .env.example .env
   ```

2. Start all services:
   ```bash
   docker-compose up -d
   ```

3. Access the application:
   - **Frontend**: http://localhost (via nginx proxy)
   - **API**: http://localhost/api (via nginx proxy)
   - **Direct API**: http://localhost:5000 (development only)
   - **Direct Frontend**: http://localhost:3000 (development only)

### Production Environment

1. Copy production environment configuration:
   ```bash
   cp .env.prod.example .env.prod
   ```

2. Edit `.env.prod` with your production values

3. Start production services:
   ```bash
   docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d
   ```

## Architecture Overview

### Core Services

- **postgres**: PostgreSQL 16 database with German locale support
- **redis**: Redis 7 for caching and session storage
- **api**: .NET 9 Web API (KGV.Api)
- **web**: Next.js frontend application (KGV.Web)
- **nginx**: Reverse proxy and load balancer

### Development Tools (Profile: tools)

- **pgadmin**: Database management interface (http://localhost:5050)
- **adminer**: Lightweight database client (http://localhost:8080)
- **mailhog**: Email testing tool (http://localhost:8025)

### Monitoring Stack (Profile: monitoring)

- **prometheus**: Metrics collection (http://localhost:9090)
- **grafana**: Metrics visualization (http://localhost:3001)
- **seq**: Centralized logging (http://localhost:5341)
- **jaeger**: Distributed tracing (http://localhost:16686)
- **otel-collector**: OpenTelemetry collector

## Service Configuration

### Environment Variables

Key environment variables (see `.env.example` for complete list):

- `POSTGRES_PASSWORD`: Database password
- `REDIS_PASSWORD`: Redis password
- `JWT_SECRET`: JWT signing key (minimum 256 bits)
- `NEXTAUTH_SECRET`: NextAuth.js secret

### Network Configuration

All services run on the `kgv-network` bridge network for development and `kgv-network-prod` for production.

### Volume Mounts

- **Development**: Source code is mounted for hot reload
- **Production**: Only data volumes are mounted

## Docker Profiles

Use profiles to start specific service groups:

```bash
# Start core services only
docker-compose up -d

# Include development tools
docker-compose --profile tools up -d

# Include monitoring stack
docker-compose --profile monitoring up -d

# Start everything
docker-compose --profile tools --profile monitoring up -d
```

## Health Checks

All services include health checks:

- **postgres**: `pg_isready` command
- **redis**: `redis-cli ping`
- **api**: HTTP GET `/health`
- **web**: HTTP GET `/api/health`
- **nginx**: HTTP GET `/health`

## SSL/TLS Configuration (Production)

For production with SSL:

1. Place SSL certificates in `nginx/ssl/`:
   - `cert.pem`: SSL certificate
   - `key.pem`: Private key

2. Update `NEXTAUTH_URL` and `NEXT_PUBLIC_API_URL` to use `https://`

## Development Features

### Hot Reload Support

- **API**: Uses `dotnet watch` for automatic rebuilds
- **Frontend**: Next.js development server with HMR
- **Nginx**: WebSocket proxy for hot reload

### Volume Mounts

Development containers mount source code directories:
- `../../src/KGV.Api:/app` (API)
- `../../src/KGV.Web:/app` (Frontend)

### Database Initialization

The PostgreSQL container automatically runs initialization scripts from `init-scripts/`:
- Creates extensions (uuid-ossp, pg_trgm, btree_gin)
- Sets up audit logging schema
- Creates application user and permissions

## Production Optimizations

### Resource Limits

Production containers have defined resource limits:
- **postgres**: 1GB RAM, 0.5 CPU
- **redis**: 512MB RAM, 0.25 CPU
- **api**: 1GB RAM, 0.5 CPU
- **web**: 512MB RAM, 0.25 CPU
- **nginx**: 256MB RAM, 0.25 CPU

### Multi-stage Builds

Dockerfiles use multi-stage builds for optimized production images:
- Separate development and production targets
- Minimal runtime images with security hardening
- Non-root user execution

### Logging

Production containers use structured JSON logging with:
- Log rotation (10MB max, 5 files)
- Centralized collection via Seq

## Troubleshooting

### Common Issues

1. **Port conflicts**: Ensure ports 80, 443, 3000, 5000, 5432, 6379 are available

2. **Permission errors**: 
   ```bash
   sudo chown -R $USER:$USER ./
   ```

3. **Database connection issues**: Check PostgreSQL logs:
   ```bash
   docker-compose logs postgres
   ```

4. **Memory issues**: Increase Docker Desktop memory allocation to 4GB+

### Useful Commands

```bash
# View logs for all services
docker-compose logs -f

# View logs for specific service
docker-compose logs -f api

# Restart a service
docker-compose restart api

# Rebuild and restart
docker-compose up -d --build api

# Check service health
docker-compose ps

# Clean up everything
docker-compose down -v --remove-orphans
```

## Security Considerations

### Development
- Default passwords are provided for convenience
- Services expose ports directly for debugging

### Production
- Strong passwords required
- Services only accessible through nginx proxy
- SSL/TLS encryption enabled
- Security headers configured
- Non-root container execution
- Resource limits enforced

## Backup and Recovery

### Database Backup
```bash
# Create backup
docker-compose exec postgres pg_dump -U kgv_admin kgv_development > backup.sql

# Restore backup  
docker-compose exec -T postgres psql -U kgv_admin kgv_development < backup.sql
```

### Volume Backup
```bash
# Backup all volumes
docker run --rm -v kgv-postgres-data:/data -v $(pwd):/backup alpine tar czf /backup/postgres-backup.tar.gz -C /data .
```

## Monitoring and Alerting

Access monitoring services:
- **Grafana**: http://localhost:3001 (admin/password from .env)
- **Prometheus**: http://localhost:9090
- **Seq**: http://localhost:5341
- **Jaeger**: http://localhost:16686

Custom dashboards are provisioned in `monitoring/grafana/dashboards/`.

## Support

For issues and questions:
1. Check service logs: `docker-compose logs <service>`
2. Verify health status: `docker-compose ps`
3. Review environment variables: `docker-compose config`
4. Consult the main project documentation