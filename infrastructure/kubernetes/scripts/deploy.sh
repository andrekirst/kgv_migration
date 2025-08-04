#!/bin/bash

# KGV Kubernetes Deployment Script
# This script deploys the KGV application to a Kubernetes cluster using Kustomize

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$(dirname "$SCRIPT_DIR")"
PROJECT_ROOT="$(dirname "$(dirname "$(dirname "$K8S_DIR")")")"

# Default values
ENVIRONMENT="development"
NAMESPACE=""
DRY_RUN=false
APPLY_MONITORING=true
APPLY_BACKUP=true
WAIT_TIMEOUT=300
VERBOSE=false

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Functions
log() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $1"
}

success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Deploy KGV application to Kubernetes cluster

OPTIONS:
    -e, --environment   Environment to deploy (development|staging|production) [default: development]
    -n, --namespace     Override default namespace
    -d, --dry-run       Perform a dry run without applying changes
    --no-monitoring     Skip monitoring stack deployment
    --no-backup         Skip backup jobs deployment
    -t, --timeout       Wait timeout in seconds [default: 300]
    -v, --verbose       Enable verbose output
    -h, --help          Show this help message

EXAMPLES:
    $0 -e development                    # Deploy to development environment
    $0 -e production -n kgv-prod        # Deploy to production with custom namespace
    $0 -e staging --dry-run              # Dry run for staging environment
    $0 -e production --no-monitoring     # Deploy production without monitoring

PREREQUISITES:
    - kubectl configured with cluster access
    - kustomize installed (or kubectl with kustomize support)
    - Appropriate RBAC permissions
    - Container registry access configured
    - Storage classes available (fast-ssd, standard)

EOF
}

check_prerequisites() {
    log "Checking prerequisites..."
    
    # Check kubectl
    if ! command -v kubectl &> /dev/null; then
        error "kubectl is not installed or not in PATH"
        exit 1
    fi
    
    # Check cluster connectivity
    if ! kubectl cluster-info &> /dev/null; then
        error "Cannot connect to Kubernetes cluster"
        exit 1
    fi
    
    # Check kustomize support
    if ! kubectl kustomize --help &> /dev/null; then
        error "kubectl does not support kustomize"
        exit 1
    fi
    
    success "Prerequisites check passed"
}

validate_environment() {
    case $ENVIRONMENT in
        development|staging|production)
            ;;
        *)
            error "Invalid environment: $ENVIRONMENT"
            error "Supported environments: development, staging, production"
            exit 1
            ;;
    esac
    
    # Set default namespace if not provided
    if [ -z "$NAMESPACE" ]; then
        case $ENVIRONMENT in
            development) NAMESPACE="kgv-dev" ;;
            staging) NAMESPACE="kgv-staging" ;;
            production) NAMESPACE="kgv-system" ;;
        esac
    fi
    
    log "Environment: $ENVIRONMENT"
    log "Namespace: $NAMESPACE"
}

check_storage_classes() {
    log "Checking storage classes..."
    
    if ! kubectl get storageclass fast-ssd &> /dev/null; then
        warning "Storage class 'fast-ssd' not found. Using 'default' instead."
    fi
    
    if ! kubectl get storageclass standard &> /dev/null; then
        warning "Storage class 'standard' not found. Using 'default' instead."
    fi
}

deploy_namespace() {
    log "Creating namespace if it doesn't exist..."
    
    if ! kubectl get namespace "$NAMESPACE" &> /dev/null; then
        if [ "$DRY_RUN" = true ]; then
            log "DRY RUN: Would create namespace $NAMESPACE"
        else
            kubectl create namespace "$NAMESPACE"
            success "Namespace $NAMESPACE created"
        fi
    else
        log "Namespace $NAMESPACE already exists"
    fi
}

deploy_application() {
    log "Deploying KGV application..."
    
    local overlay_path="$K8S_DIR/overlays/$ENVIRONMENT"
    local kustomize_args=""
    
    if [ "$DRY_RUN" = true ]; then
        kustomize_args="--dry-run=client"
    fi
    
    if [ "$VERBOSE" = true ]; then
        kubectl kustomize "$overlay_path"
    fi
    
    if [ "$DRY_RUN" = true ]; then
        log "DRY RUN: Would apply KGV application manifests"
        kubectl apply -k "$overlay_path" $kustomize_args
    else
        kubectl apply -k "$overlay_path" $kustomize_args
        success "KGV application deployed"
    fi
}

deploy_monitoring() {
    if [ "$APPLY_MONITORING" = false ]; then
        log "Skipping monitoring stack deployment"
        return
    fi
    
    log "Deploying monitoring stack..."
    
    local monitoring_path="$K8S_DIR/monitoring"
    local kustomize_args=""
    
    if [ "$DRY_RUN" = true ]; then
        kustomize_args="--dry-run=client"
    fi
    
    # Create monitoring namespace
    if ! kubectl get namespace kgv-monitoring &> /dev/null; then
        if [ "$DRY_RUN" = true ]; then
            log "DRY RUN: Would create namespace kgv-monitoring"
        else
            kubectl create namespace kgv-monitoring
            kubectl label namespace kgv-monitoring name=kgv-monitoring
        fi
    fi
    
    if [ "$DRY_RUN" = true ]; then
        log "DRY RUN: Would apply monitoring stack manifests"
    else
        kubectl apply -k "$monitoring_path" $kustomize_args
        success "Monitoring stack deployed"
    fi
}

deploy_backup() {
    if [ "$APPLY_BACKUP" = false ]; then
        log "Skipping backup jobs deployment"
        return
    fi
    
    log "Deploying backup jobs..."
    
    local backup_path="$K8S_DIR/backup"
    local kustomize_args=""
    
    if [ "$DRY_RUN" = true ]; then
        kustomize_args="--dry-run=client"
    fi
    
    if [ "$DRY_RUN" = true ]; then
        log "DRY RUN: Would apply backup job manifests"
    else
        kubectl apply -k "$backup_path" $kustomize_args
        success "Backup jobs deployed"
    fi
}

wait_for_rollout() {
    if [ "$DRY_RUN" = true ]; then
        log "DRY RUN: Would wait for deployment rollouts"
        return
    fi
    
    log "Waiting for deployment rollouts to complete..."
    
    local deployments=(
        "kgv-api"
        "kgv-web"
    )
    
    local statefulsets=(
        "postgres"
        "redis"
    )
    
    # Wait for deployments
    for deployment in "${deployments[@]}"; do
        local full_name="$deployment"
        if [ "$ENVIRONMENT" != "production" ]; then
            full_name="${ENVIRONMENT}-${deployment}"
        fi
        
        if kubectl get deployment "$full_name" -n "$NAMESPACE" &> /dev/null; then
            log "Waiting for deployment $full_name..."
            kubectl rollout status deployment/"$full_name" -n "$NAMESPACE" --timeout="${WAIT_TIMEOUT}s"
        fi
    done
    
    # Wait for statefulsets
    for statefulset in "${statefulsets[@]}"; do
        local full_name="$statefulset"
        if [ "$ENVIRONMENT" != "production" ]; then
            full_name="${ENVIRONMENT}-${statefulset}"
        fi
        
        if kubectl get statefulset "$full_name" -n "$NAMESPACE" &> /dev/null; then
            log "Waiting for statefulset $full_name..."
            kubectl rollout status statefulset/"$full_name" -n "$NAMESPACE" --timeout="${WAIT_TIMEOUT}s"
        fi
    done
    
    success "All rollouts completed successfully"
}

verify_deployment() {
    if [ "$DRY_RUN" = true ]; then
        log "DRY RUN: Would verify deployment"
        return
    fi
    
    log "Verifying deployment..."
    
    # Check pod status
    log "Pod status:"
    kubectl get pods -n "$NAMESPACE" -o wide
    
    # Check service status
    log "Service status:"
    kubectl get services -n "$NAMESPACE"
    
    # Check ingress status
    log "Ingress status:"
    kubectl get ingress -n "$NAMESPACE"
    
    # Check persistent volumes
    log "Persistent Volume Claims:"
    kubectl get pvc -n "$NAMESPACE"
    
    # Health checks
    log "Running health checks..."
    
    # Check if all pods are ready
    local not_ready_pods=$(kubectl get pods -n "$NAMESPACE" --field-selector=status.phase!=Running -o name | wc -l)
    if [ $not_ready_pods -gt 0 ]; then
        warning "$not_ready_pods pods are not in Running state"
        kubectl get pods -n "$NAMESPACE" --field-selector=status.phase!=Running
    else
        success "All pods are running"
    fi
}

cleanup_failed_resources() {
    if [ "$DRY_RUN" = true ]; then
        return
    fi
    
    log "Cleaning up failed resources..."
    
    # Remove failed pods
    kubectl delete pods -n "$NAMESPACE" --field-selector=status.phase=Failed --ignore-not-found=true
    
    # Remove completed jobs older than 1 day
    kubectl delete jobs -n "$NAMESPACE" --field-selector=status.successful=1 --ignore-not-found=true
}

show_deployment_info() {
    if [ "$DRY_RUN" = true ]; then
        return
    fi
    
    log "Deployment Information:"
    echo "=========================="
    echo "Environment: $ENVIRONMENT"
    echo "Namespace: $NAMESPACE"
    echo "Cluster: $(kubectl config current-context)"
    echo ""
    
    if [ "$ENVIRONMENT" = "development" ]; then
        echo "Access URLs (update /etc/hosts or use port-forwarding):"
        echo "  Application: http://dev.kgv.local"
        echo "  Port-forward: kubectl port-forward -n $NAMESPACE svc/dev-kgv-web 3000:80"
    else
        echo "Access URLs:"
        echo "  Application: https://kgv.example.com"
        echo "  API: https://api.kgv.example.com"
        if [ "$APPLY_MONITORING" = true ]; then
            echo "  Grafana: https://grafana.kgv.example.com"
            echo "  Prometheus: https://prometheus.kgv.example.com"
        fi
    fi
    
    echo ""
    echo "Useful commands:"
    echo "  View logs: kubectl logs -f deployment/kgv-api -n $NAMESPACE"
    echo "  Scale deployment: kubectl scale deployment kgv-api --replicas=3 -n $NAMESPACE"
    echo "  Update deployment: kubectl set image deployment/kgv-api api=new-image:tag -n $NAMESPACE"
    echo "  Port forward: kubectl port-forward -n $NAMESPACE svc/kgv-web 8080:80"
}

main() {
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            -e|--environment)
                ENVIRONMENT="$2"
                shift 2
                ;;
            -n|--namespace)
                NAMESPACE="$2"
                shift 2
                ;;
            -d|--dry-run)
                DRY_RUN=true
                shift
                ;;
            --no-monitoring)
                APPLY_MONITORING=false
                shift
                ;;
            --no-backup)
                APPLY_BACKUP=false
                shift
                ;;
            -t|--timeout)
                WAIT_TIMEOUT="$2"
                shift 2
                ;;
            -v|--verbose)
                VERBOSE=true
                shift
                ;;
            -h|--help)
                usage
                exit 0
                ;;
            *)
                error "Unknown option: $1"
                usage
                exit 1
                ;;
        esac
    done
    
    log "Starting KGV Kubernetes deployment..."
    
    check_prerequisites
    validate_environment
    check_storage_classes
    deploy_namespace
    deploy_application
    deploy_monitoring
    deploy_backup
    wait_for_rollout
    cleanup_failed_resources
    verify_deployment
    show_deployment_info
    
    success "KGV deployment completed successfully!"
}

# Run main function
main "$@"