#!/bin/bash

# KGV Migration - Production Deployment Script
# This script deploys the application in production mode

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOCKER_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="$DOCKER_DIR/.env.prod"

# Functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

check_prerequisites() {
    log_info "Checking prerequisites for production deployment..."
    
    # Check if Docker is installed and running
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed."
        exit 1
    fi
    
    if ! docker info &> /dev/null; then
        log_error "Docker is not running."
        exit 1
    fi
    
    # Check if Docker Compose is available
    if ! command -v docker-compose &> /dev/null; then
        log_error "Docker Compose is not installed."
        exit 1
    fi
    
    log_success "Prerequisites check passed"
}

validate_environment() {
    log_info "Validating production environment configuration..."
    
    cd "$DOCKER_DIR"
    
    # Check if production env file exists
    if [ ! -f "$ENV_FILE" ]; then
        log_error "Production environment file not found: $ENV_FILE"
        log_error "Please copy .env.prod.example to .env.prod and configure it"
        exit 1
    fi
    
    # Source the environment file
    set -a
    source "$ENV_FILE"
    set +a
    
    # Validate critical environment variables
    local required_vars=("POSTGRES_PASSWORD" "REDIS_PASSWORD" "JWT_SECRET" "NEXTAUTH_SECRET")
    local missing_vars=()
    
    for var in "${required_vars[@]}"; do
        if [ -z "${!var}" ]; then
            missing_vars+=("$var")
        fi
    done
    
    if [ ${#missing_vars[@]} -ne 0 ]; then
        log_error "Missing required environment variables:"
        for var in "${missing_vars[@]}"; do
            echo "  - $var"
        done
        exit 1
    fi
    
    # Validate password strength (basic check)
    if [ ${#POSTGRES_PASSWORD} -lt 16 ]; then
        log_error "POSTGRES_PASSWORD must be at least 16 characters long"
        exit 1
    fi
    
    if [ ${#JWT_SECRET} -lt 32 ]; then
        log_error "JWT_SECRET must be at least 32 characters long"
        exit 1
    fi
    
    log_success "Environment validation passed"
}

check_ssl_certificates() {
    log_info "Checking SSL certificates..."
    
    local ssl_dir="$DOCKER_DIR/nginx/ssl"
    
    if [ ! -f "$ssl_dir/cert.pem" ] || [ ! -f "$ssl_dir/key.pem" ]; then
        log_warning "SSL certificates not found in $ssl_dir"
        log_warning "The application will run with HTTP only"
        log_warning "For production, please provide SSL certificates:"
        log_warning "  - $ssl_dir/cert.pem (certificate)"
        log_warning "  - $ssl_dir/key.pem (private key)"
        
        read -p "Continue without SSL? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    else
        log_success "SSL certificates found"
    fi
}

backup_existing_data() {
    log_info "Creating backup of existing data..."
    
    cd "$DOCKER_DIR"
    
    local backup_dir="backups/$(date +%Y%m%d_%H%M%S)"
    mkdir -p "$backup_dir"
    
    # Backup database if running
    if docker-compose -f docker-compose.prod.yml ps postgres | grep -q "Up"; then
        log_info "Backing up PostgreSQL database..."
        docker-compose -f docker-compose.prod.yml exec -T postgres pg_dump -U "${POSTGRES_USER:-kgv_user}" "${POSTGRES_DB:-kgv_production}" > "$backup_dir/database.sql"
        log_success "Database backed up to $backup_dir/database.sql"
    fi
    
    # Backup volumes
    local volumes=("kgv-postgres-data-prod" "kgv-redis-data-prod" "kgv-api-logs-prod")
    for volume in "${volumes[@]}"; do
        if docker volume ls | grep -q "$volume"; then
            log_info "Backing up volume: $volume"
            docker run --rm -v "$volume":/data -v "$(pwd)/$backup_dir":/backup alpine tar czf "/backup/${volume}.tar.gz" -C /data .
        fi
    done
    
    log_success "Backup completed in $backup_dir"
}

deploy_application() {
    log_info "Deploying application in production mode..."
    
    cd "$DOCKER_DIR"
    
    # Pull latest images
    log_info "Pulling latest base images..."
    docker-compose -f docker-compose.prod.yml --env-file "$ENV_FILE" pull postgres redis nginx prometheus grafana seq
    
    # Build application images
    log_info "Building application images..."
    docker-compose -f docker-compose.prod.yml --env-file "$ENV_FILE" build --no-cache api web
    
    # Start services with zero-downtime deployment approach
    log_info "Starting services..."
    docker-compose -f docker-compose.prod.yml --env-file "$ENV_FILE" up -d
    
    log_success "Application deployment completed"
}

wait_for_services() {
    log_info "Waiting for services to be healthy..."
    
    cd "$DOCKER_DIR"
    
    local max_attempts=60
    local attempt=0
    
    # Wait for database
    log_info "Waiting for PostgreSQL..."
    while ! docker-compose -f docker-compose.prod.yml exec -T postgres pg_isready -U "${POSTGRES_USER:-kgv_user}" -d "${POSTGRES_DB:-kgv_production}" &>/dev/null; do
        sleep 5
        attempt=$((attempt + 1))
        if [ $attempt -gt $max_attempts ]; then
            log_error "PostgreSQL failed to start within expected time"
            exit 1
        fi
    done
    
    # Wait for Redis
    log_info "Waiting for Redis..."
    attempt=0
    while ! docker-compose -f docker-compose.prod.yml exec -T redis redis-cli -a "$REDIS_PASSWORD" ping &>/dev/null; do
        sleep 5
        attempt=$((attempt + 1))
        if [ $attempt -gt $max_attempts ]; then
            log_error "Redis failed to start within expected time"
            exit 1
        fi
    done
    
    # Wait for API
    log_info "Waiting for API..."
    attempt=0
    while ! curl -f http://localhost/api/health &>/dev/null; do
        sleep 10
        attempt=$((attempt + 1))
        if [ $attempt -gt $max_attempts ]; then
            log_error "API failed to start within expected time"
            exit 1
        fi
    done
    
    # Wait for Web
    log_info "Waiting for Web application..."
    attempt=0
    while ! curl -f http://localhost &>/dev/null; do
        sleep 10
        attempt=$((attempt + 1))
        if [ $attempt -gt $max_attempts ]; then
            log_error "Web application failed to start within expected time"
            exit 1
        fi
    done
    
    log_success "All services are healthy"
}

run_health_checks() {
    log_info "Running comprehensive health checks..."
    
    cd "$DOCKER_DIR"
    
    # Check container status
    local unhealthy_containers=$(docker-compose -f docker-compose.prod.yml ps --format "table {{.Name}}\t{{.Status}}" | grep -v "Up" | tail -n +2)
    
    if [ ! -z "$unhealthy_containers" ]; then
        log_error "Some containers are not healthy:"
        echo "$unhealthy_containers"
        exit 1
    fi
    
    # Check API endpoints
    local api_health=$(curl -s http://localhost/api/health | jq -r '.status' 2>/dev/null || echo "error")
    if [ "$api_health" != "healthy" ]; then
        log_error "API health check failed"
        exit 1
    fi
    
    # Check database connectivity
    if ! docker-compose -f docker-compose.prod.yml exec -T postgres psql -U "${POSTGRES_USER:-kgv_user}" -d "${POSTGRES_DB:-kgv_production}" -c "SELECT 1" &>/dev/null; then
        log_error "Database connectivity check failed"
        exit 1
    fi
    
    # Check Redis connectivity
    if ! docker-compose -f docker-compose.prod.yml exec -T redis redis-cli -a "$REDIS_PASSWORD" ping &>/dev/null; then
        log_error "Redis connectivity check failed"
        exit 1
    fi
    
    log_success "All health checks passed"
}

show_deployment_info() {
    log_success "Production deployment completed successfully!"
    echo ""
    echo "🚀 Application URLs:"
    echo "   Main Application:         http://localhost"
    echo "   API Documentation:        http://localhost/api/docs"
    echo ""
    echo "📊 Monitoring:"
    echo "   Grafana:                  http://localhost:${GRAFANA_PORT:-3001}"
    echo "   Prometheus:               http://localhost:${PROMETHEUS_PORT:-9090}"
    echo "   Seq Logs:                 http://localhost:${SEQ_WEB_PORT:-5341}"
    echo ""
    echo "📋 Management commands:"
    echo "   View logs:                docker-compose -f docker-compose.prod.yml logs -f"
    echo "   Stop services:            docker-compose -f docker-compose.prod.yml down"
    echo "   Update application:       $0 --update"
    echo "   Create backup:            $0 --backup"
    echo ""
    echo "⚠️  Important:"
    echo "   - Monitor application logs for any issues"
    echo "   - Set up automated backups"
    echo "   - Configure proper firewall rules"
    echo "   - Update SSL certificates before expiry"
}

show_help() {
    echo "KGV Migration - Production Deployment Script"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --backup           Create backup before deployment"
    echo "  --update           Update existing deployment"
    echo "  --no-ssl-check     Skip SSL certificate validation"
    echo "  --help             Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                        # Deploy with all checks"
    echo "  $0 --backup               # Create backup and deploy"
    echo "  $0 --update               # Update existing deployment"
    echo "  $0 --no-ssl-check         # Deploy without SSL validation"
}

# Main execution
main() {
    echo "=================================================="
    echo "  KGV Migration - Production Deployment"
    echo "=================================================="
    echo ""
    
    # Parse arguments
    BACKUP=false
    UPDATE=false
    NO_SSL_CHECK=false
    
    for arg in "$@"; do
        case $arg in
            --help)
                show_help
                exit 0
                ;;
            --backup)
                BACKUP=true
                ;;
            --update)
                UPDATE=true
                ;;
            --no-ssl-check)
                NO_SSL_CHECK=true
                ;;
            *)
                log_error "Unknown option: $arg"
                show_help
                exit 1
                ;;
        esac
    done
    
    # Execute steps
    check_prerequisites
    validate_environment
    
    if [ "$NO_SSL_CHECK" != true ]; then
        check_ssl_certificates
    fi
    
    if [ "$BACKUP" = true ] || [ "$UPDATE" = true ]; then
        backup_existing_data
    fi
    
    deploy_application
    wait_for_services
    run_health_checks
    show_deployment_info
}

# Execute main function with all arguments
main "$@"