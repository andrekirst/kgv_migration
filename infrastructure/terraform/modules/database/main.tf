# Database Module - PostgreSQL Flexible Server with HA and Backup
# Implements production-grade database with security and compliance

variable "resource_group_name" {}
variable "location" {}
variable "environment" {}
variable "prefix" {}
variable "tags" {}
variable "subnet_id" {}
variable "private_dns_zone_id" {}
variable "administrator_login" {}
variable "sku_name" {}
variable "storage_mb" {}
variable "backup_retention_days" {}
variable "geo_redundant_backup" {}
variable "high_availability" {}
variable "key_vault_id" {}

# Generate secure password
resource "random_password" "db_password" {
  length  = 32
  special = true
  upper   = true
  lower   = true
  numeric = true
}

# PostgreSQL Flexible Server
resource "azurerm_postgresql_flexible_server" "main" {
  name                = "psql-${var.prefix}-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  
  administrator_login    = var.administrator_login
  administrator_password = random_password.db_password.result
  
  sku_name                     = var.sku_name
  storage_mb                   = var.storage_mb * 1024
  version                      = "15"
  zone                         = var.environment == "prod" ? "1" : null
  backup_retention_days        = var.backup_retention_days
  geo_redundant_backup_enabled = var.geo_redundant_backup
  
  # High Availability Configuration
  dynamic "high_availability" {
    for_each = var.high_availability ? [1] : []
    content {
      mode                      = "ZoneRedundant"
      standby_availability_zone = "2"
    }
  }
  
  # Maintenance Window (Sunday 2-4 AM)
  maintenance_window {
    day_of_week  = 0
    start_hour   = 2
    start_minute = 0
  }
  
  tags = var.tags
}

# Network Configuration
resource "azurerm_postgresql_flexible_server_configuration" "require_secure_transport" {
  name      = "require_secure_transport"
  server_id = azurerm_postgresql_flexible_server.main.id
  value     = "on"
}

resource "azurerm_postgresql_flexible_server_configuration" "log_connections" {
  name      = "log_connections"
  server_id = azurerm_postgresql_flexible_server.main.id
  value     = "on"
}

resource "azurerm_postgresql_flexible_server_configuration" "log_disconnections" {
  name      = "log_disconnections"
  server_id = azurerm_postgresql_flexible_server.main.id
  value     = "on"
}

resource "azurerm_postgresql_flexible_server_configuration" "connection_throttling" {
  name      = "connection_throttle.enable"
  server_id = azurerm_postgresql_flexible_server.main.id
  value     = "on"
}

# Performance Tuning
resource "azurerm_postgresql_flexible_server_configuration" "shared_buffers" {
  name      = "shared_buffers"
  server_id = azurerm_postgresql_flexible_server.main.id
  value     = var.environment == "prod" ? "2097152" : "524288" # 2GB for prod, 512MB for dev
}

resource "azurerm_postgresql_flexible_server_configuration" "max_connections" {
  name      = "max_connections"
  server_id = azurerm_postgresql_flexible_server.main.id
  value     = var.environment == "prod" ? "200" : "100"
}

resource "azurerm_postgresql_flexible_server_configuration" "effective_cache_size" {
  name      = "effective_cache_size"
  server_id = azurerm_postgresql_flexible_server.main.id
  value     = var.environment == "prod" ? "6291456" : "1572864" # 6GB for prod, 1.5GB for dev
}

# Databases
resource "azurerm_postgresql_flexible_server_database" "kgv" {
  name      = "kgv_${var.environment}"
  server_id = azurerm_postgresql_flexible_server.main.id
  collation = "de_DE.utf8"
  charset   = "UTF8"
}

resource "azurerm_postgresql_flexible_server_database" "kgv_legacy" {
  name      = "kgv_legacy_${var.environment}"
  server_id = azurerm_postgresql_flexible_server.main.id
  collation = "de_DE.utf8"
  charset   = "UTF8"
}

# Firewall Rules
resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_postgresql_flexible_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Store credentials in Key Vault
resource "azurerm_key_vault_secret" "db_connection_string" {
  name         = "db-connection-string-${var.environment}"
  value        = "Host=${azurerm_postgresql_flexible_server.main.fqdn};Database=kgv_${var.environment};Username=${var.administrator_login};Password=${random_password.db_password.result};SSL Mode=Require;Trust Server Certificate=true"
  key_vault_id = var.key_vault_id
  
  content_type = "PostgreSQL Connection String"
  
  tags = merge(var.tags, {
    Type = "Database"
  })
}

resource "azurerm_key_vault_secret" "db_password" {
  name         = "db-password-${var.environment}"
  value        = random_password.db_password.result
  key_vault_id = var.key_vault_id
  
  content_type = "Password"
  
  tags = merge(var.tags, {
    Type = "Database"
  })
}

# Alert Rules for Database
resource "azurerm_monitor_metric_alert" "database_cpu" {
  name                = "alert-db-cpu-${var.environment}"
  resource_group_name = var.resource_group_name
  scopes              = [azurerm_postgresql_flexible_server.main.id]
  description         = "Alert when database CPU exceeds 80%"
  severity            = 2
  frequency           = "PT5M"
  window_size         = "PT15M"
  
  criteria {
    metric_namespace = "Microsoft.DBforPostgreSQL/flexibleServers"
    metric_name      = "cpu_percent"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 80
  }
  
  tags = var.tags
}

resource "azurerm_monitor_metric_alert" "database_storage" {
  name                = "alert-db-storage-${var.environment}"
  resource_group_name = var.resource_group_name
  scopes              = [azurerm_postgresql_flexible_server.main.id]
  description         = "Alert when database storage exceeds 85%"
  severity            = 2
  frequency           = "PT15M"
  window_size         = "PT1H"
  
  criteria {
    metric_namespace = "Microsoft.DBforPostgreSQL/flexibleServers"
    metric_name      = "storage_percent"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 85
  }
  
  tags = var.tags
}

# Diagnostic Settings
resource "azurerm_monitor_diagnostic_setting" "database" {
  name               = "diag-db-${var.environment}"
  target_resource_id = azurerm_postgresql_flexible_server.main.id
  
  log_analytics_workspace_id = data.azurerm_log_analytics_workspace.main.id
  
  enabled_log {
    category = "PostgreSQLLogs"
  }
  
  metric {
    category = "AllMetrics"
    enabled  = true
  }
}

# Data Sources
data "azurerm_log_analytics_workspace" "main" {
  name                = "log-${var.prefix}-${var.environment}"
  resource_group_name = "rg-${var.prefix}-monitoring-${var.environment}"
}

# Outputs
output "server_id" {
  value = azurerm_postgresql_flexible_server.main.id
}

output "server_fqdn" {
  value = azurerm_postgresql_flexible_server.main.fqdn
}

output "connection_string" {
  value     = azurerm_key_vault_secret.db_connection_string.id
  sensitive = true
}

output "database_name" {
  value = azurerm_postgresql_flexible_server_database.kgv.name
}

output "administrator_login" {
  value = var.administrator_login
}