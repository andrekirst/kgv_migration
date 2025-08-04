# Security Implementation Guide for KGV Docker Infrastructure

## Overview
This guide provides step-by-step instructions to implement the security fixes identified in the security audit.

## Critical Security Fixes (Implement Immediately)

### 1. Environment Variables Setup

#### Step 1: Create .env file from template
```bash
cd infrastructure/docker
cp .env.example .env
```

#### Step 2: Generate secure passwords
```bash
# Generate secure passwords for all services
echo "POSTGRES_PASSWORD=$(openssl rand -base64 32)"
echo "POSTGRES_APP_PASSWORD=$(openssl rand -base64 32)"
echo "REDIS_PASSWORD=$(openssl rand -base64 32)"
echo "JWT_SECRET=$(openssl rand -base64 64)"
echo "NEXTAUTH_SECRET=$(openssl rand -base64 32)"
echo "PGADMIN_PASSWORD=$(openssl rand -base64 24)"
echo "SEQ_ADMIN_PASSWORD=$(openssl rand -base64 24)"
echo "GRAFANA_ADMIN_PASSWORD=$(openssl rand -base64 24)"
echo "DATA_ENCRYPTION_KEY=$(openssl rand -hex 32)"
```

#### Step 3: Update .env file
Add the generated passwords to your .env file. **NEVER commit this file to git.**

### 2. Database Security

#### Step 1: Apply secure initialization
```bash
# Make initialization script executable
chmod +x infrastructure/docker/init-scripts/02-secure-init.sh
```

#### Step 2: Use separate database user for application
Update your application connection string to use `kgv_app` user instead of admin:
```
ConnectionStrings__Database: "Host=postgres;Database=kgv_production;Username=kgv_app;Password=${POSTGRES_APP_PASSWORD}"
```

### 3. Redis Security with ACL

#### Step 1: Apply Redis ACL configuration
```bash
# The redis-acl.conf file has been created
# Update docker-compose to use it:
docker-compose down redis
docker-compose up -d redis
```

#### Step 2: Set Redis user passwords
```bash
# Connect to Redis and set passwords
docker exec -it kgv-redis redis-cli
AUTH ${REDIS_PASSWORD}
ACL SETUSER kgv_app on >${YOUR_APP_PASSWORD}
ACL SETUSER admin on >${YOUR_ADMIN_PASSWORD}
ACL SETUSER monitoring on >${YOUR_MONITORING_PASSWORD}
ACL SAVE
```

### 4. SSL/TLS Configuration

#### Step 1: Generate SSL certificates for production
```bash
# For production, use Let's Encrypt
mkdir -p infrastructure/docker/nginx/ssl

# Generate DH parameters (this takes time)
openssl dhparam -out infrastructure/docker/nginx/ssl/dhparam.pem 4096

# For testing only - self-signed certificate
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout infrastructure/docker/nginx/ssl/key.pem \
  -out infrastructure/docker/nginx/ssl/cert.pem \
  -subj "/C=DE/ST=Berlin/L=Berlin/O=KGV/CN=localhost"
```

#### Step 2: For production - Use Let's Encrypt
```bash
# Install certbot
docker run -it --rm \
  -v ./nginx/ssl:/etc/letsencrypt \
  certbot/certbot certonly \
  --standalone \
  -d your-domain.com \
  --agree-tos \
  --email admin@your-domain.com
```

### 5. Apply Security Hardening

#### Step 1: Use security-enhanced Docker Compose
```bash
# For development with security enhancements
docker-compose -f docker-compose.yml -f docker-security.yml up -d

# For production
docker-compose -f docker-compose.prod.yml -f docker-security.yml up -d
```

#### Step 2: Verify security settings
```bash
# Check that containers are running as non-root
docker exec kgv-api whoami  # Should show 'appuser' or uid 1001

# Check Redis ACL is active
docker exec kgv-redis redis-cli ACL LIST

# Check Nginx security headers
curl -I http://localhost
```

## Network Security Implementation

### 1. Enable TLS for Internal Communications

#### PostgreSQL TLS
```bash
# Generate PostgreSQL certificates
cd infrastructure/docker/postgres
openssl req -new -x509 -days 365 -nodes \
  -out server.crt -keyout server.key \
  -subj "/CN=postgres"
chmod 600 server.key
```

#### Redis TLS
```bash
# Generate Redis certificates
cd infrastructure/docker/redis
openssl req -new -x509 -days 365 -nodes \
  -out redis.crt -keyout redis.key \
  -subj "/CN=redis"
```

### 2. Network Segmentation

Create separate networks for different tiers:
```yaml
networks:
  frontend:
    driver: bridge
  backend:
    driver: bridge
    internal: true
  data:
    driver: bridge
    internal: true
```

## Monitoring & Logging Security

### 1. Log Sanitization

#### Step 1: Configure log sanitization in Nginx
```nginx
# Custom log format that masks sensitive data
log_format sanitized '$remote_addr - $remote_user [$time_local] '
                    '"$request_method $uri $server_protocol" '
                    '$status $body_bytes_sent '
                    '"$http_referer" "$http_user_agent"';
```

#### Step 2: Application-level log sanitization
Ensure your application masks:
- Passwords
- API keys
- Personal data (GDPR)
- Session tokens

### 2. Security Event Monitoring

#### Step 1: Enable audit logging
```bash
# Check audit logs
docker exec kgv-postgres psql -U kgv_admin -d kgv_production \
  -c "SELECT * FROM audit.audit_log ORDER BY changed_at DESC LIMIT 10;"
```

#### Step 2: Set up alerts
Configure alerts for:
- Failed login attempts > 5
- Unusual database queries
- High error rates
- Resource exhaustion

## GDPR Compliance Implementation

### 1. Data Encryption at Rest

#### Step 1: Enable volume encryption
```bash
# For production, use encrypted volumes
# AWS EBS encryption
aws ec2 create-volume --encrypted --size 100 --volume-type gp3

# Or use LUKS encryption on Linux
cryptsetup luksFormat /dev/sdX
cryptsetup open /dev/sdX encrypted_volume
mkfs.ext4 /dev/mapper/encrypted_volume
```

### 2. Data Retention Policies

#### Step 1: Implement automated cleanup
```sql
-- Create cleanup job
CREATE OR REPLACE FUNCTION cleanup_old_data()
RETURNS void AS $$
BEGIN
    -- Delete data older than retention period
    DELETE FROM user_sessions WHERE created_at < NOW() - INTERVAL '90 days';
    DELETE FROM audit.audit_log WHERE changed_at < NOW() - INTERVAL '365 days';
END;
$$ LANGUAGE plpgsql;

-- Schedule with pg_cron or external scheduler
```

### 3. Right to Erasure

#### Step 1: Implement data deletion endpoint
```bash
# API endpoint for GDPR data deletion
POST /api/gdpr/delete-my-data
Authorization: Bearer {user_token}
```

## Security Testing

### 1. Container Vulnerability Scanning

#### Using Trivy
```bash
# Scan all images
for image in $(docker-compose config | grep 'image:' | awk '{print $2}'); do
  echo "Scanning $image..."
  docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
    aquasec/trivy image $image
done
```

### 2. Security Headers Testing

```bash
# Test security headers
curl -I https://your-domain.com | grep -E "X-Frame-Options|X-Content-Type|Strict-Transport-Security|Content-Security-Policy"

# Use online tools
# https://securityheaders.com
# https://observatory.mozilla.org
```

### 3. SSL/TLS Testing

```bash
# Test SSL configuration
nmap --script ssl-enum-ciphers -p 443 your-domain.com

# Or use online tool
# https://www.ssllabs.com/ssltest/
```

## Deployment Checklist

### Pre-Deployment
- [ ] All environment variables set in .env.prod
- [ ] No default or weak passwords
- [ ] SSL certificates installed
- [ ] DH parameters generated (4096 bit)
- [ ] Database backup created
- [ ] Security headers configured
- [ ] Rate limiting configured
- [ ] ACLs and permissions set
- [ ] Container images scanned for vulnerabilities
- [ ] Network segmentation implemented

### During Deployment
- [ ] Use security-enhanced docker-compose
- [ ] Verify containers run as non-root
- [ ] Check all health endpoints
- [ ] Verify SSL/TLS is working
- [ ] Test authentication flow
- [ ] Monitor logs for errors

### Post-Deployment
- [ ] Run security scan
- [ ] Test all security headers
- [ ] Verify audit logging works
- [ ] Test rate limiting
- [ ] Perform penetration testing
- [ ] Document security configuration
- [ ] Set up monitoring alerts
- [ ] Schedule security reviews

## Maintenance & Updates

### Weekly Tasks
- Review security logs
- Check for failed login attempts
- Monitor resource usage
- Review audit logs

### Monthly Tasks
- Update container images
- Rotate passwords
- Review access logs
- Test backup restoration
- Run vulnerability scans

### Quarterly Tasks
- Full security audit
- Penetration testing
- Update SSL certificates
- Review and update security policies
- GDPR compliance check

## Emergency Procedures

### Suspected Breach
1. Isolate affected containers: `docker-compose stop [service]`
2. Preserve logs: `docker logs [container] > incident_$(date +%s).log`
3. Rotate all credentials immediately
4. Review audit logs for unauthorized access
5. Notify security team and DPO

### Password Compromise
```bash
# Immediate password rotation
docker exec kgv-postgres psql -U postgres -c "ALTER USER kgv_app PASSWORD 'new_secure_password';"
docker exec kgv-redis redis-cli CONFIG SET requirepass new_secure_password
# Update .env file and restart services
```

### DDoS Attack
1. Enable rate limiting at CDN level
2. Block suspicious IPs
3. Scale horizontally if needed
4. Enable challenge pages

## Security Contacts

- Security Team: security@your-domain.com
- Data Protection Officer: dpo@your-domain.com
- Emergency Hotline: +49 XXX XXXXX
- Bug Bounty: security-bounty@your-domain.com

## Additional Resources

- [OWASP Docker Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Docker_Security_Cheat_Sheet.html)
- [CIS Docker Benchmark](https://www.cisecurity.org/benchmark/docker)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)
- [BSI IT-Grundschutz](https://www.bsi.bund.de/EN/Topics/ITGrundschutz/itgrundschutz_node.html)

---

**Remember:** Security is not a one-time task but an ongoing process. Regular reviews, updates, and training are essential for maintaining a secure infrastructure.