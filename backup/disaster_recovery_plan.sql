-- =============================================================================
-- KGV Migration: Backup and Disaster Recovery Plan
-- Version: 1.0
-- Description: Comprehensive backup and disaster recovery strategies
-- =============================================================================

-- =============================================================================
-- BACKUP STRATEGY CONFIGURATION
-- =============================================================================

-- Create schema for backup management
CREATE SCHEMA IF NOT EXISTS backup_management;

-- Table to track backup operations
CREATE TABLE backup_management.backup_log (
    id BIGSERIAL PRIMARY KEY,
    backup_type VARCHAR(20) NOT NULL, -- 'FULL', 'INCREMENTAL', 'WAL_ARCHIVE', 'SNAPSHOT'
    backup_method VARCHAR(20) NOT NULL, -- 'pg_dump', 'pg_basebackup', 'wal-g', 'custom'
    database_name VARCHAR(100) NOT NULL,
    backup_location TEXT NOT NULL,
    backup_size_bytes BIGINT,
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    end_time TIMESTAMP WITH TIME ZONE,
    status VARCHAR(20) NOT NULL DEFAULT 'RUNNING', -- 'RUNNING', 'SUCCESS', 'FAILED', 'CANCELLED'
    error_message TEXT,
    retention_until DATE,
    metadata JSONB,
    
    CONSTRAINT backup_log_type_check 
        CHECK (backup_type IN ('FULL', 'INCREMENTAL', 'WAL_ARCHIVE', 'SNAPSHOT')),
    CONSTRAINT backup_log_method_check 
        CHECK (backup_method IN ('pg_dump', 'pg_basebackup', 'wal-g', 'custom')),
    CONSTRAINT backup_log_status_check 
        CHECK (status IN ('RUNNING', 'SUCCESS', 'FAILED', 'CANCELLED'))
);

-- Indexes for backup log
CREATE INDEX idx_backup_log_start_time ON backup_management.backup_log(start_time DESC);
CREATE INDEX idx_backup_log_status ON backup_management.backup_log(status);
CREATE INDEX idx_backup_log_type ON backup_management.backup_log(backup_type);
CREATE INDEX idx_backup_log_retention ON backup_management.backup_log(retention_until) WHERE retention_until IS NOT NULL;

-- Table for disaster recovery checkpoints
CREATE TABLE backup_management.recovery_checkpoints (
    id BIGSERIAL PRIMARY KEY,
    checkpoint_name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    checkpoint_time TIMESTAMP WITH TIME ZONE NOT NULL,
    lsn_position PG_LSN NOT NULL, -- Log sequence number
    backup_reference_id BIGINT, -- Reference to backup_log
    data_consistency_verified BOOLEAN NOT NULL DEFAULT false,
    verification_time TIMESTAMP WITH TIME ZONE,
    recovery_tested BOOLEAN NOT NULL DEFAULT false,
    last_recovery_test TIMESTAMP WITH TIME ZONE,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_recovery_checkpoints_backup 
        FOREIGN KEY (backup_reference_id) REFERENCES backup_management.backup_log(id) 
        ON DELETE SET NULL ON UPDATE CASCADE
);

-- Index for recovery checkpoints
CREATE INDEX idx_recovery_checkpoints_time ON backup_management.recovery_checkpoints(checkpoint_time DESC);
CREATE INDEX idx_recovery_checkpoints_verified ON backup_management.recovery_checkpoints(data_consistency_verified);

-- =============================================================================
-- BACKUP FUNCTIONS
-- =============================================================================

-- Function to initiate a full database backup
CREATE OR REPLACE FUNCTION backup_management.create_full_backup(
    p_backup_location TEXT,
    p_compression_level INTEGER DEFAULT 6,
    p_retention_days INTEGER DEFAULT 30
) RETURNS BIGINT AS $$
DECLARE
    v_backup_id BIGINT;
    v_database_name TEXT;
    v_backup_filename TEXT;
    v_retention_date DATE;
BEGIN
    -- Get current database name
    SELECT current_database() INTO v_database_name;
    
    -- Calculate retention date
    v_retention_date := CURRENT_DATE + p_retention_days;
    
    -- Generate backup filename with timestamp
    v_backup_filename := format('%s/kgv_full_backup_%s_%s.sql.gz',
        p_backup_location,
        v_database_name,
        to_char(NOW(), 'YYYY-MM-DD_HH24-MI-SS')
    );
    
    -- Insert backup log entry
    INSERT INTO backup_management.backup_log (
        backup_type, backup_method, database_name, backup_location,
        start_time, retention_until, metadata
    ) VALUES (
        'FULL', 'pg_dump', v_database_name, v_backup_filename,
        NOW(), v_retention_date,
        jsonb_build_object(
            'compression_level', p_compression_level,
            'format', 'custom',
            'include_data', true,
            'include_schema', true
        )
    ) RETURNING id INTO v_backup_id;
    
    -- Note: Actual pg_dump execution would be handled by external scripts
    -- This function provides the framework and logging
    
    RAISE NOTICE 'Full backup initiated with ID %. Use external script to execute: pg_dump -Fc -Z% -f % %',
        v_backup_id, p_compression_level, v_backup_filename, v_database_name;
    
    RETURN v_backup_id;
END;
$$ LANGUAGE plpgsql;

-- Function to log backup completion
CREATE OR REPLACE FUNCTION backup_management.complete_backup(
    p_backup_id BIGINT,
    p_status VARCHAR(20),
    p_backup_size_bytes BIGINT DEFAULT NULL,
    p_error_message TEXT DEFAULT NULL
) RETURNS BOOLEAN AS $$
BEGIN
    UPDATE backup_management.backup_log 
    SET 
        end_time = NOW(),
        status = p_status,
        backup_size_bytes = p_backup_size_bytes,
        error_message = p_error_message
    WHERE id = p_backup_id;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Backup with ID % not found', p_backup_id;
    END IF;
    
    -- Create recovery checkpoint for successful full backups
    IF p_status = 'SUCCESS' THEN
        INSERT INTO backup_management.recovery_checkpoints (
            checkpoint_name,
            description,
            checkpoint_time,
            lsn_position,
            backup_reference_id
        ) VALUES (
            format('full_backup_%s', p_backup_id),
            format('Recovery checkpoint for full backup ID %s', p_backup_id),
            NOW(),
            pg_current_wal_lsn(),
            p_backup_id
        );
    END IF;
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- Function to create application-specific backup (critical data only)
CREATE OR REPLACE FUNCTION backup_management.create_critical_data_backup(
    p_backup_location TEXT
) RETURNS BIGINT AS $$
DECLARE
    v_backup_id BIGINT;
    v_database_name TEXT;
    v_backup_filename TEXT;
    v_table_list TEXT[];
BEGIN
    -- Get current database name
    SELECT current_database() INTO v_database_name;
    
    -- Define critical tables for KGV system
    v_table_list := ARRAY[
        'districts',
        'cadastral_districts', 
        'applications',
        'application_history',
        'users',
        'file_references',
        'entry_numbers',
        'number_sequences'
    ];
    
    -- Generate backup filename
    v_backup_filename := format('%s/kgv_critical_backup_%s_%s.sql',
        p_backup_location,
        v_database_name,
        to_char(NOW(), 'YYYY-MM-DD_HH24-MI-SS')
    );
    
    -- Insert backup log entry
    INSERT INTO backup_management.backup_log (
        backup_type, backup_method, database_name, backup_location,
        start_time, retention_until, metadata
    ) VALUES (
        'INCREMENTAL', 'pg_dump', v_database_name, v_backup_filename,
        NOW(), CURRENT_DATE + 7, -- 7-day retention for critical backups
        jsonb_build_object(
            'tables', v_table_list,
            'data_only', false,
            'critical_backup', true
        )
    ) RETURNING id INTO v_backup_id;
    
    RAISE NOTICE 'Critical data backup initiated with ID %. Tables: %', v_backup_id, array_to_string(v_table_list, ', ');
    
    RETURN v_backup_id;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- DISASTER RECOVERY FUNCTIONS
-- =============================================================================

-- Function to verify data consistency after recovery
CREATE OR REPLACE FUNCTION backup_management.verify_data_consistency()
RETURNS TABLE(
    check_name TEXT,
    status TEXT,
    details TEXT,
    record_count BIGINT
) AS $$
BEGIN
    -- Check referential integrity
    RETURN QUERY
    WITH integrity_checks AS (
        SELECT 
            'cadastral_districts_reference' as check_name,
            CASE 
                WHEN COUNT(*) = 0 THEN 'PASS'
                ELSE 'FAIL'
            END as status,
            CASE 
                WHEN COUNT(*) = 0 THEN 'All cadastral districts reference valid districts'
                ELSE format('%s cadastral districts reference invalid districts', COUNT(*))
            END as details,
            COUNT(*) as record_count
        FROM cadastral_districts cd 
        LEFT JOIN districts d ON cd.district_id = d.id 
        WHERE d.id IS NULL
        
        UNION ALL
        
        SELECT 
            'application_history_reference',
            CASE 
                WHEN COUNT(*) = 0 THEN 'PASS'
                ELSE 'FAIL'
            END,
            CASE 
                WHEN COUNT(*) = 0 THEN 'All application history records reference valid applications'
                ELSE format('%s history records reference invalid applications', COUNT(*))
            END,
            COUNT(*)
        FROM application_history ah 
        LEFT JOIN applications a ON ah.application_id = a.id 
        WHERE a.id IS NULL
        
        UNION ALL
        
        SELECT 
            'unique_constraints',
            CASE 
                WHEN SUM(violation_count) = 0 THEN 'PASS'
                ELSE 'FAIL'
            END,
            CASE 
                WHEN SUM(violation_count) = 0 THEN 'All unique constraints satisfied'
                ELSE format('%s unique constraint violations found', SUM(violation_count))
            END,
            SUM(violation_count)
        FROM (
            SELECT COUNT(*) - COUNT(DISTINCT name) as violation_count FROM districts WHERE is_active = true
            UNION ALL
            SELECT COUNT(*) - COUNT(DISTINCT (district_code, number, year)) FROM file_references WHERE is_active = true
            UNION ALL
            SELECT COUNT(*) - COUNT(DISTINCT (district_code, number, year)) FROM entry_numbers WHERE is_active = true
        ) violations
        
        UNION ALL
        
        SELECT 
            'data_completeness',
            CASE 
                WHEN COUNT(*) = 0 THEN 'PASS'
                ELSE 'WARN'
            END,
            CASE 
                WHEN COUNT(*) = 0 THEN 'All critical fields populated'
                ELSE format('%s applications missing critical data', COUNT(*))
            END,
            COUNT(*)
        FROM applications 
        WHERE is_active = true 
          AND (first_name IS NULL OR last_name IS NULL OR TRIM(first_name) = '' OR TRIM(last_name) = '')
    )
    SELECT ic.check_name, ic.status, ic.details, ic.record_count
    FROM integrity_checks ic;
END;
$$ LANGUAGE plpgsql;

-- Function to create a recovery test environment
CREATE OR REPLACE FUNCTION backup_management.prepare_recovery_test(
    p_test_database_name TEXT,
    p_backup_file TEXT
) RETURNS TEXT AS $$
DECLARE
    v_recovery_script TEXT;
BEGIN
    -- Generate recovery test script
    v_recovery_script := format('
-- Recovery Test Script for KGV Database
-- Generated: %s
-- Target Database: %s
-- Source Backup: %s

-- Step 1: Create test database
CREATE DATABASE %I;

-- Step 2: Restore from backup
-- Execute in shell: pg_restore -d %I %L

-- Step 3: Verify data consistency
\c %I
SELECT * FROM backup_management.verify_data_consistency();

-- Step 4: Test critical functions
SELECT get_next_file_reference_number(''B1'', EXTRACT(YEAR FROM NOW())::INTEGER);
SELECT calculate_waiting_list_position(1, ''32'');

-- Step 5: Performance verification
SELECT * FROM analyze_query_performance();

-- Step 6: Cleanup (after testing)
-- DROP DATABASE %I;
    ', 
    NOW()::TEXT,
    p_test_database_name,
    p_backup_file,
    p_test_database_name,
    p_test_database_name,
    p_backup_file,
    p_test_database_name,
    p_test_database_name
    );
    
    RETURN v_recovery_script;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- BACKUP MAINTENANCE AND CLEANUP
-- =============================================================================

-- Function to clean up expired backups
CREATE OR REPLACE FUNCTION backup_management.cleanup_expired_backups()
RETURNS TABLE(
    backup_id BIGINT,
    backup_location TEXT,
    retention_until DATE,
    cleanup_action TEXT
) AS $$
BEGIN
    RETURN QUERY
    UPDATE backup_management.backup_log 
    SET 
        status = 'EXPIRED',
        metadata = COALESCE(metadata, '{}'::JSONB) || jsonb_build_object('expired_at', NOW())
    WHERE retention_until < CURRENT_DATE
      AND status = 'SUCCESS'
    RETURNING id, backup_location, retention_until, 'MARKED_EXPIRED' as cleanup_action;
    
    -- Note: Actual file deletion should be handled by external scripts
    -- for safety reasons
END;
$$ LANGUAGE plpgsql;

-- Function to generate backup status report
CREATE OR REPLACE FUNCTION backup_management.get_backup_status_report()
RETURNS TABLE(
    backup_type VARCHAR(20),
    last_successful_backup TIMESTAMP WITH TIME ZONE,
    backup_age_hours INTEGER,
    total_backups_7days INTEGER,
    failed_backups_7days INTEGER,
    avg_backup_size_gb NUMERIC,
    oldest_retained_backup DATE
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        bl.backup_type,
        MAX(bl.end_time) FILTER (WHERE bl.status = 'SUCCESS') as last_successful_backup,
        EXTRACT(HOURS FROM (NOW() - MAX(bl.end_time) FILTER (WHERE bl.status = 'SUCCESS')))::INTEGER as backup_age_hours,
        COUNT(*) FILTER (WHERE bl.start_time >= NOW() - INTERVAL '7 days')::INTEGER as total_backups_7days,
        COUNT(*) FILTER (WHERE bl.start_time >= NOW() - INTERVAL '7 days' AND bl.status = 'FAILED')::INTEGER as failed_backups_7days,
        ROUND(AVG(bl.backup_size_bytes) FILTER (WHERE bl.status = 'SUCCESS' AND bl.backup_size_bytes IS NOT NULL) / (1024^3), 2) as avg_backup_size_gb,
        MIN(bl.retention_until) FILTER (WHERE bl.status = 'SUCCESS' AND bl.retention_until >= CURRENT_DATE) as oldest_retained_backup
    FROM backup_management.backup_log bl
    GROUP BY bl.backup_type
    ORDER BY bl.backup_type;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- DISASTER RECOVERY PROCEDURES
-- =============================================================================

-- Main disaster recovery procedure
CREATE OR REPLACE FUNCTION backup_management.initiate_disaster_recovery(
    p_recovery_point_name TEXT,
    p_target_time TIMESTAMP WITH TIME ZONE DEFAULT NULL
) RETURNS TABLE(
    step_number INTEGER,
    step_name TEXT,
    status TEXT,
    instructions TEXT,
    estimated_duration INTERVAL
) AS $$
DECLARE
    v_checkpoint RECORD;
    v_target_time TIMESTAMP WITH TIME ZONE;
BEGIN
    -- Use provided time or current time
    v_target_time := COALESCE(p_target_time, NOW());
    
    -- Find appropriate recovery checkpoint
    SELECT * INTO v_checkpoint
    FROM backup_management.recovery_checkpoints
    WHERE checkpoint_name = p_recovery_point_name
       OR (p_recovery_point_name IS NULL AND checkpoint_time <= v_target_time)
    ORDER BY checkpoint_time DESC
    LIMIT 1;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'No suitable recovery checkpoint found for point: %', COALESCE(p_recovery_point_name, v_target_time::TEXT);
    END IF;
    
    -- Return disaster recovery plan
    RETURN QUERY VALUES
        (1, 'Assess Damage', 'MANUAL', 
         'Determine scope of data loss and system availability. Document current state.', 
         '15 minutes'::INTERVAL),
        
        (2, 'Stop Application Services', 'MANUAL',
         'Stop all application services to prevent further data corruption.', 
         '5 minutes'::INTERVAL),
        
        (3, 'Verify Backup Integrity', 'AUTOMATED',
         format('Verify backup file integrity for: %s', v_checkpoint.backup_reference_id),
         '10 minutes'::INTERVAL),
        
        (4, 'Create Recovery Database', 'MANUAL',
         'Create new database instance for recovery process.',
         '15 minutes'::INTERVAL),
        
        (5, 'Restore Database', 'AUTOMATED',
         format('Restore from backup ID %s to recovery point %s', 
                v_checkpoint.backup_reference_id, v_checkpoint.checkpoint_time),
         '30 minutes'::INTERVAL),
        
        (6, 'Apply WAL Files', 'AUTOMATED',
         format('Apply WAL files from LSN %s to target time %s', 
                v_checkpoint.lsn_position, v_target_time),
         '20 minutes'::INTERVAL),
        
        (7, 'Verify Data Consistency', 'AUTOMATED',
         'Run data consistency checks using backup_management.verify_data_consistency()',
         '10 minutes'::INTERVAL),
        
        (8, 'Test Critical Functions', 'AUTOMATED',
         'Test sequence generation, waiting list calculations, and core business logic.',
         '15 minutes'::INTERVAL),
        
        (9, 'Update Connection Strings', 'MANUAL',
         'Update application configuration to point to recovered database.',
         '10 minutes'::INTERVAL),
        
        (10, 'Restart Application Services', 'MANUAL',
         'Start application services and verify functionality.',
         '15 minutes'::INTERVAL),
        
        (11, 'Validate User Access', 'MANUAL',
         'Test user login and core application functions.',
         '20 minutes'::INTERVAL),
        
        (12, 'Monitor System Performance', 'ONGOING',
         'Monitor system performance and data integrity for 24-48 hours.',
         '2 days'::INTERVAL);
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- BACKUP AUTOMATION SCRIPTS
-- =============================================================================

-- Shell script template for automated backups
CREATE OR REPLACE FUNCTION backup_management.generate_backup_script()
RETURNS TEXT AS $$
BEGIN
    RETURN '#!/bin/bash
# KGV Database Backup Script
# Generated: ' || NOW()::TEXT || '

set -e

# Configuration
DB_NAME="' || current_database() || '"
BACKUP_DIR="/var/backups/postgresql/kgv"
RETENTION_DAYS=30
LOG_FILE="/var/log/postgresql/kgv_backup.log"

# Ensure backup directory exists
mkdir -p "$BACKUP_DIR"

# Function to log messages
log_message() {
    echo "$(date): $1" | tee -a "$LOG_FILE"
}

# Full backup function
perform_full_backup() {
    local backup_id=$(psql -d "$DB_NAME" -t -c "SELECT backup_management.create_full_backup('"'"'$BACKUP_DIR'"'"')")
    backup_id=$(echo $backup_id | xargs) # trim whitespace
    
    log_message "Starting full backup with ID: $backup_id"
    
    local backup_file="$BACKUP_DIR/kgv_full_backup_${DB_NAME}_$(date +%Y-%m-%d_%H-%M-%S).sql.gz"
    
    if pg_dump -Fc -Z6 -f "$backup_file" "$DB_NAME"; then
        local size=$(stat -c%s "$backup_file")
        psql -d "$DB_NAME" -c "SELECT backup_management.complete_backup($backup_id, '"'"'SUCCESS'"'"', $size)"
        log_message "Full backup completed successfully: $backup_file ($(numfmt --to=iec $size))"
    else
        psql -d "$DB_NAME" -c "SELECT backup_management.complete_backup($backup_id, '"'"'FAILED'"'"', NULL, '"'"'pg_dump failed'"'"')"
        log_message "Full backup failed"
        exit 1
    fi
}

# Critical data backup function
perform_critical_backup() {
    local backup_id=$(psql -d "$DB_NAME" -t -c "SELECT backup_management.create_critical_data_backup('"'"'$BACKUP_DIR'"'"')")
    backup_id=$(echo $backup_id | xargs)
    
    log_message "Starting critical data backup with ID: $backup_id"
    
    local backup_file="$BACKUP_DIR/kgv_critical_backup_${DB_NAME}_$(date +%Y-%m-%d_%H-%M-%S).sql"
    
    if pg_dump -d "$DB_NAME" --data-only -t districts -t cadastral_districts -t applications -t application_history -t users -t file_references -t entry_numbers -t number_sequences -f "$backup_file"; then
        local size=$(stat -c%s "$backup_file")
        psql -d "$DB_NAME" -c "SELECT backup_management.complete_backup($backup_id, '"'"'SUCCESS'"'"', $size)"
        log_message "Critical backup completed successfully: $backup_file ($(numfmt --to=iec $size))"
    else
        psql -d "$DB_NAME" -c "SELECT backup_management.complete_backup($backup_id, '"'"'FAILED'"'"', NULL, '"'"'pg_dump failed'"'"')"
        log_message "Critical backup failed"
        exit 1
    fi
}

# Cleanup expired backups
cleanup_backups() {
    log_message "Starting backup cleanup"
    psql -d "$DB_NAME" -c "SELECT backup_management.cleanup_expired_backups()" > /tmp/cleanup_list.txt
    
    # Note: Add actual file deletion logic here based on cleanup_list.txt
    log_message "Backup cleanup completed"
}

# Main execution
case "${1:-full}" in
    "full")
        perform_full_backup
        ;;
    "critical")
        perform_critical_backup
        ;;
    "cleanup")
        cleanup_backups
        ;;
    *)
        echo "Usage: $0 {full|critical|cleanup}"
        exit 1
        ;;
esac
';
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- COMMENTS
-- =============================================================================

COMMENT ON SCHEMA backup_management IS 'Backup and disaster recovery management system';
COMMENT ON TABLE backup_management.backup_log IS 'Tracks all backup operations and their status';
COMMENT ON TABLE backup_management.recovery_checkpoints IS 'Stores recovery checkpoints for point-in-time recovery';
COMMENT ON FUNCTION backup_management.create_full_backup(TEXT, INTEGER, INTEGER) IS 'Initiates a full database backup';
COMMENT ON FUNCTION backup_management.complete_backup(BIGINT, VARCHAR, BIGINT, TEXT) IS 'Logs completion of a backup operation';
COMMENT ON FUNCTION backup_management.verify_data_consistency() IS 'Verifies data consistency after recovery operations';
COMMENT ON FUNCTION backup_management.initiate_disaster_recovery(TEXT, TIMESTAMP WITH TIME ZONE) IS 'Provides step-by-step disaster recovery procedure';
COMMENT ON FUNCTION backup_management.generate_backup_script() IS 'Generates shell script for automated backups';