#!/bin/bash

# KGV Kubernetes Management Script
# This script provides various management operations for the KGV application

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$(dirname "$SCRIPT_DIR")"

# Default values
ENVIRONMENT="production"
NAMESPACE=""
OPERATION=""
COMPONENT=""
REPLICAS=""

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
Usage: $0 <operation> [OPTIONS]

Manage KGV application on Kubernetes cluster

OPERATIONS:
    status          Show cluster and application status
    logs            View application logs
    scale           Scale deployments
    backup          Manage backups
    restore         Restore from backup
    update          Update application images
    troubleshoot    Run troubleshooting commands
    cleanup         Clean up resources

OPTIONS:
    -e, --environment   Environment (development|staging|production) [default: production]
    -n, --namespace     Override default namespace
    -c, --component     Component name (api|web|postgres|redis|monitoring)
    -r, --replicas      Number of replicas for scaling
    -h, --help          Show this help message

EXAMPLES:
    $0 status -e development                    # Show development environment status
    $0 logs -c api -e production               # View API logs in production
    $0 scale -c web -r 5 -e production        # Scale web component to 5 replicas
    $0 backup --list                           # List available backups
    $0 backup --create                         # Create manual backup
    $0 restore --file backup_20231201.sql     # Restore from specific backup
    $0 update -c api --image new-tag          # Update API image
    $0 troubleshoot -c postgres               # Troubleshoot PostgreSQL issues

EOF
}

validate_environment() {
    case $ENVIRONMENT in
        development|staging|production)
            ;;
        *)
            error "Invalid environment: $ENVIRONMENT"
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
}

check_namespace() {
    if ! kubectl get namespace "$NAMESPACE" &> /dev/null; then
        error "Namespace $NAMESPACE does not exist"
        exit 1
    fi
}

show_status() {
    log "KGV Application Status - Environment: $ENVIRONMENT"
    echo "=================================================="
    
    echo ""
    echo "Cluster Information:"
    echo "-------------------"
    kubectl cluster-info
    
    echo ""
    echo "Namespace: $NAMESPACE"
    echo "-------------------"
    kubectl get namespace "$NAMESPACE" -o wide
    
    echo ""
    echo "Pods:"
    echo "-----"
    kubectl get pods -n "$NAMESPACE" -o wide
    
    echo ""
    echo "Services:"
    echo "---------"
    kubectl get services -n "$NAMESPACE" -o wide
    
    echo ""
    echo "Ingress:"
    echo "--------"
    kubectl get ingress -n "$NAMESPACE" -o wide
    
    echo ""
    echo "Persistent Volume Claims:"
    echo "------------------------"
    kubectl get pvc -n "$NAMESPACE" -o wide
    
    echo ""
    echo "Horizontal Pod Autoscalers:"
    echo "---------------------------"
    kubectl get hpa -n "$NAMESPACE" -o wide
    
    echo ""
    echo "Deployments:"
    echo "------------"
    kubectl get deployments -n "$NAMESPACE" -o wide
    
    echo ""
    echo "StatefulSets:"
    echo "-------------"
    kubectl get statefulsets -n "$NAMESPACE" -o wide
    
    if kubectl get namespace kgv-monitoring &> /dev/null; then
        echo ""
        echo "Monitoring (kgv-monitoring namespace):"
        echo "--------------------------------------"
        kubectl get pods -n kgv-monitoring -o wide
    fi
}

show_logs() {
    if [ -z "$COMPONENT" ]; then
        error "Component not specified. Use -c or --component"
        exit 1
    fi
    
    local deployment_name="$COMPONENT"
    if [ "$ENVIRONMENT" != "production" ]; then
        deployment_name="${ENVIRONMENT}-${COMPONENT}"
    fi
    
    case $COMPONENT in
        api)
            deployment_name="${deployment_name/-api/kgv-api}"
            ;;
        web)
            deployment_name="${deployment_name/-web/kgv-web}"
            ;;
        postgres|redis)
            log "Showing logs for StatefulSet $deployment_name"
            kubectl logs -f statefulset/"$deployment_name" -n "$NAMESPACE"
            return
            ;;
        monitoring)
            log "Showing logs for monitoring components"
            kubectl logs -f deployment/prometheus -n kgv-monitoring
            return
            ;;
        *)
            error "Unknown component: $COMPONENT"
            exit 1
            ;;
    esac
    
    log "Showing logs for deployment $deployment_name"
    kubectl logs -f deployment/"$deployment_name" -n "$NAMESPACE"
}

scale_component() {
    if [ -z "$COMPONENT" ]; then
        error "Component not specified. Use -c or --component"
        exit 1
    fi
    
    if [ -z "$REPLICAS" ]; then
        error "Replicas not specified. Use -r or --replicas"
        exit 1
    fi
    
    local deployment_name="$COMPONENT"
    if [ "$ENVIRONMENT" != "production" ]; then
        deployment_name="${ENVIRONMENT}-${COMPONENT}"
    fi
    
    case $COMPONENT in
        api)
            deployment_name="${deployment_name/-api/kgv-api}"
            ;;
        web)
            deployment_name="${deployment_name/-web/kgv-web}"
            ;;
        postgres|redis)
            warning "StatefulSets should not be scaled automatically. Consider the implications."
            read -p "Are you sure you want to scale $COMPONENT to $REPLICAS replicas? (y/N): " -n 1 -r
            echo
            if [[ ! $REPLY =~ ^[Yy]$ ]]; then
                log "Scaling cancelled"
                return
            fi
            kubectl scale statefulset/"$deployment_name" --replicas="$REPLICAS" -n "$NAMESPACE"
            return
            ;;
        *)
            error "Unknown component: $COMPONENT"
            exit 1
            ;;
    esac
    
    log "Scaling deployment $deployment_name to $REPLICAS replicas"
    kubectl scale deployment/"$deployment_name" --replicas="$REPLICAS" -n "$NAMESPACE"
    
    log "Waiting for rollout to complete..."
    kubectl rollout status deployment/"$deployment_name" -n "$NAMESPACE"
    
    success "Scaling completed successfully"
}

manage_backups() {
    local backup_action="$1"
    
    case $backup_action in
        --list)
            log "Available backups:"
            kubectl exec -n "$NAMESPACE" deployment/postgres -- \
                find /backups/postgres -name "kgv_backup_*.sql*" -type f | \
                sort -r | head -20
            ;;
        --create)
            log "Creating manual backup..."
            kubectl create job postgres-manual-backup-$(date +%s) \
                --from=cronjob/postgres-backup -n "$NAMESPACE"
            success "Manual backup job created"
            ;;
        --status)
            log "Backup job status:"
            kubectl get cronjobs -n "$NAMESPACE" | grep backup
            kubectl get jobs -n "$NAMESPACE" | grep backup
            ;;
        *)
            error "Unknown backup action: $backup_action"
            echo "Available actions: --list, --create, --status"
            exit 1
            ;;
    esac
}

restore_backup() {
    local backup_file="$1"
    
    if [ -z "$backup_file" ]; then
        error "Backup file not specified"
        echo "Usage: $0 restore --file <backup-file-path>"
        exit 1
    fi
    
    warning "This will restore the database from backup: $backup_file"
    warning "This operation may cause data loss!"
    read -p "Are you sure you want to proceed? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log "Restore cancelled"
        return
    fi
    
    log "Creating restore job..."
    
    # Create restore job from template
    kubectl create job postgres-restore-$(date +%s) \
        --from=job/postgres-restore \
        --dry-run=client -o yaml | \
        sed "s|/backups/postgres/backup_to_restore.sql.custom|$backup_file|g" | \
        kubectl apply -f -
    
    success "Restore job created. Monitor with: kubectl logs -f job/postgres-restore-<timestamp> -n $NAMESPACE"
}

update_component() {
    local image_tag="$1"
    
    if [ -z "$COMPONENT" ]; then
        error "Component not specified. Use -c or --component"
        exit 1
    fi
    
    if [ -z "$image_tag" ]; then
        error "Image tag not specified"
        echo "Usage: $0 update -c <component> --image <image:tag>"
        exit 1
    fi
    
    local deployment_name="$COMPONENT"
    if [ "$ENVIRONMENT" != "production" ]; then
        deployment_name="${ENVIRONMENT}-${COMPONENT}"
    fi
    
    case $COMPONENT in
        api)
            deployment_name="${deployment_name/-api/kgv-api}"
            local container_name="api"
            ;;
        web)
            deployment_name="${deployment_name/-web/kgv-web}"
            local container_name="web"
            ;;
        *)
            error "Component $COMPONENT cannot be updated using this method"
            exit 1
            ;;
    esac
    
    log "Updating $deployment_name with image: $image_tag"
    kubectl set image deployment/"$deployment_name" "$container_name=$image_tag" -n "$NAMESPACE"
    
    log "Waiting for rollout to complete..."
    kubectl rollout status deployment/"$deployment_name" -n "$NAMESPACE"
    
    success "Update completed successfully"
}

troubleshoot() {
    if [ -z "$COMPONENT" ]; then
        log "Running general troubleshooting..."
        
        echo ""
        echo "Failed Pods:"
        echo "------------"
        kubectl get pods -n "$NAMESPACE" --field-selector=status.phase=Failed
        
        echo ""
        echo "Pending Pods:"
        echo "-------------"
        kubectl get pods -n "$NAMESPACE" --field-selector=status.phase=Pending
        
        echo ""
        echo "Recent Events:"
        echo "--------------"
        kubectl get events -n "$NAMESPACE" --sort-by='.lastTimestamp' | tail -20
        
        echo ""
        echo "Resource Usage:"
        echo "---------------"
        kubectl top pods -n "$NAMESPACE" 2>/dev/null || echo "Metrics server not available"
        
        return
    fi
    
    local resource_name="$COMPONENT"
    if [ "$ENVIRONMENT" != "production" ]; then
        resource_name="${ENVIRONMENT}-${COMPONENT}"
    fi
    
    case $COMPONENT in
        api)
            resource_name="${resource_name/-api/kgv-api}"
            resource_type="deployment"
            ;;
        web)
            resource_name="${resource_name/-web/kgv-web}"
            resource_type="deployment"
            ;;
        postgres|redis)
            resource_type="statefulset"
            ;;
        *)
            error "Unknown component: $COMPONENT"
            exit 1
            ;;
    esac
    
    log "Troubleshooting $COMPONENT ($resource_name)"
    
    echo ""
    echo "Resource Status:"
    echo "----------------"
    kubectl describe "$resource_type"/"$resource_name" -n "$NAMESPACE"
    
    echo ""
    echo "Pod Status:"
    echo "-----------"
    kubectl get pods -n "$NAMESPACE" -l app.kubernetes.io/name="$COMPONENT" -o wide
    
    echo ""
    echo "Recent Logs:"
    echo "------------"
    kubectl logs --tail=50 "$resource_type"/"$resource_name" -n "$NAMESPACE"
    
    echo ""
    echo "Events:"
    echo "-------"
    kubectl get events -n "$NAMESPACE" --field-selector involvedObject.name="$resource_name"
}

cleanup_resources() {
    log "Cleaning up failed and completed resources..."
    
    # Remove failed pods
    kubectl delete pods -n "$NAMESPACE" --field-selector=status.phase=Failed --ignore-not-found=true
    
    # Remove completed jobs older than 1 day
    kubectl delete jobs -n "$NAMESPACE" --field-selector=status.successful=1 --ignore-not-found=true
    
    # Remove evicted pods
    kubectl get pods -n "$NAMESPACE" | grep Evicted | awk '{print $1}' | xargs -r kubectl delete pod -n "$NAMESPACE"
    
    success "Cleanup completed"
}

main() {
    if [ $# -eq 0 ]; then
        usage
        exit 1
    fi
    
    OPERATION="$1"
    shift
    
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
            -c|--component)
                COMPONENT="$2"
                shift 2
                ;;
            -r|--replicas)
                REPLICAS="$2"
                shift 2
                ;;
            --image)
                IMAGE_TAG="$2"
                shift 2
                ;;
            --file)
                BACKUP_FILE="$2"
                shift 2
                ;;
            --list|--create|--status)
                BACKUP_ACTION="$1"
                shift
                ;;
            -h|--help)
                usage
                exit 0
                ;;
            *)
                if [[ "$1" == --* ]]; then
                    EXTRA_ARGS="$1"
                    shift
                else
                    error "Unknown option: $1"
                    usage
                    exit 1
                fi
                ;;
        esac
    done
    
    validate_environment
    check_namespace
    
    case $OPERATION in
        status)
            show_status
            ;;
        logs)
            show_logs
            ;;
        scale)
            scale_component
            ;;
        backup)
            manage_backups "$BACKUP_ACTION"
            ;;
        restore)
            restore_backup "$BACKUP_FILE"
            ;;
        update)
            update_component "$IMAGE_TAG"
            ;;
        troubleshoot)
            troubleshoot
            ;;
        cleanup)
            cleanup_resources
            ;;
        *)
            error "Unknown operation: $OPERATION"
            usage
            exit 1
            ;;
    esac
}

# Run main function
main "$@"