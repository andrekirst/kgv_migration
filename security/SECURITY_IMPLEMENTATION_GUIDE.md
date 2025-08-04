# Security Implementation Guide for KGV PostgreSQL Migration

## Immediate Actions Required (Critical - 24 hours)

### 1. Remove Hardcoded Credentials
```bash
# Create .env file (DO NOT COMMIT)
cat > .env << 'EOF'
DB_PASSWORD=$(openssl rand -base64 32)
REDIS_PASSWORD=$(openssl rand -base64 32)
JWT_SECRET=$(openssl rand -base64 64)
NEXTAUTH_SECRET=$(openssl rand -base64 32)
PGADMIN_PASSWORD=$(openssl rand -base64 24)
SEQ_PASSWORD=$(openssl rand -base64 24)
GRAFANA_PASSWORD=$(openssl rand -base64 24)
EOF

# Load environment variables
export $(cat .env | xargs)

# Create Docker secrets
echo "$DB_PASSWORD" | docker secret create kgv_db_password_v1 -
echo "$REDIS_PASSWORD" | docker secret create kgv_redis_password_v1 -
echo "$JWT_SECRET" | docker secret create kgv_api_jwt_secret_v1 -
```

### 2. Enable SSL/TLS for Database Connections
```bash
# Generate SSL certificates
cd security/ssl
openssl req -new -x509 -days 365 -nodes -text -out server.crt \
  -keyout server.key -subj "/CN=kgv-postgres"
chmod 600 server.key
chown 999:999 server.* 

# Update connection strings
sed -i 's/sslmode=disable/sslmode=require/g' docker-compose.yml
```

### 3. Fix SQL Injection Vulnerabilities
```python
# Replace in migration_pipeline.py
# OLD (VULNERABLE):
sql_cur.execute(f"SELECT * FROM [{source_table}]")

# NEW (SECURE):
sql_cur.execute("SELECT * FROM [?]", (source_table,))
```

### 4. Implement Database Encryption
```sql
-- Run implement-security.sql
docker exec -i kgv-postgres psql -U postgres < security/implement-security.sql
```

### 5. Disable Exposed Ports
```yaml
# Update docker-compose.yml
# Remove or comment out:
# ports:
#   - "5432:5432"  # PostgreSQL
#   - "6379:6379"  # Redis
```

## High Priority Actions (48-72 hours)

### 1. Set Up Azure Key Vault
```bash
# Create Key Vault
az keyvault create \
  --name kgv-secrets-prod \
  --resource-group rg-kgv-prod \
  --location germanywestcentral \
  --sku standard

# Store secrets
az keyvault secret set --vault-name kgv-secrets-prod \
  --name db-password --value "$DB_PASSWORD"
```

### 2. Implement Network Segmentation
```bash
# Apply secure Docker Compose
docker-compose -f security/docker-compose.secure.yml up -d
```

### 3. Configure WAF and Rate Limiting
```nginx
# nginx/conf.d/security.conf
limit_req_zone $binary_remote_addr zone=login:10m rate=5r/m;
limit_req_zone $binary_remote_addr zone=api:10m rate=100r/s;

location /api/auth/login {
    limit_req zone=login burst=5 nodelay;
    # ... proxy settings
}
```

### 4. Enable Audit Logging
```sql
-- Enable pgAudit
CREATE EXTENSION pgaudit;
ALTER SYSTEM SET pgaudit.log = 'ALL';
SELECT pg_reload_conf();
```

## GDPR Compliance Checklist

### Data Protection Measures
- [ ] Encryption at rest (TDE) enabled
- [ ] Encryption in transit (TLS 1.3) enforced
- [ ] PII data classified and tagged
- [ ] Data masking implemented for non-production
- [ ] Pseudonymization functions created
- [ ] Access controls implemented (RBAC)
- [ ] Audit logging comprehensive
- [ ] Data retention policies configured
- [ ] Right to erasure (Article 17) implemented
- [ ] Data portability (Article 20) enabled

### Access Control Implementation
```sql
-- Grant minimal required permissions
REVOKE ALL ON DATABASE kgv_production FROM PUBLIC;
GRANT CONNECT ON DATABASE kgv_production TO kgv_app;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO kgv_app;
REVOKE DELETE ON ALL TABLES IN SCHEMA public FROM kgv_app; -- Soft deletes only
```

### Monitoring and Alerting
```yaml
# prometheus/alerts.yml
groups:
  - name: security
    rules:
      - alert: TooManyFailedLogins
        expr: rate(failed_login_attempts[5m]) > 10
        for: 1m
        annotations:
          summary: "High number of failed login attempts"
      
      - alert: UnauthorizedDatabaseAccess
        expr: unauthorized_db_access > 0
        for: 1m
        annotations:
          summary: "Unauthorized database access detected"
```

## Security Testing Procedures

### 1. Vulnerability Scanning
```bash
# Run Docker security scan
docker scan kgv-postgres:latest
docker scan kgv-api:latest

# OWASP dependency check
dependency-check --project KGV --scan . --format HTML
```

### 2. SQL Injection Testing
```bash
# Test with sqlmap (authorized testing only)
sqlmap -u "http://localhost:5000/api/applications?id=1" \
  --batch --risk=3 --level=5
```

### 3. Authentication Testing
```bash
# Test rate limiting
for i in {1..20}; do
  curl -X POST http://localhost:5000/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"test","password":"wrong"}'
done
```

## Incident Response Plan

### Detection
1. Monitor audit.failed_auth_log for suspicious patterns
2. Check audit.access_log for unusual queries
3. Review application logs for errors/exceptions

### Response Steps
1. **Isolate**: Disconnect affected systems
2. **Investigate**: Review audit logs, identify scope
3. **Contain**: Block malicious IPs, revoke compromised credentials
4. **Eradicate**: Remove malicious code, patch vulnerabilities
5. **Recover**: Restore from secure backup
6. **Document**: Create incident report

### Contact Information
- Security Team: security@frankfurt.de
- DPO: dpo@frankfurt.de
- On-Call: +49-69-212-XXXXX

## Regular Security Tasks

### Daily
- [ ] Review failed authentication logs
- [ ] Check backup completion and encryption
- [ ] Monitor database connections

### Weekly
- [ ] Review audit logs for anomalies
- [ ] Update security patches
- [ ] Test backup restoration

### Monthly
- [ ] Rotate credentials
- [ ] Security vulnerability scan
- [ ] Review and update access permissions
- [ ] GDPR compliance audit

### Quarterly
- [ ] Penetration testing
- [ ] Security training for team
- [ ] Update incident response plan
- [ ] Review data retention compliance

## Compliance Documentation

### Required Documents
1. **Data Protection Impact Assessment (DPIA)**
   - Template: `security/templates/DPIA_template.docx`
   - Due: Before go-live

2. **Data Processing Register**
   - Location: `security/compliance/processing_register.xlsx`
   - Update: Monthly

3. **Security Incident Log**
   - Location: `security/incidents/`
   - Format: YYYY-MM-DD_incident_description.md

4. **Access Control Matrix**
   - Location: `security/access_control_matrix.xlsx`
   - Review: Quarterly

## Emergency Procedures

### Database Compromise
```bash
# 1. Isolate database
docker network disconnect kgv-network kgv-postgres

# 2. Create forensic copy
docker exec kgv-postgres pg_dumpall > emergency_backup_$(date +%Y%m%d_%H%M%S).sql

# 3. Reset all passwords
ALTER USER kgv_app WITH PASSWORD 'new_secure_password';
ALTER USER kgv_readonly WITH PASSWORD 'new_secure_password';

# 4. Review audit logs
SELECT * FROM audit.access_log 
WHERE event_time > NOW() - INTERVAL '24 hours'
ORDER BY event_time DESC;
```

### Data Breach Response
1. **Immediate** (< 1 hour):
   - Isolate affected systems
   - Preserve evidence
   - Notify security team

2. **Short-term** (< 24 hours):
   - Assess scope and impact
   - Notify DPO
   - Begin forensic investigation

3. **GDPR Timeline** (< 72 hours):
   - Notify supervisory authority
   - Prepare breach notification
   - Document timeline and actions

## Security Configuration Files

All security configuration files are located in the `security/` directory:

```
security/
├── SECURITY_AUDIT_REPORT.md           # Full audit findings
├── SECURITY_IMPLEMENTATION_GUIDE.md   # This file
├── docker-compose.secure.yml          # Hardened Docker configuration
├── implement-security.sql             # Database security setup
├── pg_hba_secure.conf                # PostgreSQL authentication
├── postgresql-security.conf          # PostgreSQL security settings
├── ssl/                              # SSL certificates
├── nginx/                            # WAF and reverse proxy configs
└── templates/                        # Compliance templates
```

## Validation Steps

After implementing security measures, validate:

1. **SSL/TLS Enforcement**
```bash
psql "postgresql://kgv_app@localhost/kgv_production?sslmode=disable"
# Should fail with SSL required error
```

2. **Authentication**
```bash
psql -U kgv_app -d kgv_production -c "DELETE FROM applications WHERE id = 1;"
# Should fail with permission denied
```

3. **Audit Logging**
```sql
SELECT * FROM audit.access_log WHERE event_time > NOW() - INTERVAL '1 hour';
-- Should show recent activity
```

4. **Encryption**
```sql
SELECT security.encrypt_pii('sensitive data');
-- Should return encrypted string
```

## Support and Resources

- **PostgreSQL Security**: https://www.postgresql.org/docs/16/security.html
- **OWASP Top 10**: https://owasp.org/www-project-top-ten/
- **GDPR Guidelines**: https://gdpr.eu/
- **BSI IT-Grundschutz**: https://www.bsi.bund.de/EN/

---

**Last Updated:** 2025-08-04  
**Next Review:** 2025-09-04  
**Classification:** CONFIDENTIAL - INTERNAL USE ONLY