# Multi-stage Dockerfile for KGV .NET 9 Web API

# Development stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS development
WORKDIR /app
EXPOSE 5000

# Install curl for health checks
RUN apk add --no-cache curl

# Development dependencies
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS development-with-deps
WORKDIR /src

# Copy project files
COPY *.csproj ./
RUN dotnet restore

# Copy source code
COPY . ./
RUN dotnet build -c Debug -o /app

# Set up file watcher for hot reload
WORKDIR /app
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:5000"]

# Production build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy source code and build
COPY . ./
RUN dotnet build -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Production runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS production
WORKDIR /app

# Install curl for health checks
RUN apk add --no-cache curl \
    && addgroup -g 1001 -S appgroup \
    && adduser -S -D -H -u 1001 -h /app -s /sbin/nologin -G appgroup appuser

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "KGV.Api.dll"]