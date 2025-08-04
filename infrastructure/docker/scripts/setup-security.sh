#!/bin/bash
# Security Setup Script for KGV Migration Docker Infrastructure
# This script implements the security recommendations from the audit

set -euo pipefail

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
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

# Check if running as root
if [[ $EUID -eq 0 ]]; then
   log_error "This script should not be run as root for security reasons"
   exit 1
fi

# Check for required tools
check_dependencies() {
    local deps=("docker" "docker-compose" "openssl" "htpasswd")
    for dep in "${deps[@]}"; do
        if ! command -v "$dep" &> /dev/null; then
            log_error "$dep is not installed. Please install it first."
            exit 1
        fi
    done
    log_success "All required dependencies are installed"
}

# Create directory structure
create_directories() {
    log_info "Creating secure directory structure..."
    
    directories=(
        "data/postgres"
        "data/redis"
        "logs/api"
        "logs/nginx"
        "logs/audit"
        "certs/postgres"
        "certs/redis"
        "certs/nginx"
        "certs/api"
        "keys"
        "backup"
        "backup/encrypted"
        "falco/rules.d"
        "auditbeat"
        "vault/config"
        "vault/data"
    )
    
    for dir in "${directories[@]}"; do
        mkdir -p "$dir"
        # Set restrictive permissions
        chmod 750 "$dir"
    done
    
    # Set specific permissions for sensitive directories
    chmod 700 keys
    chmod 700 vault/data
    chmod 700 backup/encrypted
    
    log_success "Directory structure created with secure permissions"
}

# Generate secure passwords
generate_passwords() {
    log_info "Generating secure passwords..."
    
    # Generate random passwords
    POSTGRES_PASSWORD=$(openssl rand -base64 32)
    POSTGRES_APP_PASSWORD=$(openssl rand -base64 32)
    REDIS_PASSWORD=$(openssl rand -base64 32)
    JWT_SECRET=$(openssl rand -base64 64)
    NEXTAUTH_SECRET=$(openssl rand -base64 32)
    DATA_ENCRYPTION_KEY=$(openssl rand -hex 32)
    VAULT_ROOT_TOKEN=$(openssl rand -base64 32)
    GRAFANA_ADMIN_PASSWORD=$(openssl rand -base64 24)
    SEQ_ADMIN_PASSWORD=$(openssl rand -base64 24)
    
    # Save to temporary file (will be deleted after creating Docker secrets)
    cat > .secrets.tmp << EOF
POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
POSTGRES_APP_PASSWORD=${POSTGRES_APP_PASSWORD}
REDIS_PASSWORD=${REDIS_PASSWORD}
JWT_SECRET=${JWT_SECRET}
NEXTAUTH_SECRET=${NEXTAUTH_SECRET}
DATA_ENCRYPTION_KEY=${DATA_ENCRYPTION_KEY}
VAULT_ROOT_TOKEN=${VAULT_ROOT_TOKEN}
GRAFANA_ADMIN_PASSWORD=${GRAFANA_ADMIN_PASSWORD}
SEQ_ADMIN_PASSWORD=${SEQ_ADMIN_PASSWORD}
EOF
    
    chmod 600 .secrets.tmp
    log_success "Secure passwords generated"
}

# Create Docker secrets
create_docker_secrets() {
    log_info "Creating Docker secrets..."
    
    # Source the secrets file
    source .secrets.tmp
    
    # Create Docker secrets
    echo "$POSTGRES_PASSWORD" | docker secret create kgv_postgres_password - 2>/dev/null || true
    echo "$POSTGRES_APP_PASSWORD" | docker secret create kgv_postgres_app_password - 2>/dev/null || true
    echo "$REDIS_PASSWORD" | docker secret create kgv_redis_password - 2>/dev/null || true
    echo "$JWT_SECRET" | docker secret create kgv_jwt_secret - 2>/dev/null || true
    echo "$NEXTAUTH_SECRET" | docker secret create kgv_nextauth_secret - 2>/dev/null || true
    echo "$DATA_ENCRYPTION_KEY" | docker secret create kgv_data_encryption_key - 2>/dev/null || true
    
    # Clean up secrets file
    shred -vfz -n 3 .secrets.tmp
    
    log_success "Docker secrets created"
}

# Generate SSL certificates
generate_certificates() {
    log_info "Generating SSL certificates..."
    
    # Certificate configuration
    CERT_SUBJECT="/C=DE/ST=Berlin/L=Berlin/O=KGV Migration/OU=IT Security/CN=kgv.local"
    
    # Generate CA certificate
    openssl genrsa -out certs/ca.key 4096
    openssl req -x509 -new -nodes -key certs/ca.key -sha256 -days 3650 -out certs/ca.crt -subj "$CERT_SUBJECT"
    
    # Generate certificates for each service
    services=("postgres" "redis" "nginx" "api")
    
    for service in "${services[@]}"; do
        # Generate private key
        openssl genrsa -out "certs/${service}/${service}.key" 2048
        
        # Generate certificate request
        openssl req -new -key "certs/${service}/${service}.key" -out "certs/${service}/${service}.csr" \
            -subj "/C=DE/ST=Berlin/L=Berlin/O=KGV Migration/OU=IT Security/CN=${service}.kgv.local"
        
        # Sign certificate with CA
        openssl x509 -req -in "certs/${service}/${service}.csr" -CA certs/ca.crt -CAkey certs/ca.key \
            -CAcreateserial -out "certs/${service}/${service}.crt" -days 365 -sha256
        
        # Create full chain
        cat "certs/${service}/${service}.crt" certs/ca.crt > "certs/${service}/chain.pem"
        
        # Set permissions
        chmod 600 "certs/${service}/${service}.key"
        chmod 644 "certs/${service}/${service}.crt"
        chmod 644 "certs/${service}/chain.pem"
        
        # Copy CA to service directory
        cp certs/ca.crt "certs/${service}/ca.crt"
    done
    
    # Generate DH parameters for extra security
    openssl dhparam -out certs/nginx/dhparam.pem 2048
    openssl dhparam -out certs/redis/redis.dh 2048
    
    # Generate self-signed certificate for Nginx default server
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout certs/nginx/default.key -out certs/nginx/default.crt \
        -subj "/C=DE/ST=Berlin/L=Berlin/O=KGV Migration/OU=IT Security/CN=default.kgv.local"
    
    log_success "SSL certificates generated"
}

# Configure firewall rules
configure_firewall() {
    log_info "Configuring firewall rules..."
    
    # Check if ufw is installed
    if command -v ufw &> /dev/null; then
        log_warning "Please configure the following firewall rules manually:"
        echo "  sudo ufw default deny incoming"
        echo "  sudo ufw default allow outgoing"
        echo "  sudo ufw allow 443/tcp comment 'HTTPS'"
        echo "  sudo ufw allow 22/tcp comment 'SSH'"
        echo "  sudo ufw --force enable"
    else
        log_warning "UFW not installed. Please configure firewall manually."
    fi
}

# Create security configuration files
create_security_configs() {
    log_info "Creating security configuration files..."
    
    # Create Falco configuration
    cat > falco/falco.yaml << 'EOF'
# Falco configuration for KGV Migration
rules_file:
  - /etc/falco/falco_rules.yaml
  - /etc/falco/falco_rules.local.yaml
  - /etc/falco/rules.d

json_output: true
json_include_output_property: true
log_stderr: true
log_syslog: true
log_level: info

outputs:
  rate: 1
  max_burst: 1000

syscall_event_drops:
  actions:
    - log
    - alert
  rate: 0.03333
  max_burst: 10

webserver:
  enabled: true
  listen_port: 8765
  k8s_audit_endpoint: /k8s-audit
  ssl_enabled: false
EOF
    
    # Create Auditbeat configuration
    cat > auditbeat/auditbeat.yml << 'EOF'
# Auditbeat configuration for KGV Migration
auditbeat.modules:
- module: auditd
  audit_rules: |
    -w /etc/passwd -p wa -k passwd_changes
    -w /etc/group -p wa -k group_changes
    -w /etc/shadow -p wa -k shadow_changes
    -w /etc/sudoers -p wa -k sudoers_changes
    -a always,exit -F arch=b64 -S execve -k exec
    -a always,exit -F arch=b64 -S socket -S connect -k network
    -a always,exit -F arch=b64 -S open -S openat -k file_access

- module: file_integrity
  paths:
  - /app
  - /etc
  - /usr/bin
  - /usr/sbin
  - /var/lib/postgresql/data
  
- module: system
  datasets:
    - host
    - login
    - package
    - process
    - socket
    - user

output.elasticsearch:
  hosts: ["elasticsearch:9200"]
  
logging.level: info
logging.to_files: true
logging.files:
  path: /var/log/auditbeat
  name: auditbeat
  keepfiles: 7
  permissions: 0600
EOF
    
    log_success "Security configuration files created"
}

# Create backup encryption script
create_backup_script() {
    log_info "Creating encrypted backup script..."
    
    cat > scripts/backup-encrypted.sh << 'EOF'
#!/bin/bash
# Encrypted backup script for KGV Migration

set -euo pipefail

BACKUP_DIR="/backup/encrypted"
DATE=$(date +%Y%m%d_%H%M%S)
ENCRYPTION_KEY_FILE="/run/secrets/backup_encryption_key"

# Backup PostgreSQL
docker exec kgv-postgres-prod pg_dumpall -U kgv_admin | \
    gzip | \
    openssl enc -aes-256-cbc -pbkdf2 -pass file:"${ENCRYPTION_KEY_FILE}" \
    > "${BACKUP_DIR}/postgres_${DATE}.sql.gz.enc"

# Backup Redis
docker exec kgv-redis-prod redis-cli --rdb /tmp/dump.rdb && \
    docker cp kgv-redis-prod:/tmp/dump.rdb - | \
    gzip | \
    openssl enc -aes-256-cbc -pbkdf2 -pass file:"${ENCRYPTION_KEY_FILE}" \
    > "${BACKUP_DIR}/redis_${DATE}.rdb.gz.enc"

# Backup application data
tar czf - /app/data | \
    openssl enc -aes-256-cbc -pbkdf2 -pass file:"${ENCRYPTION_KEY_FILE}" \
    > "${BACKUP_DIR}/appdata_${DATE}.tar.gz.enc"

# Remove old backups (keep last 30 days)
find "${BACKUP_DIR}" -name "*.enc" -mtime +30 -delete

echo "Backup completed: ${DATE}"
EOF
    
    chmod +x scripts/backup-encrypted.sh
    log_success "Encrypted backup script created"
}

# Create security monitoring script
create_monitoring_script() {
    log_info "Creating security monitoring script..."
    
    cat > scripts/security-monitor.sh << 'EOF'
#!/bin/bash
# Security monitoring script for KGV Migration

set -euo pipefail

# Check for suspicious processes
check_processes() {
    # Look for cryptocurrency miners
    if ps aux | grep -E "(minerd|xmrig|cryptonight)" | grep -v grep; then
        echo "WARNING: Potential cryptocurrency miner detected!"
    fi
    
    # Check for reverse shells
    if netstat -tulpn | grep -E ":(4444|5555|6666|7777|8888|9999)"; then
        echo "WARNING: Suspicious port activity detected!"
    fi
}

# Check container security
check_containers() {
    # Check for privileged containers
    if docker ps --format "table {{.Names}}\t{{.Status}}" | grep -i privileged; then
        echo "WARNING: Privileged container detected!"
    fi
    
    # Check for containers running as root
    for container in $(docker ps -q); do
        if docker exec "$container" id -u 2>/dev/null | grep -q "^0$"; then
            echo "WARNING: Container $container running as root!"
        fi
    done
}

# Check file integrity
check_file_integrity() {
    # Generate checksums for critical files
    find /app -type f -name "*.dll" -o -name "*.exe" -o -name "*.so" | \
        xargs sha256sum > /tmp/checksums.txt
    
    # Compare with baseline (if exists)
    if [ -f /var/lib/checksums.baseline ]; then
        diff /var/lib/checksums.baseline /tmp/checksums.txt || \
            echo "WARNING: File integrity check failed!"
    fi
}

# Run all checks
check_processes
check_containers
check_file_integrity

echo "Security monitoring completed at $(date)"
EOF
    
    chmod +x scripts/security-monitor.sh
    log_success "Security monitoring script created"
}

# Create security checklist
create_security_checklist() {
    log_info "Creating security checklist..."
    
    cat > SECURITY_CHECKLIST.md << 'EOF'
# Security Implementation Checklist

## Pre-Deployment
- [ ] All passwords generated with sufficient entropy (min 32 characters)
- [ ] Docker secrets created and verified
- [ ] SSL certificates generated and installed
- [ ] Firewall rules configured
- [ ] SELinux/AppArmor profiles applied
- [ ] File permissions verified (principle of least privilege)

## Network Security
- [ ] Network segmentation implemented (frontend/backend/database)
- [ ] TLS enabled for all services
- [ ] mTLS configured for service-to-service communication
- [ ] Firewall rules restrict unnecessary ports
- [ ] Rate limiting configured on all endpoints
- [ ] DDoS protection enabled

## Container Security
- [ ] All containers run as non-root users
- [ ] Read-only root filesystems where possible
- [ ] Security options (no-new-privileges, etc.) applied
- [ ] Resource limits configured
- [ ] Health checks implemented
- [ ] Container images scanned for vulnerabilities

## Application Security
- [ ] Input validation implemented
- [ ] SQL injection prevention verified
- [ ] XSS protection enabled
- [ ] CSRF tokens implemented
- [ ] Authentication/authorization properly configured
- [ ] Session management secure
- [ ] API rate limiting active
- [ ] Security headers configured

## Data Protection
- [ ] Encryption at rest configured
- [ ] Encryption in transit enabled
- [ ] Backup encryption implemented
- [ ] Key rotation schedule defined
- [ ] Data retention policies configured
- [ ] GDPR compliance verified

## Monitoring & Logging
- [ ] Audit logging enabled
- [ ] Security monitoring active
- [ ] Intrusion detection configured
- [ ] Log aggregation implemented
- [ ] Alerting rules defined
- [ ] Incident response plan documented

## Compliance
- [ ] OWASP Top 10 addressed
- [ ] GDPR requirements met
- [ ] Security policies documented
- [ ] Access controls reviewed
- [ ] Penetration testing scheduled
- [ ] Security training completed

## Production Readiness
- [ ] Vulnerability scanning automated
- [ ] Security patches up to date
- [ ] Disaster recovery tested
- [ ] Backup restoration verified
- [ ] Performance under load tested
- [ ] Security review completed and signed off
EOF
    
    log_success "Security checklist created"
}

# Main execution
main() {
    echo "====================================="
    echo "KGV Migration Security Setup Script"
    echo "====================================="
    echo ""
    
    check_dependencies
    create_directories
    generate_passwords
    create_docker_secrets
    generate_certificates
    configure_firewall
    create_security_configs
    create_backup_script
    create_monitoring_script
    create_security_checklist
    
    echo ""
    echo "====================================="
    echo "Security Setup Complete!"
    echo "====================================="
    echo ""
    echo "Next steps:"
    echo "1. Review SECURITY_CHECKLIST.md"
    echo "2. Configure firewall rules as shown above"
    echo "3. Test the secure configuration:"
    echo "   docker-compose -f docker-compose.yml -f docker-compose.secure.yml up -d"
    echo "4. Run security monitoring:"
    echo "   ./scripts/security-monitor.sh"
    echo "5. Schedule regular backups:"
    echo "   crontab -e"
    echo "   0 2 * * * /path/to/scripts/backup-encrypted.sh"
    echo ""
    log_warning "Remember to securely store the generated passwords!"
    log_warning "Check logs/audit/ for security events"
}

# Run main function
main "$@"