# KGV Migration - Docker Security Hardening Guide

## Overview

This guide provides step-by-step instructions to implement the security recommendations from the security audit. Follow these steps to achieve production-ready security.

## Table of Contents

1. [Immediate Actions (Critical)](#immediate-actions-critical)
2. [Network Security](#network-security)
3. [Secrets Management](#secrets-management)
4. [Container Hardening](#container-hardening)
5. [Database Security](#database-security)
6. [Application Security](#application-security)
7. [Monitoring & Compliance](#monitoring--compliance)
8. [Testing & Validation](#testing--validation)

---

## Immediate Actions (Critical)

These must be completed before any production deployment.

### 1. Remove Hardcoded Credentials

```bash
# Step 1: Generate secure passwords
./scripts/setup-security.sh

# Step 2: Create Docker secrets
docker secret create kgv_postgres_password - < /dev/urandom | head -c 32 | base64
docker secret create kgv_redis_password - < /dev/urandom | head -c 32 | base64
docker secret create kgv_jwt_secret - < /dev/urandom | head -c 64 | base64

# Step 3: Update docker-compose to use secrets
docker-compose -f docker-compose.yml -f docker-compose.secure.yml config
```

### 2. Disable Public Database Port

```yaml
# Change in docker-compose.prod.yml
postgres:
  # Remove or comment out:
  # ports:
  #   - "5432:5432"
  expose:
    - "5432"  # Only accessible within Docker network
```

### 3. Implement Network Segmentation

```bash
# Create isolated networks
docker network create --driver bridge --internal kgv-database
docker network create --driver bridge --internal kgv-backend
docker network create --driver bridge kgv-frontend

# Verify network isolation
docker network inspect kgv-database
```

---

## Network Security

### TLS/SSL Configuration

#### 1. Generate Certificates

```bash
# Generate CA certificate
openssl genrsa -out ca.key 4096
openssl req -x509 -new -nodes -key ca.key -sha256 -days 3650 -out ca.crt

# Generate service certificates
for service in postgres redis nginx api; do
    openssl genrsa -out ${service}.key 2048
    openssl req -new -key ${service}.key -out ${service}.csr
    openssl x509 -req -in ${service}.csr -CA ca.crt -CAkey ca.key \
        -CAcreateserial -out ${service}.crt -days 365 -sha256
done
```

#### 2. Configure PostgreSQL TLS

```sql
-- postgresql.conf
ssl = on
ssl_cert_file = '/var/lib/postgresql/server.crt'
ssl_key_file = '/var/lib/postgresql/server.key'
ssl_ca_file = '/var/lib/postgresql/ca.crt'
ssl_ciphers = 'HIGH:MEDIUM:+3DES:!aNULL'
ssl_prefer_server_ciphers = on
ssl_min_protocol_version = 'TLSv1.2'
```

#### 3. Configure Redis TLS

```conf
# redis.conf
tls-port 6379
port 0
tls-cert-file /tls/redis.crt
tls-key-file /tls/redis.key
tls-ca-cert-file /tls/ca.crt
tls-auth-clients yes
tls-protocols "TLSv1.2 TLSv1.3"
```

### Firewall Configuration

```bash
# UFW configuration for Docker host
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 443/tcp comment 'HTTPS'
sudo ufw allow 22/tcp comment 'SSH'
sudo ufw --force enable

# iptables rules for container communication
sudo iptables -I DOCKER-USER -i ext_if ! -s 172.28.0.0/16 -j DROP
sudo iptables -I DOCKER-USER -m conntrack --ctstate RELATED,ESTABLISHED -j ACCEPT
```

---

## Secrets Management

### Docker Secrets Implementation

```bash
# Create secrets from files
echo "super_secret_password" | docker secret create postgres_password -
echo "redis_secret" | docker secret create redis_password -

# Use in docker-compose.yml
services:
  postgres:
    secrets:
      - postgres_password
    environment:
      POSTGRES_PASSWORD_FILE: /run/secrets/postgres_password
```

### HashiCorp Vault Integration

```bash
# Initialize Vault
docker exec -it kgv-vault vault operator init

# Unseal Vault
docker exec -it kgv-vault vault operator unseal <key1>
docker exec -it kgv-vault vault operator unseal <key2>
docker exec -it kgv-vault vault operator unseal <key3>

# Create secrets
docker exec -it kgv-vault vault kv put secret/kgv/database \
    password="secure_password" \
    username="kgv_admin"
```

### Environment Variables Security

```bash
# Use .env files with proper permissions
touch .env.production
chmod 600 .env.production

# Load secrets at runtime
source .env.production
docker-compose up -d
```

---

## Container Hardening

### User Permissions

```dockerfile
# Dockerfile best practices
FROM node:20-alpine AS builder
# Build stage...

FROM gcr.io/distroless/nodejs20-debian11
COPY --from=builder --chown=1001:1001 /app /app
USER 1001
EXPOSE 3000
CMD ["/app/server.js"]
```

### Security Options

```yaml
# docker-compose.yml
services:
  api:
    security_opt:
      - no-new-privileges:true
      - apparmor:docker-default
      - seccomp:default
    cap_drop:
      - ALL
    cap_add:
      - NET_BIND_SERVICE
    read_only: true
    tmpfs:
      - /tmp
```

### Resource Limits

```yaml
services:
  api:
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
```

---

## Database Security

### PostgreSQL Hardening

```sql
-- Security configuration
ALTER SYSTEM SET ssl = on;
ALTER SYSTEM SET ssl_ciphers = 'HIGH:MEDIUM:+3DES:!aNULL';
ALTER SYSTEM SET password_encryption = 'scram-sha-256';
ALTER SYSTEM SET log_connections = on;
ALTER SYSTEM SET log_disconnections = on;
ALTER SYSTEM SET log_statement = 'all';

-- Create application user with limited privileges
CREATE USER kgv_app WITH ENCRYPTED PASSWORD 'secure_password';
GRANT CONNECT ON DATABASE kgv_production TO kgv_app;
GRANT USAGE ON SCHEMA public TO kgv_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO kgv_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO kgv_app;

-- Enable row-level security
ALTER TABLE antrag ENABLE ROW LEVEL SECURITY;
CREATE POLICY antrag_policy ON antrag
    FOR ALL
    TO kgv_app
    USING (bezirk_id IN (SELECT bezirk_id FROM user_permissions WHERE user_id = current_user));
```

### Redis Security

```conf
# ACL configuration
ACL DELUSER default
ACL SETUSER kgv_app on >app_password ~kgv:* +@read +@write +@list -@dangerous
ACL SETUSER monitoring on >monitor_password ~* +ping +info +client -@dangerous
ACL SAVE
```

### Backup Encryption

```bash
#!/bin/bash
# Encrypted backup script
BACKUP_DATE=$(date +%Y%m%d_%H%M%S)
ENCRYPTION_KEY=$(cat /run/secrets/backup_key)

# PostgreSQL backup
docker exec postgres pg_dumpall -U postgres | \
  gzip | \
  openssl enc -aes-256-cbc -salt -pass pass:"$ENCRYPTION_KEY" \
  > backup_${BACKUP_DATE}.sql.gz.enc

# Restore command
openssl enc -aes-256-cbc -d -pass pass:"$ENCRYPTION_KEY" \
  -in backup_${BACKUP_DATE}.sql.gz.enc | \
  gunzip | \
  docker exec -i postgres psql -U postgres
```

---

## Application Security

### API Security (.NET)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Security services
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
    .ProtectKeysWithCertificate(certificate);

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("Secure",
        policy => policy
            .WithOrigins("https://kgv.example.com")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowCredentials()
            .WithHeaders("Content-Type", "Authorization"));
});

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

// Security middleware
app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
```

### Frontend Security (Next.js)

```javascript
// next.config.js
module.exports = {
  poweredByHeader: false,
  
  async headers() {
    return [
      {
        source: '/:path*',
        headers: [
          {
            key: 'X-Frame-Options',
            value: 'DENY',
          },
          {
            key: 'X-Content-Type-Options',
            value: 'nosniff',
          },
          {
            key: 'Referrer-Policy',
            value: 'strict-origin-when-cross-origin',
          },
          {
            key: 'Content-Security-Policy',
            value: `
              default-src 'self';
              script-src 'self' 'nonce-{nonce}';
              style-src 'self' 'nonce-{nonce}';
              img-src 'self' data: https:;
              font-src 'self';
              connect-src 'self' https://api.kgv.example.com;
              frame-ancestors 'none';
            `.replace(/\s{2,}/g, ' ').trim(),
          },
        ],
      },
    ];
  },
};
```

### Input Validation

```csharp
// Model validation
public class AntragDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [RegularExpression(@"^[a-zA-ZäöüÄÖÜß\s-]+$")]
    public string Name { get; set; }
    
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }
    
    [Required]
    [Phone]
    public string Telefon { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int BezirkId { get; set; }
}

// Sanitization
public static class InputSanitizer
{
    public static string SanitizeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        return System.Web.HttpUtility.HtmlEncode(input);
    }
    
    public static string SanitizeSql(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        return input.Replace("'", "''")
                   .Replace(";", "")
                   .Replace("--", "")
                   .Replace("/*", "")
                   .Replace("*/", "");
    }
}
```

---

## Monitoring & Compliance

### Security Monitoring Stack

```yaml
# docker-compose.monitoring.yml
services:
  falco:
    image: falcosecurity/falco:latest
    privileged: true
    volumes:
      - /var/run/docker.sock:/host/var/run/docker.sock
      - /proc:/host/proc:ro
      - ./falco/rules.yaml:/etc/falco/rules.yaml
    
  wazuh:
    image: wazuh/wazuh:latest
    ports:
      - "1514:1514/udp"
      - "55000:55000"
    
  auditbeat:
    image: docker.elastic.co/beats/auditbeat:8.11.0
    user: root
    cap_add:
      - AUDIT_CONTROL
      - AUDIT_READ
```

### Audit Logging

```sql
-- PostgreSQL audit logging
CREATE EXTENSION IF NOT EXISTS pgaudit;

ALTER SYSTEM SET pgaudit.log = 'ALL';
ALTER SYSTEM SET pgaudit.log_level = 'INFO';
ALTER SYSTEM SET pgaudit.log_client = on;
ALTER SYSTEM SET pgaudit.log_statement_once = off;
ALTER SYSTEM SET pgaudit.log_parameter = on;
```

### GDPR Compliance

```csharp
// Data anonymization
public class GdprService
{
    public async Task<bool> DeletePersonalData(int userId)
    {
        // Anonymize instead of delete
        var user = await _context.Users.FindAsync(userId);
        user.Name = $"DELETED_USER_{userId}";
        user.Email = $"deleted_{userId}@example.com";
        user.Phone = "000-000-0000";
        user.Address = "DELETED";
        
        await _context.SaveChangesAsync();
        
        // Log the deletion
        await _auditLog.LogAsync($"Personal data deleted for user {userId}");
        
        return true;
    }
    
    public async Task<string> ExportPersonalData(int userId)
    {
        var userData = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Name,
                u.Email,
                u.Phone,
                u.Address,
                u.CreatedAt,
                u.UpdatedAt
            })
            .FirstOrDefaultAsync();
            
        return JsonSerializer.Serialize(userData);
    }
}
```

---

## Testing & Validation

### Security Testing

```bash
# Container vulnerability scanning
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image kgv-api:latest

# Network security scan
docker run --rm -it \
  instrumentisto/nmap -sV -sC -O -A kgv.example.com

# Web application security scan
docker run -t owasp/zap2docker-stable zap-baseline.py \
  -t https://kgv.example.com
```

### Penetration Testing Checklist

- [ ] SQL Injection testing
- [ ] XSS vulnerability scanning
- [ ] CSRF token validation
- [ ] Authentication bypass attempts
- [ ] Session hijacking tests
- [ ] Rate limiting verification
- [ ] Input validation testing
- [ ] File upload security
- [ ] API security testing
- [ ] Container escape attempts

### Compliance Validation

```bash
# CIS Docker Benchmark
docker run --rm --net host --pid host --userns host --cap-add audit_control \
  -v /etc:/etc:ro \
  -v /usr/bin/containerd:/usr/bin/containerd:ro \
  -v /usr/bin/runc:/usr/bin/runc:ro \
  -v /usr/lib/systemd:/usr/lib/systemd:ro \
  -v /var/lib:/var/lib:ro \
  -v /var/run/docker.sock:/var/run/docker.sock:ro \
  --label docker_bench_security \
  docker/docker-bench-security
```

---

## Security Maintenance

### Regular Tasks

#### Daily
- Review security logs
- Check for failed authentication attempts
- Monitor resource usage

#### Weekly
- Run vulnerability scans
- Review firewall logs
- Check for security updates

#### Monthly
- Rotate passwords and keys
- Review access logs
- Update security documentation
- Test backup restoration

#### Quarterly
- Conduct security audit
- Penetration testing
- Review and update security policies
- Security training

### Incident Response

```bash
#!/bin/bash
# Incident response script
case "$1" in
  "breach")
    # Isolate affected containers
    docker network disconnect kgv-frontend compromised-container
    # Capture forensic data
    docker logs compromised-container > incident_$(date +%s).log
    # Alert security team
    curl -X POST https://alerts.example.com/security -d "Security breach detected"
    ;;
  "suspicious")
    # Enable enhanced logging
    docker exec nginx nginx -s reload
    # Start packet capture
    tcpdump -i docker0 -w capture_$(date +%s).pcap
    ;;
esac
```

---

## Security Contacts

- **Security Team Lead:** security@kgv.example.com
- **Incident Response:** incident@kgv.example.com
- **24/7 Security Hotline:** +49-XXX-XXXXXXX

## References

- [OWASP Docker Security](https://owasp.org/www-project-docker-top-10/)
- [CIS Docker Benchmark](https://www.cisecurity.org/benchmark/docker)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)
- [BSI IT-Grundschutz](https://www.bsi.bund.de/EN/Topics/ITGrundschutz/itgrundschutz_node.html)
- [GDPR Compliance](https://gdpr.eu/)

---

**Document Version:** 1.0  
**Last Updated:** 2025-08-04  
**Review Cycle:** Monthly  
**Classification:** CONFIDENTIAL