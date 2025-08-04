# KGV Database Migration Runbook

## Overview
This runbook provides step-by-step instructions for migrating the KGV (Kleingartenverein) management system from SQL Server to PostgreSQL, including rollback procedures and validation steps.

## Pre-Migration Checklist

### System Requirements
- [ ] PostgreSQL 15+ installed and configured
- [ ] pgloader or equivalent ETL tool available
- [ ] Sufficient disk space (estimate 3x source database size)
- [ ] Network connectivity between source and target systems
- [ ] Administrative access to both database systems
- [ ] Application services can be stopped for migration window

### Backup Requirements
- [ ] Full SQL Server backup completed and verified
- [ ] Transaction log backup chain intact
- [ ] PostgreSQL cluster backup taken (if existing data)
- [ ] All backup files stored in secure, accessible location
- [ ] Recovery procedures tested and documented

### Team Coordination
- [ ] Migration team assembled and roles assigned
- [ ] Communication plan established
- [ ] Rollback decision authority identified
- [ ] Stakeholders notified of migration schedule
- [ ] Emergency contact information available

## Migration Process

### Phase 1: Pre-Migration Setup (Duration: 30 minutes)

#### Step 1.1: Initialize PostgreSQL Environment
```bash
# Execute PostgreSQL schema setup
psql -d kgv_production -f postgresql/01_schema_core.sql
psql -d kgv_production -f postgresql/02_schema_sequences.sql
psql -d kgv_production -f etl/01_data_type_mapping.sql
```

**Validation:**
```sql
-- Verify schema creation
SELECT schemaname, tablename, tableowner 
FROM pg_tables 
WHERE schemaname IN ('public', 'migration_staging', 'data_quality', 'backup_management');
```

**Expected Result:** All core tables created successfully

#### Step 1.2: Prepare Migration Staging
```bash
# Generate unique batch ID for migration
BATCH_ID=$(date +%Y%m%d%H%M%S)
echo "Migration Batch ID: $BATCH_ID"

# Create migration log entry
psql -d kgv_production -c "
SELECT migration_staging.log_migration_step(
    $BATCH_ID, 
    'MIGRATION_START', 
    'STARTED', 
    'SUCCESS', 
    'Migration batch $BATCH_ID initiated'
);"
```

### Phase 2: Data Extraction (Duration: 45 minutes)

#### Step 2.1: Extract SQL Server Data
```bash
# Export SQL Server data to CSV files
# Run on SQL Server machine or with SQL Server client tools

# Districts (Bezirk)
bcp "SELECT * FROM [kgv].[dbo].[Bezirk]" queryout "bezirk.csv" -c -t, -S server -d kgv -T

# Applications (Antrag) 
bcp "SELECT * FROM [kgv].[dbo].[Antrag]" queryout "antrag.csv" -c -t, -S server -d kgv -T

# All other tables...
bcp "SELECT * FROM [kgv].[dbo].[Aktenzeichen]" queryout "aktenzeichen.csv" -c -t, -S server -d kgv -T
bcp "SELECT * FROM [kgv].[dbo].[Bezirke_Katasterbezirke]" queryout "bezirke_katasterbezirke.csv" -c -t, -S server -d kgv -T
bcp "SELECT * FROM [kgv].[dbo].[Eingangsnummer]" queryout "eingangsnummer.csv" -c -t, -S server -d kgv -T
bcp "SELECT * FROM [kgv].[dbo].[Katasterbezirk]" queryout "katasterbezirk.csv" -c -t, -S server -d kgv -T
bcp "SELECT * FROM [kgv].[dbo].[Kennungen]" queryout "kennungen.csv" -c -t, -S server -d kgv -T
bcp "SELECT * FROM [kgv].[dbo].[Mischenfelder]" queryout "mischenfelder.csv" -c -t, -S server -d kgv -T
bcp "SELECT * FROM [kgv].[dbo].[Personen]" queryout "personen.csv" -c -t, -S server -d kgv -T
bcp "SELECT * FROM [kgv].[dbo].[Verlauf]" queryout "verlauf.csv" -c -t, -S server -d kgv -T
```

#### Step 2.2: Transfer Data Files
```bash
# Secure copy data files to PostgreSQL server
scp *.csv user@postgresql-server:/tmp/migration/

# Verify file integrity
md5sum *.csv > checksums.md5
scp checksums.md5 user@postgresql-server:/tmp/migration/
```

**Validation:**
```bash
# On PostgreSQL server
cd /tmp/migration
md5sum -c checksums.md5
wc -l *.csv  # Record counts for validation
```

### Phase 3: Data Loading (Duration: 60 minutes)

#### Step 3.1: Load Data into Staging Tables
```bash
# Load data into staging tables
psql -d kgv_production -c "
-- Set batch ID for all staging records
\set batch_id $BATCH_ID

-- Load districts
COPY migration_staging.raw_bezirk (bez_ID, bez_Name, migration_batch_id) 
FROM '/tmp/migration/bezirk.csv' 
WITH (FORMAT csv, HEADER true, DELIMITER ',');

-- Load applications
COPY migration_staging.raw_antrag (
    an_ID, an_Aktenzeichen, an_WartelistenNr32, an_WartelistenNr33,
    an_Anrede, an_Titel, an_Vorname, an_Nachname,
    an_Anrede2, an_Titel2, an_Vorname2, an_Nachname2,
    an_Briefanrede, an_Strasse, an_PLZ, an_Ort,
    an_Telefon, an_MobilTelefon, an_GeschTelefon,
    an_Bewerbungsdatum, an_Bestaetigungsdatum, an_AktuellesAngebot, an_Loeschdatum,
    an_Wunsch, an_Vermerk, an_Aktiv, an_DeaktiviertAm,
    an_Geburtstag, an_Geburtstag2, an_MobilTelefon2, an_EMail,
    migration_batch_id
) FROM '/tmp/migration/antrag.csv' 
WITH (FORMAT csv, HEADER true, DELIMITER ',');

-- Continue for all other tables...
"
```

**Validation:**
```sql
-- Verify staging data counts
SELECT 
    'raw_bezirk' as table_name, 
    COUNT(*) as record_count 
FROM migration_staging.raw_bezirk 
WHERE migration_batch_id = :batch_id

UNION ALL

SELECT 
    'raw_antrag', 
    COUNT(*) 
FROM migration_staging.raw_antrag 
WHERE migration_batch_id = :batch_id;

-- Compare with source system counts
```

#### Step 3.2: Transform and Load into Target Schema
```sql
-- Execute full migration transformation
SELECT * FROM migration_staging.run_full_migration(:batch_id);
```

**Expected Output:**
```
entity                | status  | records_processed | records_success | records_error | duration_seconds
Districts            | SUCCESS | 25                | 25              | 0             | 2
Cadastral Districts  | SUCCESS | 150               | 150             | 0             | 5
Users                | SUCCESS | 12                | 12              | 0             | 3
File References      | SUCCESS | 5000              | 5000            | 0             | 15
Entry Numbers        | SUCCESS | 3000              | 3000            | 0             | 12
Applications         | SUCCESS | 12000             | 11950           | 50            | 45
Application History  | SUCCESS | 25000             | 24800           | 200           | 30
Misc Entities        | SUCCESS | 200               | 200             | 0             | 5
```

### Phase 4: Data Quality Validation (Duration: 30 minutes)

#### Step 4.1: Execute Data Quality Checks
```sql
-- Run comprehensive data quality validation
SELECT * FROM data_quality.execute_all_rules(:batch_id);
```

#### Step 4.2: Review and Address Issues
```sql
-- Get detailed violation report
SELECT * FROM data_quality.generate_report(:batch_id);

-- Review specific violations if any
SELECT * FROM data_quality.get_rule_violations('applications_email_format', :batch_id);
```

**Acceptance Criteria:**
- No ERROR-level violations
- WARNING-level violations < 5% of total records
- All referential integrity checks pass

### Phase 5: Performance Optimization (Duration: 20 minutes)

#### Step 5.1: Update Statistics and Refresh Views
```sql
-- Update table statistics
ANALYZE;

-- Refresh materialized views
SELECT * FROM refresh_performance_views();

-- Update sequence counters to prevent conflicts
SELECT reset_sequence('file_reference', 'B1', EXTRACT(YEAR FROM NOW()), 
    (SELECT MAX(number) + 1 FROM file_references WHERE district_code = 'B1' AND year = EXTRACT(YEAR FROM NOW()))
);
```

#### Step 5.2: Performance Validation
```sql
-- Test performance of critical queries
EXPLAIN (ANALYZE, BUFFERS) 
SELECT * FROM applications 
WHERE is_active = true 
  AND waiting_list_number_32 IS NOT NULL 
ORDER BY waiting_list_number_32::INTEGER 
LIMIT 100;

-- Verify index usage
SELECT * FROM analyze_query_performance();
```

### Phase 6: Application Integration (Duration: 45 minutes)

#### Step 6.1: Update Connection Strings
```bash
# Update application configuration
# Edit appsettings.json or equivalent configuration file
sed -i 's/Server=sql-server/Host=postgresql-server/' /app/config/appsettings.json
sed -i 's/Database=kgv_sqlserver/Database=kgv_production/' /app/config/appsettings.json
```

#### Step 6.2: Test Application Connectivity
```bash
# Start application in test mode
./start_application.sh --test-mode

# Execute connectivity tests
curl http://localhost/api/health
curl http://localhost/api/districts
curl http://localhost/api/applications?limit=10
```

### Phase 7: Go-Live Validation (Duration: 30 minutes)

#### Step 7.1: Functional Testing
- [ ] User login functionality
- [ ] Application submission process
- [ ] Waiting list calculation
- [ ] Report generation
- [ ] Administrative functions

#### Step 7.2: Performance Testing
- [ ] Response times within acceptable limits
- [ ] Concurrent user handling
- [ ] Database connection pooling

#### Step 7.3: Final Verification
```sql
-- Final data consistency check
SELECT * FROM backup_management.verify_data_consistency();

-- Create go-live checkpoint
INSERT INTO backup_management.recovery_checkpoints (
    checkpoint_name, description, checkpoint_time, lsn_position
) VALUES (
    'go_live_' || :batch_id,
    'Go-live checkpoint for migration batch ' || :batch_id,
    NOW(),
    pg_current_wal_lsn()
);
```

## Rollback Procedures

### Scenario 1: Pre-Go-Live Rollback (Application not yet switched)

**Duration:** 15 minutes
**Risk Level:** Low

1. **Stop Migration Process**
   ```bash
   # Cancel any running migration jobs
   pkill -f migration
   
   # Mark migration as cancelled
   psql -d kgv_production -c "
   UPDATE migration_staging.migration_log 
   SET status = 'CANCELLED' 
   WHERE batch_id = $BATCH_ID AND status = 'RUNNING';
   "
   ```

2. **Clean Up PostgreSQL**
   ```sql
   -- Drop migrated data (keep schema for future attempts)
   TRUNCATE applications CASCADE;
   TRUNCATE application_history;
   TRUNCATE users CASCADE;
   TRUNCATE districts CASCADE;
   TRUNCATE file_references;
   TRUNCATE entry_numbers;
   ```

3. **Verify SQL Server System**
   ```sql
   -- Verify SQL Server is operational
   SELECT COUNT(*) FROM [dbo].[Antrag];
   SELECT GETDATE();
   ```

### Scenario 2: Post-Go-Live Rollback (Application switched to PostgreSQL)

**Duration:** 45 minutes
**Risk Level:** High

1. **Immediate Actions (5 minutes)**
   ```bash
   # Stop application services immediately
   systemctl stop kgv-application
   
   # Switch application back to SQL Server
   sed -i 's/Host=postgresql-server/Server=sql-server/' /app/config/appsettings.json
   sed -i 's/Database=kgv_production/Database=kgv_sqlserver/' /app/config/appsettings.json
   ```

2. **Data Synchronization Assessment (10 minutes)**
   ```sql
   -- Check if any new data was created in PostgreSQL
   SELECT COUNT(*) FROM applications WHERE created_at > 'GO_LIVE_TIMESTAMP';
   SELECT COUNT(*) FROM application_history WHERE created_at > 'GO_LIVE_TIMESTAMP';
   ```

3. **Data Recovery Process (25 minutes)**
   
   **If No New Data Created:**
   ```bash
   # Simply restart application with SQL Server
   systemctl start kgv-application
   ```
   
   **If New Data Created:**
   ```bash
   # Extract new data from PostgreSQL
   pg_dump -d kgv_production -t applications -t application_history \
     --where="created_at > 'GO_LIVE_TIMESTAMP'" \
     -f /tmp/rollback_data.sql
   
   # Convert and import to SQL Server (manual process required)
   # This requires custom scripts for data type conversion
   ```

4. **Verification (5 minutes)**
   ```sql
   -- Verify SQL Server functionality
   SELECT COUNT(*) FROM [dbo].[Antrag] WHERE an_Aktiv = '1';
   
   -- Test critical functions
   EXEC sp_who2;
   ```

### Scenario 3: Emergency Rollback (System Failure)

**Duration:** 60 minutes
**Risk Level:** Critical

1. **Immediate System Recovery**
   ```bash
   # Activate disaster recovery procedure
   psql -d postgres -c "
   SELECT * FROM backup_management.initiate_disaster_recovery('go_live_$BATCH_ID');
   "
   ```

2. **Follow Disaster Recovery Runbook**
   - Execute steps from disaster recovery procedure
   - Restore from last known good backup
   - Verify data consistency
   - Restore application services

## Post-Migration Tasks

### Day 1 Activities
- [ ] Monitor application performance
- [ ] Verify user access and functionality
- [ ] Check error logs for issues
- [ ] Backup PostgreSQL database
- [ ] Update monitoring systems

### Week 1 Activities
- [ ] Performance tuning based on usage patterns
- [ ] User feedback collection and issue resolution
- [ ] Capacity planning review
- [ ] Documentation updates

### Month 1 Activities
- [ ] Comprehensive performance review
- [ ] Optimize queries based on real usage
- [ ] Archive SQL Server system (if satisfied)
- [ ] Update disaster recovery procedures

## Success Criteria

### Technical Success Criteria
- [ ] All data migrated with < 0.1% error rate
- [ ] Application response times within 10% of baseline
- [ ] Zero data loss during migration
- [ ] All business processes functional
- [ ] Backup and recovery procedures tested

### Business Success Criteria
- [ ] Users can perform all daily tasks
- [ ] Reports generate correctly
- [ ] Waiting list calculations accurate
- [ ] No impact on customer service
- [ ] Stakeholder approval received

## Emergency Contacts

| Role | Name | Phone | Email |
|------|------|-------|-------|
| Migration Lead | [Name] | [Phone] | [Email] |
| Database Administrator | [Name] | [Phone] | [Email] |
| Application Owner | [Name] | [Phone] | [Email] |
| Infrastructure Team | [Name] | [Phone] | [Email] |
| Business Stakeholder | [Name] | [Phone] | [Email] |

## Lessons Learned Template

After migration completion, document:

### What Went Well
- [List successful aspects]

### What Could Be Improved
- [List areas for improvement]

### Recommendations for Future Migrations
- [List recommendations]

### Time and Resource Utilization
- Planned vs. Actual Duration: [X hours vs Y hours]
- Team Size: [X people]
- Key Success Factors: [List factors]

---

**Document Version:** 1.0  
**Last Updated:** $(date)  
**Next Review Date:** $(date -d "+3 months")  
**Approved By:** [Name and Date]