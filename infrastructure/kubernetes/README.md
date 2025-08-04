# KGV Kubernetes Infrastructure

This directory contains the complete Kubernetes infrastructure for the KGV (Kleingartenverein) migration project, providing production-ready deployment configurations following cloud-native best practices.

## 📁 Directory Structure

```
infrastructure/kubernetes/
├── base/                           # Base Kustomize configurations
│   ├── namespace.yaml             # Namespace definitions
│   ├── service-accounts.yaml      # Service accounts and RBAC
│   ├── rbac.yaml                  # Role-based access control
│   ├── configmap-app.yaml         # Application configuration
│   ├── secret-app.yaml            # Application secrets
│   ├── postgres-*.yaml            # PostgreSQL StatefulSet & services
│   ├── redis-*.yaml               # Redis StatefulSet & services
│   ├── api-*.yaml                 # .NET API deployment & services
│   ├── web-*.yaml                 # Next.js web deployment & services
│   ├── ingress.yaml               # Ingress configuration with TLS
│   ├── network-policies.yaml      # Network security policies
│   ├── pdb.yaml                   # Pod Disruption Budgets
│   └── kustomization.yaml         # Base Kustomize configuration
├── overlays/                      # Environment-specific overlays
│   ├── development/               # Development environment
│   ├── staging/                   # Staging environment
│   └── production/                # Production environment
├── monitoring/                    # Monitoring stack (Prometheus, Grafana)
├── backup/                        # Backup and recovery jobs
├── scripts/                       # Deployment and management scripts
└── README.md                      # This file
```

## 🏗️ Architecture Overview

### Core Components

- **API Service**: .NET 9 Web API with health checks, metrics, and observability
- **Web Application**: Next.js frontend with SSR and static optimization
- **PostgreSQL**: Primary database with automated backups and monitoring
- **Redis**: Caching layer with persistence and high availability
- **NGINX Ingress**: Load balancing and TLS termination

### Supporting Infrastructure

- **Monitoring**: Prometheus, Grafana, and Alertmanager stack
- **Backup System**: Automated PostgreSQL backups with retention policies
- **Security**: NetworkPolicies, RBAC, and Pod Security Standards
- **Scaling**: HorizontalPodAutoscaler with custom metrics

## 🚀 Quick Start

### Prerequisites

- Kubernetes cluster (1.25+)
- kubectl configured with cluster access
- Container registry access (Azure Container Registry)
- Storage classes: `fast-ssd` and `standard`
- Ingress controller (NGINX recommended)
- cert-manager for TLS certificates (optional)

### Deployment

1. **Deploy to Development**:
   ```bash
   ./scripts/deploy.sh -e development
   ```

2. **Deploy to Production**:
   ```bash
   ./scripts/deploy.sh -e production
   ```

3. **Deploy with Monitoring**:
   ```bash
   ./scripts/deploy.sh -e production --monitoring
   ```

4. **Dry Run**:
   ```bash
   ./scripts/deploy.sh -e production --dry-run
   ```

## 🔧 Configuration

### Environment Variables

Update the following files before deployment:

- `base/secret-app.yaml`: Application secrets
- `base/configmap-app.yaml`: Application configuration
- `overlays/{env}/kustomization.yaml`: Environment-specific settings

### Key Configurations

#### Storage
- **PostgreSQL**: 100Gi (production), 20Gi (development)
- **Redis**: 20Gi (production), 5Gi (development)
- **Monitoring**: 50Gi Prometheus, 10Gi Grafana

#### Scaling
- **API**: 2-20 replicas (production), 1-3 (development)
- **Web**: 2-15 replicas (production), 1-3 (development)
- **Database**: Single instance with high availability features

#### Security
- **RBAC**: Least-privilege access control
- **NetworkPolicies**: Zero-trust network segmentation
- **Pod Security**: Non-root users, read-only filesystems
- **Secrets**: Encrypted at rest, mounted as volumes

## 📊 Monitoring and Observability

### Metrics Collection
- **Application Metrics**: Custom business metrics via Prometheus endpoints
- **Infrastructure Metrics**: Node, pod, and container metrics
- **Database Metrics**: PostgreSQL performance and connection metrics
- **Cache Metrics**: Redis performance and memory usage

### Alerting Rules
- **Critical**: Service down, database unavailable
- **Warning**: High resource usage, slow response times
- **Info**: Scaling events, backup completions

### Dashboards
- **KGV Overview**: Application health and performance
- **Infrastructure**: Cluster resource utilization
- **Database**: PostgreSQL performance metrics
- **Security**: Network policy violations and access patterns

## 💾 Backup and Recovery

### Automated Backups
- **Daily**: Full database backup at 2 AM (retention: 30 days)
- **Weekly**: Schema + data backup on Sundays (retention: 90 days)
- **Monitoring**: Daily backup verification and reporting

### Manual Operations
```bash
# Create manual backup
./scripts/manage.sh backup --create

# List available backups
./scripts/manage.sh backup --list

# Restore from backup
./scripts/manage.sh restore --file /backups/postgres/backup_20231201.sql.custom
```

### Disaster Recovery
1. **Database Restore**: Use backup CronJobs or manual restore procedures
2. **Application Recovery**: Redeploy using Kustomize configurations
3. **Monitoring Recovery**: Prometheus data retention and Grafana dashboard restore

## 🔐 Security

### Network Security
- **NetworkPolicies**: Deny-all default with explicit allow rules
- **TLS**: End-to-end encryption with cert-manager integration
- **Ingress Security**: Rate limiting, WAF rules, and security headers

### Access Control
- **RBAC**: Fine-grained permissions for service accounts
- **ServiceAccounts**: Dedicated accounts for each component
- **Pod Security**: Security contexts and admission controllers

### Secrets Management
- **Kubernetes Secrets**: Encrypted storage for sensitive data
- **External Secrets**: Integration with cloud key management services
- **Rotation**: Automated secret rotation procedures

## 📈 Scaling and Performance

### Horizontal Pod Autoscaling
- **CPU-based**: Scale based on CPU utilization (70% threshold)
- **Memory-based**: Scale based on memory usage (80% threshold)
- **Custom Metrics**: Scale based on application-specific metrics

### Resource Management
- **Requests**: Guaranteed resource allocation
- **Limits**: Maximum resource consumption
- **Quality of Service**: Guaranteed QoS for critical components

### Performance Optimization
- **Caching**: Redis for session and application data
- **Database**: Connection pooling and query optimization
- **Static Assets**: CDN integration and edge caching

## 🛠️ Management Scripts

### Deploy Script (`scripts/deploy.sh`)
Complete deployment automation with environment-specific configurations.

```bash
# Development deployment
./scripts/deploy.sh -e development

# Production deployment with full stack
./scripts/deploy.sh -e production --monitoring --backup

# Staging with custom namespace
./scripts/deploy.sh -e staging -n kgv-staging-custom
```

### Management Script (`scripts/manage.sh`)
Day-to-day operations and troubleshooting.

```bash
# View application status
./scripts/manage.sh status -e production

# Scale web component
./scripts/manage.sh scale -c web -r 5 -e production

# View API logs
./scripts/manage.sh logs -c api -e production

# Troubleshoot PostgreSQL
./scripts/manage.sh troubleshoot -c postgres -e production
```

## 🌍 Multi-Environment Support

### Development Environment
- **Namespace**: `kgv-dev`
- **Resources**: Minimal resource allocation
- **Features**: Debug logging, development tools enabled
- **Access**: Local port-forwarding or ingress

### Staging Environment
- **Namespace**: `kgv-staging`
- **Resources**: Production-like resource allocation
- **Features**: Production configuration with staging data
- **Access**: Staging domain with authentication

### Production Environment
- **Namespace**: `kgv-system`
- **Resources**: Full resource allocation with autoscaling
- **Features**: Production hardening, monitoring, backups
- **Access**: Production domain with full security

## 🔄 CI/CD Integration

### GitOps Workflow
1. **Code Changes**: Push to feature branch
2. **CI Pipeline**: Build and test containers
3. **Image Push**: Push to container registry
4. **Deploy**: Update Kustomize overlay with new image tags
5. **Verification**: Automated smoke tests and health checks

### Image Management
- **Registry**: Azure Container Registry (`acrkgvprod.azurecr.io`)
- **Tagging**: Semantic versioning with environment tags
- **Security**: Image scanning and vulnerability management

## 📋 Maintenance Procedures

### Regular Maintenance
- **Weekly**: Review monitoring alerts and performance metrics
- **Monthly**: Update container images and security patches
- **Quarterly**: Disaster recovery testing and backup verification

### Emergency Procedures
- **Incident Response**: Automated alerting and escalation procedures
- **Recovery**: Database restore and application rollback procedures
- **Communication**: Status page updates and stakeholder notifications

## 🤝 Contributing

### Development Workflow
1. Create feature branch
2. Update Kubernetes manifests
3. Test in development environment
4. Create pull request with deployment verification
5. Deploy to staging for integration testing

### Best Practices
- **Security First**: All changes must pass security reviews
- **Documentation**: Update README and inline documentation
- **Testing**: Verify changes in non-production environments
- **Monitoring**: Ensure observability for new features

## 📞 Support and Troubleshooting

### Common Issues
- **Pod Stuck in Pending**: Check resource requests and node capacity
- **Service Unavailable**: Verify service selectors and endpoint health
- **Database Connection Issues**: Check network policies and credentials
- **High Memory Usage**: Review application metrics and scaling policies

### Support Contacts
- **Platform Team**: k8s-platform@kgv.example.com
- **Development Team**: dev-team@kgv.example.com
- **Operations**: ops@kgv.example.com

### Documentation Links
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Kustomize Documentation](https://kustomize.io/)
- [Prometheus Operator](https://prometheus-operator.dev/)
- [NGINX Ingress Controller](https://kubernetes.github.io/ingress-nginx/)

---

**Note**: This infrastructure is designed for production workloads with high availability, security, and observability requirements. Ensure all security configurations are reviewed and updated according to your organization's policies before deployment.