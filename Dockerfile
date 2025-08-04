# Multi-stage Dockerfile for KGV .NET 9 Web API
# Stage 1: Build environment
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /app

# Create non-root user for build process
RUN addgroup -g 1001 -S dotnet && \
    adduser -S dotnet -u 1001 -G dotnet

# Copy csproj files and restore dependencies
COPY src/KGV.Domain/KGV.Domain.csproj ./src/KGV.Domain/
COPY src/KGV.Application/KGV.Application.csproj ./src/KGV.Application/
COPY src/KGV.Infrastructure/KGV.Infrastructure.csproj ./src/KGV.Infrastructure/
COPY src/KGV.API/KGV.API.csproj ./src/KGV.API/
COPY src/KGV.sln ./src/

# Change ownership to non-root user
RUN chown -R dotnet:dotnet /app
USER dotnet

# Restore dependencies
RUN dotnet restore src/KGV.sln

# Copy source code
COPY --chown=dotnet:dotnet src/ ./src/

# Build the application
WORKDIR /app/src/KGV.API
RUN dotnet build -c Release --no-restore

# Publish the application
RUN dotnet publish -c Release --no-build -o /app/publish

# Stage 2: Runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# Install additional packages for German localization and security
RUN apk add --no-cache \
    tzdata \
    ca-certificates \
    icu-libs \
    && rm -rf /var/cache/apk/*

# Set German timezone as default
ENV TZ=Europe/Berlin

# Set environment variables for globalization
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LC_ALL=de_DE.UTF-8
ENV LANG=de_DE.UTF-8

# Create non-root user
RUN addgroup -g 1001 -S kgvapi && \
    adduser -S kgvapi -u 1001 -G kgvapi

# Create application directory
WORKDIR /app

# Copy published application
COPY --from=build --chown=kgvapi:kgvapi /app/publish .

# Create directory for logs with proper permissions
RUN mkdir -p /app/logs && \
    chown -R kgvapi:kgvapi /app/logs

# Switch to non-root user
USER kgvapi

# Expose ports
EXPOSE 8080
EXPOSE 8443

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1

# Set entrypoint
ENTRYPOINT ["dotnet", "KGV.API.dll"]

# Metadata
LABEL maintainer="KGV Development Team"
LABEL description="KGV Management API - Containerized .NET 9 Web API"
LABEL version="1.0.0"
LABEL org.opencontainers.image.source="https://github.com/andrekirst/kgv_migration"
LABEL org.opencontainers.image.documentation="https://github.com/andrekirst/kgv_migration/README.md"
LABEL org.opencontainers.image.vendor="KGV Organization"
LABEL org.opencontainers.image.licenses="MIT"