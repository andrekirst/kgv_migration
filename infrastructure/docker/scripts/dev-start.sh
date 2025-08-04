#!/bin/bash

# KGV Migration - Development Environment Startup Script
# This script sets up and starts the complete development environment

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
ENV_FILE="$DOCKER_DIR/.env"

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
    log_info "Checking prerequisites..."
    
    # Check if Docker is installed and running
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed. Please install Docker Desktop."
        exit 1
    fi
    
    if ! docker info &> /dev/null; then
        log_error "Docker is not running. Please start Docker Desktop."
        exit 1
    fi
    
    # Check if Docker Compose is available
    if ! command -v docker-compose &> /dev/null; then
        log_error "Docker Compose is not installed."
        exit 1
    fi
    
    log_success "Prerequisites check passed"
}

setup_environment() {
    log_info "Setting up environment configuration..."
    
    cd "$DOCKER_DIR"
    
    # Create .env file if it doesn't exist
    if [ ! -f "$ENV_FILE" ]; then
        log_warning ".env file not found. Creating from template..."
        cp .env.example .env
        log_success "Created .env file from template"
        log_warning "Please review and update the .env file with your settings"
    else
        log_success "Environment file exists"
    fi
}

cleanup_old_containers() {
    log_info "Cleaning up old containers and volumes..."
    
    cd "$DOCKER_DIR"
    
    # Stop and remove containers if they exist
    docker-compose down --remove-orphans 2>/dev/null || true
    
    # Remove dangling images
    docker image prune -f &>/dev/null || true
    
    log_success "Cleanup completed"
}

start_core_services() {
    log_info "Starting core services (database, cache, api, web, nginx)..."
    
    cd "$DOCKER_DIR"
    
    # Pull latest images
    log_info "Pulling latest images..."
    docker-compose pull
    
    # Build and start core services
    docker-compose up -d --build
    
    log_success "Core services started"
}

start_tools() {
    if [ "$1" = "--with-tools" ]; then
        log_info "Starting development tools..."
        cd "$DOCKER_DIR"
        docker-compose --profile tools up -d
        log_success "Development tools started"
    fi
}

start_monitoring() {
    if [ "$1" = "--with-monitoring" ] || [ "$2" = "--with-monitoring" ]; then
        log_info "Starting monitoring stack..."
        cd "$DOCKER_DIR"
        docker-compose --profile monitoring up -d
        log_success "Monitoring stack started"
    fi
}

wait_for_services() {
    log_info "Waiting for services to be healthy..."
    
    cd "$DOCKER_DIR"
    
    # Wait for database
    log_info "Waiting for PostgreSQL..."
    while ! docker-compose exec -T postgres pg_isready -U kgv_admin -d kgv_development &>/dev/null; do
        sleep 2
    done
    
    # Wait for Redis
    log_info "Waiting for Redis..."
    while ! docker-compose exec -T redis redis-cli ping &>/dev/null; do
        sleep 2
    done
    
    # Wait for API
    log_info "Waiting for API..."
    while ! curl -f http://localhost:5000/health &>/dev/null; do
        sleep 5
    done
    
    # Wait for Web
    log_info "Waiting for Web application..."
    while ! curl -f http://localhost:3000 &>/dev/null; do
        sleep 5
    done
    
    # Wait for Nginx
    log_info "Waiting for Nginx..."
    while ! curl -f http://localhost/health &>/dev/null; do
        sleep 2
    done
    
    log_success "All services are healthy"
}

show_service_urls() {
    log_success "Development environment is ready!"
    echo ""
    echo "🚀 Service URLs:"
    echo "   Frontend (via Nginx):     http://localhost"
    echo "   API (via Nginx):          http://localhost/api"
    echo "   API (direct):             http://localhost:5000"
    echo "   Frontend (direct):        http://localhost:3000"
    echo ""
    
    if docker-compose ps | grep -q "tools"; then
        echo "🛠️  Development Tools:"
        echo "   pgAdmin:                  http://localhost:5050"
        echo "   Adminer:                  http://localhost:8080"
        echo "   MailHog:                  http://localhost:8025"
        echo ""
    fi
    
    if docker-compose ps | grep -q "monitoring"; then
        echo "📊 Monitoring:"
        echo "   Grafana:                  http://localhost:3001"
        echo "   Prometheus:               http://localhost:9090"
        echo "   Seq Logs:                 http://localhost:5341"
        echo "   Jaeger Tracing:           http://localhost:16686"
        echo ""
    fi
    
    echo "📋 Useful commands:"
    echo "   View logs:                docker-compose logs -f"
    echo "   Stop services:            docker-compose down"
    echo "   Restart service:          docker-compose restart <service>"
    echo "   Shell into container:     docker-compose exec <service> sh"
    echo ""
    echo "🔧 For development:"
    echo "   API code changes will auto-reload with 'dotnet watch'"
    echo "   Frontend changes will hot-reload via Next.js HMR"
}

show_help() {
    echo "KGV Migration - Development Environment Startup"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --with-tools        Start development tools (pgAdmin, Adminer, MailHog)"
    echo "  --with-monitoring   Start monitoring stack (Grafana, Prometheus, Seq, Jaeger)"
    echo "  --cleanup          Clean up containers and volumes before starting"
    echo "  --help             Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                           # Start core services only"
    echo "  $0 --with-tools              # Start with development tools"
    echo "  $0 --with-monitoring         # Start with monitoring"
    echo "  $0 --with-tools --with-monitoring  # Start everything"
    echo "  $0 --cleanup --with-tools    # Clean up first, then start with tools"
}

# Main execution
main() {
    echo "=================================================="
    echo "  KGV Migration - Development Environment Setup"
    echo "=================================================="
    echo ""
    
    # Parse arguments
    CLEANUP=false
    WITH_TOOLS=false
    WITH_MONITORING=false
    
    for arg in "$@"; do
        case $arg in
            --help)
                show_help
                exit 0
                ;;
            --cleanup)
                CLEANUP=true
                ;;
            --with-tools)
                WITH_TOOLS=true
                ;;
            --with-monitoring)
                WITH_MONITORING=true
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
    setup_environment
    
    if [ "$CLEANUP" = true ]; then
        cleanup_old_containers
    fi
    
    start_core_services
    
    if [ "$WITH_TOOLS" = true ]; then
        start_tools --with-tools
    fi
    
    if [ "$WITH_MONITORING" = true ]; then
        start_monitoring --with-monitoring
    fi
    
    wait_for_services
    show_service_urls
}

# Execute main function with all arguments
main "$@"