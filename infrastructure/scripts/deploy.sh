#!/bin/bash
# KGV Migration - Deployment Script
# Purpose: Automated deployment script for KGV infrastructure and applications
# Usage: ./deploy.sh [environment] [action]

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"
TERRAFORM_DIR="${PROJECT_ROOT}/infrastructure/terraform"
KUBERNETES_DIR="${PROJECT_ROOT}/infrastructure/kubernetes"

# Functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

usage() {
    cat << EOF
Usage: $0 [environment] [action]

Environments:
  dev         Development environment
  staging     Staging environment
  prod        Production environment

Actions:
  validate    Validate infrastructure code
  plan        Show infrastructure changes
  apply       Apply infrastructure changes
  destroy     Destroy infrastructure (dev/staging only)
  deploy-app  Deploy application containers
  rollback    Rollback to previous version
  status      Show deployment status
  test        Run smoke tests

Examples:
  $0 dev validate
  $0 staging plan
  $0 prod apply
  $0 staging deploy-app

EOF
    exit 1
}

# Validate environment
validate_environment() {
    local env=$1
    if [[ ! "$env" =~ ^(dev|staging|prod)$ ]]; then
        log_error "Invalid environment: $env"
        usage
    fi
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check for required tools
    local required_tools=("terraform" "az" "kubectl" "docker" "jq")
    for tool in "${required_tools[@]}"; do
        if ! command -v "$tool" &> /dev/null; then
            log_error "$tool is not installed"
            exit 1
        fi
    done
    
    # Check Azure CLI login
    if ! az account show &> /dev/null; then
        log_error "Not logged in to Azure. Please run 'az login'"
        exit 1
    fi
    
    log_info "All prerequisites met"
}

# Terraform operations
terraform_validate() {
    local env=$1
    log_info "Validating Terraform configuration for $env..."
    
    cd "$TERRAFORM_DIR"
    terraform init -backend=false
    terraform fmt -check -recursive
    terraform validate
    
    log_info "Terraform validation successful"
}

terraform_plan() {
    local env=$1
    log_info "Planning Terraform changes for $env..."
    
    cd "$TERRAFORM_DIR"
    
    # Initialize with backend
    terraform init \
        -backend-config="key=kgv-${env}.tfstate"
    
    # Create plan
    terraform plan \
        -var="environment=${env}" \
        -var-file="terraform.${env}.tfvars" \
        -out="${env}.tfplan"
    
    log_info "Terraform plan created: ${env}.tfplan"
}

terraform_apply() {
    local env=$1
    
    # Production requires confirmation
    if [[ "$env" == "prod" ]]; then
        log_warn "You are about to deploy to PRODUCTION!"
        read -p "Type 'yes' to continue: " -r
        if [[ ! $REPLY == "yes" ]]; then
            log_error "Production deployment cancelled"
            exit 1
        fi
    fi
    
    log_info "Applying Terraform changes for $env..."
    
    cd "$TERRAFORM_DIR"
    
    # Check if plan exists
    if [[ ! -f "${env}.tfplan" ]]; then
        log_error "No plan file found. Run 'plan' first."
        exit 1
    fi
    
    # Apply the plan
    terraform apply "${env}.tfplan"
    
    # Save outputs
    terraform output -json > "${env}-outputs.json"
    
    log_info "Terraform apply completed successfully"
}

terraform_destroy() {
    local env=$1
    
    # Prevent production destroy
    if [[ "$env" == "prod" ]]; then
        log_error "Cannot destroy production environment"
        exit 1
    fi
    
    log_warn "You are about to DESTROY the $env environment!"
    read -p "Type 'destroy-${env}' to continue: " -r
    if [[ ! $REPLY == "destroy-${env}" ]]; then
        log_error "Destroy cancelled"
        exit 1
    fi
    
    log_info "Destroying infrastructure for $env..."
    
    cd "$TERRAFORM_DIR"
    
    terraform init \
        -backend-config="key=kgv-${env}.tfstate"
    
    terraform destroy \
        -var="environment=${env}" \
        -var-file="terraform.${env}.tfvars" \
        -auto-approve
    
    log_info "Infrastructure destroyed"
}

# Application deployment
deploy_application() {
    local env=$1
    log_info "Deploying application to $env..."
    
    # Get infrastructure outputs
    cd "$TERRAFORM_DIR"
    if [[ ! -f "${env}-outputs.json" ]]; then
        log_error "No infrastructure outputs found. Run 'apply' first."
        exit 1
    fi
    
    local registry=$(jq -r '.container_registry.value' "${env}-outputs.json")
    local resource_group=$(jq -r '.resource_group_name.value' "${env}-outputs.json")
    
    # Build and push Docker images
    log_info "Building Docker images..."
    
    # API
    docker build -t "${registry}/kgv-api:${env}-${GITHUB_SHA:-latest}" \
        -f "${PROJECT_ROOT}/src/KGV.Api/Dockerfile" \
        "${PROJECT_ROOT}/src/KGV.Api"
    
    # Web
    docker build -t "${registry}/kgv-web:${env}-${GITHUB_SHA:-latest}" \
        -f "${PROJECT_ROOT}/src/KGV.Web/Dockerfile" \
        "${PROJECT_ROOT}/src/KGV.Web"
    
    # Push images
    log_info "Pushing images to registry..."
    az acr login --name "${registry%%.*}"
    docker push "${registry}/kgv-api:${env}-${GITHUB_SHA:-latest}"
    docker push "${registry}/kgv-web:${env}-${GITHUB_SHA:-latest}"
    
    # Update Container Apps
    log_info "Updating Container Apps..."
    
    az containerapp update \
        --name "ca-kgv-api-${env}" \
        --resource-group "$resource_group" \
        --image "${registry}/kgv-api:${env}-${GITHUB_SHA:-latest}"
    
    az containerapp update \
        --name "ca-kgv-web-${env}" \
        --resource-group "$resource_group" \
        --image "${registry}/kgv-web:${env}-${GITHUB_SHA:-latest}"
    
    log_info "Application deployed successfully"
}

# Rollback deployment
rollback_deployment() {
    local env=$1
    log_info "Rolling back deployment in $env..."
    
    cd "$TERRAFORM_DIR"
    local resource_group=$(jq -r '.resource_group_name.value' "${env}-outputs.json")
    
    # Get previous revision for API
    local api_revision=$(az containerapp revision list \
        --name "ca-kgv-api-${env}" \
        --resource-group "$resource_group" \
        --query "[1].name" -o tsv)
    
    # Get previous revision for Web
    local web_revision=$(az containerapp revision list \
        --name "ca-kgv-web-${env}" \
        --resource-group "$resource_group" \
        --query "[1].name" -o tsv)
    
    # Activate previous revisions
    az containerapp revision activate \
        --name "ca-kgv-api-${env}" \
        --resource-group "$resource_group" \
        --revision "$api_revision"
    
    az containerapp revision activate \
        --name "ca-kgv-web-${env}" \
        --resource-group "$resource_group" \
        --revision "$web_revision"
    
    log_info "Rollback completed"
}

# Show deployment status
show_status() {
    local env=$1
    log_info "Deployment status for $env environment:"
    
    cd "$TERRAFORM_DIR"
    if [[ ! -f "${env}-outputs.json" ]]; then
        log_warn "No infrastructure deployed"
        return
    fi
    
    local resource_group=$(jq -r '.resource_group_name.value' "${env}-outputs.json")
    local app_url=$(jq -r '.application_gateway_url.value' "${env}-outputs.json")
    
    echo ""
    echo "Resource Group: $resource_group"
    echo "Application URL: $app_url"
    echo ""
    
    # Check Container Apps status
    log_info "Container Apps Status:"
    az containerapp show \
        --name "ca-kgv-api-${env}" \
        --resource-group "$resource_group" \
        --query "{name:name, status:properties.provisioningState, replicas:properties.template.scale.maxReplicas}" \
        -o table
    
    az containerapp show \
        --name "ca-kgv-web-${env}" \
        --resource-group "$resource_group" \
        --query "{name:name, status:properties.provisioningState, replicas:properties.template.scale.maxReplicas}" \
        -o table
    
    # Check health endpoints
    log_info "Health Check:"
    if curl -sf "${app_url}/api/health" > /dev/null; then
        echo "✓ API is healthy"
    else
        echo "✗ API health check failed"
    fi
    
    if curl -sf "${app_url}" > /dev/null; then
        echo "✓ Web is healthy"
    else
        echo "✗ Web health check failed"
    fi
}

# Run smoke tests
run_tests() {
    local env=$1
    log_info "Running smoke tests for $env..."
    
    cd "$TERRAFORM_DIR"
    local app_url=$(jq -r '.application_gateway_url.value' "${env}-outputs.json")
    
    # Basic connectivity tests
    log_info "Testing API endpoints..."
    
    # Health check
    curl -sf "${app_url}/api/health" || log_error "Health check failed"
    
    # API version
    curl -sf "${app_url}/api/version" || log_error "Version check failed"
    
    # Web application
    curl -sf "${app_url}" || log_error "Web application check failed"
    
    log_info "Smoke tests completed"
}

# Main execution
main() {
    if [[ $# -lt 2 ]]; then
        usage
    fi
    
    local environment=$1
    local action=$2
    
    validate_environment "$environment"
    check_prerequisites
    
    case "$action" in
        validate)
            terraform_validate "$environment"
            ;;
        plan)
            terraform_plan "$environment"
            ;;
        apply)
            terraform_apply "$environment"
            ;;
        destroy)
            terraform_destroy "$environment"
            ;;
        deploy-app)
            deploy_application "$environment"
            ;;
        rollback)
            rollback_deployment "$environment"
            ;;
        status)
            show_status "$environment"
            ;;
        test)
            run_tests "$environment"
            ;;
        *)
            log_error "Invalid action: $action"
            usage
            ;;
    esac
    
    log_info "Operation completed successfully"
}

# Run main function
main "$@"