# KGV Migration - Main Terraform Configuration
# Provider: Azure (Deutschland-Zentrale für GDPR-Compliance)
# Last Updated: 2025-08-04

terraform {
  required_version = ">= 1.5.0"
  
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.85.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 2.47.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6.0"
    }
  }
  
  # State Management in Azure Storage
  backend "azurerm" {
    resource_group_name  = "rg-kgv-terraform-state"
    storage_account_name = "stkgvterraformstate"
    container_name      = "tfstate"
    key                 = "kgv-migration.tfstate"
  }
}

# Azure Provider Configuration
provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = true
    }
    key_vault {
      purge_soft_delete_on_destroy = false
      recover_soft_deleted_key_vaults = true
    }
  }
}

# Variables
variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be dev, staging, or prod."
  }
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "germanywestcentral" # Frankfurt Region for GDPR Compliance
}

variable "location_secondary" {
  description = "Secondary Azure region for disaster recovery"
  type        = string
  default     = "germanynorth" # Berlin Region for DR
}

# Locals for naming conventions
locals {
  prefix = "kgv"
  suffix = var.environment
  
  common_tags = {
    Project     = "KGV-Migration"
    Environment = var.environment
    ManagedBy   = "Terraform"
    CostCenter  = "IT-Modernization"
    Compliance  = "GDPR"
    Owner       = "Stadt-Frankfurt"
  }
  
  # Cost optimization settings
  sku_sizes = {
    dev = {
      app_service_plan = "B2"
      database        = "GP_Gen5_2"
      redis           = "C1"
      container_cpu   = "0.5"
      container_memory = "1"
    }
    staging = {
      app_service_plan = "S2"
      database        = "GP_Gen5_4"
      redis           = "C2"
      container_cpu   = "1"
      container_memory = "2"
    }
    prod = {
      app_service_plan = "P1v3"
      database        = "GP_Gen5_8"
      redis           = "P1"
      container_cpu   = "2"
      container_memory = "4"
    }
  }
}

# Resource Groups
resource "azurerm_resource_group" "main" {
  name     = "rg-${local.prefix}-${local.suffix}"
  location = var.location
  tags     = local.common_tags
}

resource "azurerm_resource_group" "monitoring" {
  name     = "rg-${local.prefix}-monitoring-${local.suffix}"
  location = var.location
  tags     = local.common_tags
}

# Networking Module
module "networking" {
  source = "./modules/networking"
  
  resource_group_name = azurerm_resource_group.main.name
  location           = var.location
  environment        = var.environment
  prefix            = local.prefix
  tags              = local.common_tags
}

# Security Module (Key Vault, Managed Identities)
module "security" {
  source = "./modules/security"
  
  resource_group_name = azurerm_resource_group.main.name
  location           = var.location
  environment        = var.environment
  prefix            = local.prefix
  tags              = local.common_tags
  
  tenant_id = data.azurerm_client_config.current.tenant_id
}

# Database Module (PostgreSQL Flexible Server)
module "database" {
  source = "./modules/database"
  
  resource_group_name = azurerm_resource_group.main.name
  location           = var.location
  environment        = var.environment
  prefix            = local.prefix
  tags              = local.common_tags
  
  subnet_id               = module.networking.database_subnet_id
  private_dns_zone_id     = module.networking.private_dns_zone_postgresql_id
  administrator_login     = "kgvadmin"
  sku_name               = local.sku_sizes[var.environment].database
  storage_mb             = var.environment == "prod" ? 256 : 128
  backup_retention_days  = var.environment == "prod" ? 30 : 7
  geo_redundant_backup   = var.environment == "prod" ? true : false
  high_availability      = var.environment == "prod" ? true : false
  
  key_vault_id = module.security.key_vault_id
}

# Redis Cache Module
module "redis" {
  source = "./modules/redis"
  
  resource_group_name = azurerm_resource_group.main.name
  location           = var.location
  environment        = var.environment
  prefix            = local.prefix
  tags              = local.common_tags
  
  subnet_id        = module.networking.redis_subnet_id
  sku_name        = local.sku_sizes[var.environment].redis
  enable_non_ssl  = false
  minimum_tls_version = "1.2"
}

# Container Apps Environment Module
module "container_apps" {
  source = "./modules/container_apps"
  
  resource_group_name = azurerm_resource_group.main.name
  location           = var.location
  environment        = var.environment
  prefix            = local.prefix
  tags              = local.common_tags
  
  subnet_id                = module.networking.container_apps_subnet_id
  log_analytics_workspace_id = module.monitoring.log_analytics_workspace_id
}

# Application Deployment Module
module "applications" {
  source = "./modules/applications"
  
  resource_group_name = azurerm_resource_group.main.name
  location           = var.location
  environment        = var.environment
  prefix            = local.prefix
  tags              = local.common_tags
  
  container_app_environment_id = module.container_apps.environment_id
  container_registry_server    = module.container_apps.registry_server
  container_registry_username  = module.container_apps.registry_username
  container_registry_password  = module.container_apps.registry_password
  
  # Application configurations
  api_image     = "${module.container_apps.registry_server}/kgv-api:${var.environment}"
  web_image     = "${module.container_apps.registry_server}/kgv-web:${var.environment}"
  
  api_cpu       = local.sku_sizes[var.environment].container_cpu
  api_memory    = local.sku_sizes[var.environment].container_memory
  web_cpu       = local.sku_sizes[var.environment].container_cpu
  web_memory    = local.sku_sizes[var.environment].container_memory
  
  database_connection_string = module.database.connection_string
  redis_connection_string    = module.redis.connection_string
  key_vault_uri             = module.security.key_vault_uri
  
  min_replicas = var.environment == "prod" ? 2 : 1
  max_replicas = var.environment == "prod" ? 10 : 3
}

# Application Gateway (Load Balancer) Module
module "application_gateway" {
  source = "./modules/application_gateway"
  
  resource_group_name = azurerm_resource_group.main.name
  location           = var.location
  environment        = var.environment
  prefix            = local.prefix
  tags              = local.common_tags
  
  subnet_id           = module.networking.application_gateway_subnet_id
  public_ip_id        = module.networking.public_ip_id
  
  backend_addresses = [
    module.applications.api_fqdn,
    module.applications.web_fqdn
  ]
  
  ssl_certificate_name     = "kgv-frankfurt-de"
  key_vault_id            = module.security.key_vault_id
  enable_waf              = var.environment == "prod" ? true : false
  enable_autoscaling      = var.environment == "prod" ? true : false
}

# Monitoring Module
module "monitoring" {
  source = "./modules/monitoring"
  
  resource_group_name = azurerm_resource_group.monitoring.name
  location           = var.location
  environment        = var.environment
  prefix            = local.prefix
  tags              = local.common_tags
  
  retention_in_days = var.environment == "prod" ? 90 : 30
}

# Alerts Module
module "alerts" {
  source = "./modules/alerts"
  
  resource_group_name = azurerm_resource_group.monitoring.name
  location           = var.location
  environment        = var.environment
  prefix            = local.prefix
  tags              = local.common_tags
  
  action_group_email = var.environment == "prod" ? "kgv-ops@frankfurt.de" : "kgv-dev@frankfurt.de"
  
  # Resources to monitor
  app_insights_id     = module.monitoring.application_insights_id
  database_id        = module.database.server_id
  redis_id           = module.redis.cache_id
  container_apps_ids = module.applications.container_app_ids
}

# Backup Module
module "backup" {
  source = "./modules/backup"
  
  resource_group_name = azurerm_resource_group.main.name
  location           = var.location
  environment        = var.environment
  prefix            = local.prefix
  tags              = local.common_tags
  
  # Backup policies
  database_backup_enabled = true
  daily_backup_retention  = var.environment == "prod" ? 30 : 7
  weekly_backup_retention = var.environment == "prod" ? 12 : 4
  monthly_backup_retention = var.environment == "prod" ? 12 : 3
  
  database_id = module.database.server_id
}

# Traffic Manager for Multi-Region (Production Only)
module "traffic_manager" {
  count  = var.environment == "prod" ? 1 : 0
  source = "./modules/traffic_manager"
  
  resource_group_name = azurerm_resource_group.main.name
  environment        = var.environment
  prefix            = local.prefix
  tags              = local.common_tags
  
  primary_endpoint   = module.application_gateway.public_ip_fqdn
  secondary_endpoint = "" # Will be configured when DR region is deployed
}

# Data Sources
data "azurerm_client_config" "current" {}

# Outputs
output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "application_gateway_url" {
  value = "https://${module.application_gateway.public_ip_fqdn}"
}

output "api_url" {
  value = "https://${module.applications.api_fqdn}"
}

output "web_url" {
  value = "https://${module.applications.web_fqdn}"
}

output "database_server" {
  value     = module.database.server_fqdn
  sensitive = true
}

output "key_vault_uri" {
  value = module.security.key_vault_uri
}

output "container_registry" {
  value = module.container_apps.registry_server
}

output "monthly_cost_estimate" {
  value = {
    environment = var.environment
    currency    = "EUR"
    estimated_cost = var.environment == "prod" ? "2,850" : var.environment == "staging" ? "1,420" : "580"
    breakdown = {
      container_apps   = var.environment == "prod" ? "800" : "400"
      database        = var.environment == "prod" ? "1,200" : "600"
      redis           = var.environment == "prod" ? "250" : "120"
      networking      = var.environment == "prod" ? "200" : "100"
      monitoring      = var.environment == "prod" ? "150" : "75"
      storage_backup  = var.environment == "prod" ? "250" : "125"
    }
  }
  description = "Estimated monthly costs in EUR (based on Azure Germany pricing)"
}