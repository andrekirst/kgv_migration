-- =============================================================================
-- KGV Migration: PostgreSQL Performance Optimizations
-- Version: 1.0
-- Description: Advanced performance optimizations for PostgreSQL schema
-- =============================================================================

-- Enable extensions for performance
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";
CREATE EXTENSION IF NOT EXISTS "pg_buffercache";
CREATE EXTENSION IF NOT EXISTS "postgis" CASCADE;
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
CREATE EXTENSION IF NOT EXISTS "btree_gin";
CREATE EXTENSION IF NOT EXISTS "btree_gist";

-- =============================================================================
-- PARTITIONING STRATEGIES
-- =============================================================================

-- Partition applications by creation year for better performance
-- Drop existing applications table (backup data first in production!)
-- ALTER TABLE applications RENAME TO applications_backup;

-- Create partitioned applications table
CREATE TABLE applications_partitioned (
    LIKE applications INCLUDING ALL
) PARTITION BY RANGE (EXTRACT(YEAR FROM created_at));

-- Create yearly partitions for applications (2020-2030)
CREATE TABLE applications_y2020 PARTITION OF applications_partitioned
    FOR VALUES FROM (2020) TO (2021);

CREATE TABLE applications_y2021 PARTITION OF applications_partitioned
    FOR VALUES FROM (2021) TO (2022);

CREATE TABLE applications_y2022 PARTITION OF applications_partitioned
    FOR VALUES FROM (2022) TO (2023);

CREATE TABLE applications_y2023 PARTITION OF applications_partitioned
    FOR VALUES FROM (2023) TO (2024);

CREATE TABLE applications_y2024 PARTITION OF applications_partitioned
    FOR VALUES FROM (2024) TO (2025);

CREATE TABLE applications_y2025 PARTITION OF applications_partitioned
    FOR VALUES FROM (2025) TO (2026);

CREATE TABLE applications_y2026 PARTITION OF applications_partitioned
    FOR VALUES FROM (2026) TO (2027);

CREATE TABLE applications_y2027 PARTITION OF applications_partitioned
    FOR VALUES FROM (2027) TO (2028);

CREATE TABLE applications_y2028 PARTITION OF applications_partitioned
    FOR VALUES FROM (2028) TO (2029);

CREATE TABLE applications_y2029 PARTITION OF applications_partitioned
    FOR VALUES FROM (2029) TO (2030);

CREATE TABLE applications_y2030 PARTITION OF applications_partitioned
    FOR VALUES FROM (2030) TO (2031);

-- Default partition for any dates outside the range
CREATE TABLE applications_default PARTITION OF applications_partitioned DEFAULT;

-- Partition application_history by action_date for audit trail performance
CREATE TABLE application_history_partitioned (
    LIKE application_history INCLUDING ALL
) PARTITION BY RANGE (EXTRACT(YEAR FROM action_date));

-- Create yearly partitions for history (2020-2030)
CREATE TABLE application_history_y2020 PARTITION OF application_history_partitioned
    FOR VALUES FROM (2020) TO (2021);

CREATE TABLE application_history_y2021 PARTITION OF application_history_partitioned
    FOR VALUES FROM (2021) TO (2022);

CREATE TABLE application_history_y2022 PARTITION OF application_history_partitioned
    FOR VALUES FROM (2022) TO (2023);

CREATE TABLE application_history_y2023 PARTITION OF application_history_partitioned
    FOR VALUES FROM (2023) TO (2024);

CREATE TABLE application_history_y2024 PARTITION OF application_history_partitioned
    FOR VALUES FROM (2024) TO (2025);

CREATE TABLE application_history_y2025 PARTITION OF application_history_partitioned
    FOR VALUES FROM (2025) TO (2026);

CREATE TABLE application_history_y2026 PARTITION OF application_history_partitioned
    FOR VALUES FROM (2026) TO (2027);

CREATE TABLE application_history_y2027 PARTITION OF application_history_partitioned
    FOR VALUES FROM (2027) TO (2028);

CREATE TABLE application_history_y2028 PARTITION OF application_history_partitioned
    FOR VALUES FROM (2028) TO (2029);

CREATE TABLE application_history_y2029 PARTITION OF application_history_partitioned
    FOR VALUES FROM (2029) TO (2030);

CREATE TABLE application_history_y2030 PARTITION OF application_history_partitioned
    FOR VALUES FROM (2030) TO (2031);

-- Default partition for history
CREATE TABLE application_history_default PARTITION OF application_history_partitioned DEFAULT;

-- =============================================================================
-- ADVANCED INDEXING STRATEGIES
-- =============================================================================

-- Composite indexes for common query patterns
CREATE INDEX CONCURRENTLY idx_applications_status_date 
    ON applications(is_active, application_date) 
    WHERE is_active = true;

CREATE INDEX CONCURRENTLY idx_applications_location_search 
    ON applications(postal_code, city) 
    WHERE is_active = true;

CREATE INDEX CONCURRENTLY idx_applications_waiting_list_combined 
    ON applications(waiting_list_number_32, waiting_list_number_33, application_date);

-- Full-text search indexes
CREATE INDEX CONCURRENTLY idx_applications_fulltext_search 
    ON applications USING gin(
        to_tsvector('german', 
            COALESCE(first_name, '') || ' ' || 
            COALESCE(last_name, '') || ' ' || 
            COALESCE(street, '') || ' ' || 
            COALESCE(city, '')
        )
    );

-- Partial indexes for active records only
CREATE INDEX CONCURRENTLY idx_applications_active_email 
    ON applications(email) 
    WHERE is_active = true AND email IS NOT NULL;

CREATE INDEX CONCURRENTLY idx_applications_active_phone 
    ON applications(phone) 
    WHERE is_active = true AND phone IS NOT NULL;

-- Indexes for audit trail queries
CREATE INDEX CONCURRENTLY idx_application_history_timeline 
    ON application_history(application_id, action_date DESC);

CREATE INDEX CONCURRENTLY idx_application_history_user_activity 
    ON application_history(user_id, action_date DESC) 
    WHERE user_id IS NOT NULL;

-- Covering indexes for frequently accessed columns
CREATE INDEX CONCURRENTLY idx_applications_list_view 
    ON applications(application_date DESC) 
    INCLUDE (first_name, last_name, postal_code, city, is_active);

CREATE INDEX CONCURRENTLY idx_users_active_list 
    ON users(last_name, first_name) 
    INCLUDE (email, phone, job_title, is_active) 
    WHERE is_active = true;

-- BRIN indexes for large sequential data
CREATE INDEX CONCURRENTLY idx_application_history_brin_date 
    ON application_history USING brin(action_date);

-- Indexes for foreign key relationships
CREATE INDEX CONCURRENTLY idx_cadastral_districts_district_lookup 
    ON cadastral_districts(district_id) 
    INCLUDE (code, name);

-- =============================================================================
-- MATERIALIZED VIEWS FOR REPORTING
-- =============================================================================

-- Application statistics view
CREATE MATERIALIZED VIEW application_statistics AS
SELECT 
    DATE_TRUNC('month', application_date) AS month,
    COUNT(*) AS total_applications,
    COUNT(*) FILTER (WHERE is_active = true) AS active_applications,
    COUNT(*) FILTER (WHERE confirmation_date IS NOT NULL) AS confirmed_applications,
    COUNT(*) FILTER (WHERE current_offer_date IS NOT NULL) AS applications_with_offers,
    AVG(EXTRACT(DAYS FROM (confirmation_date - application_date))) AS avg_processing_days
FROM applications 
WHERE application_date IS NOT NULL
GROUP BY DATE_TRUNC('month', application_date)
ORDER BY month DESC;

-- Create unique index on materialized view
CREATE UNIQUE INDEX idx_application_statistics_month 
    ON application_statistics(month);

-- Waiting list statistics view
CREATE MATERIALIZED VIEW waiting_list_statistics AS
SELECT 
    '32' AS area,
    COUNT(*) AS total_applications,
    MIN(application_date) AS oldest_application,
    MAX(application_date) AS newest_application,
    AVG(EXTRACT(DAYS FROM (CURRENT_DATE - application_date))) AS avg_waiting_days
FROM applications 
WHERE waiting_list_number_32 IS NOT NULL AND is_active = true

UNION ALL

SELECT 
    '33' AS area,
    COUNT(*) AS total_applications,
    MIN(application_date) AS oldest_application,
    MAX(application_date) AS newest_application,
    AVG(EXTRACT(DAYS FROM (CURRENT_DATE - application_date))) AS avg_waiting_days
FROM applications 
WHERE waiting_list_number_33 IS NOT NULL AND is_active = true;

-- User activity statistics view
CREATE MATERIALIZED VIEW user_activity_statistics AS
SELECT 
    u.id,
    u.first_name,
    u.last_name,
    u.job_title,
    COUNT(ah.id) AS total_actions,
    COUNT(ah.id) FILTER (WHERE ah.action_date >= CURRENT_DATE - INTERVAL '30 days') AS actions_last_30_days,
    MAX(ah.action_date) AS last_activity_date,
    COUNT(DISTINCT ah.application_id) AS applications_handled
FROM users u
LEFT JOIN application_history ah ON u.id = ah.user_id
WHERE u.is_active = true
GROUP BY u.id, u.first_name, u.last_name, u.job_title
ORDER BY total_actions DESC;

-- =============================================================================
-- STORED PROCEDURES FOR MAINTENANCE
-- =============================================================================

-- Procedure to refresh materialized views
CREATE OR REPLACE FUNCTION refresh_materialized_views()
RETURNS void AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY application_statistics;
    REFRESH MATERIALIZED VIEW waiting_list_statistics;
    REFRESH MATERIALIZED VIEW user_activity_statistics;
    
    -- Log the refresh
    INSERT INTO system_logs (level, message, created_at)
    VALUES ('INFO', 'Materialized views refreshed', NOW());
END;
$$ LANGUAGE plpgsql;

-- Procedure to analyze table statistics
CREATE OR REPLACE FUNCTION update_table_statistics()
RETURNS void AS $$
BEGIN
    ANALYZE applications;
    ANALYZE application_history;
    ANALYZE users;
    ANALYZE districts;
    ANALYZE cadastral_districts;
    ANALYZE file_references;
    ANALYZE entry_numbers;
    ANALYZE identifiers;
    ANALYZE field_mappings;
    
    -- Log the analysis
    INSERT INTO system_logs (level, message, created_at)
    VALUES ('INFO', 'Table statistics updated', NOW());
END;
$$ LANGUAGE plpgsql;

-- Procedure to create new yearly partitions
CREATE OR REPLACE FUNCTION create_yearly_partitions(target_year INTEGER)
RETURNS void AS $$
DECLARE
    partition_name TEXT;
    start_year INTEGER;
    end_year INTEGER;
BEGIN
    start_year := target_year;
    end_year := target_year + 1;
    
    -- Create application partition
    partition_name := 'applications_y' || target_year::TEXT;
    EXECUTE format('CREATE TABLE %I PARTITION OF applications_partitioned FOR VALUES FROM (%L) TO (%L)',
                   partition_name, start_year, end_year);
    
    -- Create application history partition
    partition_name := 'application_history_y' || target_year::TEXT;
    EXECUTE format('CREATE TABLE %I PARTITION OF application_history_partitioned FOR VALUES FROM (%L) TO (%L)',
                   partition_name, start_year, end_year);
    
    -- Log the creation
    INSERT INTO system_logs (level, message, created_at)
    VALUES ('INFO', 'Created yearly partitions for year ' || target_year::TEXT, NOW());
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- PERFORMANCE MONITORING VIEWS
-- =============================================================================

-- View for monitoring query performance
CREATE VIEW query_performance AS
SELECT 
    query,
    calls,
    total_time,
    mean_time,
    min_time,
    max_time,
    stddev_time,
    rows,
    100.0 * shared_blks_hit / nullif(shared_blks_hit + shared_blks_read, 0) AS hit_percent
FROM pg_stat_statements
ORDER BY total_time DESC;

-- View for monitoring index usage
CREATE VIEW index_usage AS
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_tup_read,
    idx_tup_fetch,
    idx_scan,
    CASE 
        WHEN idx_scan = 0 THEN 'Unused'
        WHEN idx_scan < 100 THEN 'Low Usage'
        WHEN idx_scan < 1000 THEN 'Medium Usage'
        ELSE 'High Usage'
    END AS usage_category
FROM pg_stat_user_indexes
ORDER BY idx_scan DESC;

-- View for monitoring table statistics
CREATE VIEW table_statistics AS
SELECT 
    schemaname,
    tablename,
    n_tup_ins,
    n_tup_upd,
    n_tup_del,
    n_live_tup,
    n_dead_tup,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze
FROM pg_stat_user_tables
ORDER BY n_live_tup DESC;

-- =============================================================================
-- AUTOMATIC MAINTENANCE JOBS
-- =============================================================================

-- Create system logs table for maintenance logging
CREATE TABLE IF NOT EXISTS system_logs (
    id BIGSERIAL PRIMARY KEY,
    level VARCHAR(10) NOT NULL DEFAULT 'INFO',
    message TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create index on system logs
CREATE INDEX IF NOT EXISTS idx_system_logs_created_at 
    ON system_logs(created_at DESC);

-- Function to clean old system logs
CREATE OR REPLACE FUNCTION cleanup_system_logs()
RETURNS void AS $$
BEGIN
    DELETE FROM system_logs 
    WHERE created_at < NOW() - INTERVAL '90 days';
    
    INSERT INTO system_logs (level, message, created_at)
    VALUES ('INFO', 'Cleaned up old system logs', NOW());
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- CONNECTION POOLING CONFIGURATION
-- =============================================================================

-- Optimize PostgreSQL settings for better performance
-- These should be added to postgresql.conf:

/*
# Connection settings
max_connections = 200
superuser_reserved_connections = 3

# Memory settings
shared_buffers = 256MB
effective_cache_size = 1GB
work_mem = 16MB
maintenance_work_mem = 256MB

# Query planner settings
default_statistics_target = 100
random_page_cost = 1.1
effective_io_concurrency = 200

# WAL settings
wal_buffers = 16MB
checkpoint_completion_target = 0.9
wal_writer_delay = 200ms

# Logging settings
log_min_duration_statement = 1000ms
log_checkpoints = on
log_connections = on
log_disconnections = on
log_lock_waits = on

# Autovacuum settings
autovacuum = on
autovacuum_max_workers = 3
autovacuum_naptime = 20s
autovacuum_vacuum_threshold = 50
autovacuum_analyze_threshold = 50
autovacuum_vacuum_scale_factor = 0.1
autovacuum_analyze_scale_factor = 0.05
*/

-- =============================================================================
-- GEOGRAPHIC DATA SUPPORT (PostGIS)
-- =============================================================================

-- Add geographic columns to applications for location-based queries
ALTER TABLE applications 
ADD COLUMN IF NOT EXISTS location_point GEOMETRY(POINT, 4326);

-- Create spatial index
CREATE INDEX IF NOT EXISTS idx_applications_location_gist 
    ON applications USING gist(location_point);

-- Function to geocode addresses (placeholder - integrate with geocoding service)
CREATE OR REPLACE FUNCTION geocode_application_address(app_id BIGINT)
RETURNS void AS $$
DECLARE
    app_record RECORD;
    lat DECIMAL;
    lon DECIMAL;
BEGIN
    SELECT street, postal_code, city INTO app_record
    FROM applications 
    WHERE id = app_id;
    
    -- This is a placeholder - integrate with actual geocoding service
    -- For now, we'll use approximate coordinates for German postal codes
    IF app_record.postal_code IS NOT NULL THEN
        -- Simple approximation based on postal code
        lat := 51.0 + (app_record.postal_code::INTEGER % 1000) / 100.0;
        lon := 10.0 + (app_record.postal_code::INTEGER % 100) / 50.0;
        
        UPDATE applications 
        SET location_point = ST_SetSRID(ST_MakePoint(lon, lat), 4326)
        WHERE id = app_id;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- TEMPORAL TABLES FOR AUDIT TRAILS
-- =============================================================================

-- Add temporal columns to main tables for full audit trail
ALTER TABLE applications 
ADD COLUMN IF NOT EXISTS valid_from TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
ADD COLUMN IF NOT EXISTS valid_to TIMESTAMP WITH TIME ZONE DEFAULT 'infinity';

-- Create history table for applications
CREATE TABLE IF NOT EXISTS applications_history (
    LIKE applications,
    operation CHAR(1) NOT NULL,
    changed_by TEXT DEFAULT CURRENT_USER,
    changed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create trigger function for temporal table
CREATE OR REPLACE FUNCTION applications_temporal_trigger()
RETURNS trigger AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        -- Update valid_to for the deleted record
        UPDATE applications SET valid_to = NOW() WHERE id = OLD.id;
        
        -- Insert into history table
        INSERT INTO applications_history SELECT OLD.*, 'D', CURRENT_USER, NOW();
        
        -- Don't actually delete the record
        RETURN NULL;
    ELSIF TG_OP = 'UPDATE' THEN
        -- Insert old version into history
        INSERT INTO applications_history SELECT OLD.*, 'U', CURRENT_USER, NOW();
        
        -- Update valid_from for new version
        NEW.valid_from = NOW();
        
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        -- Insert into history table
        INSERT INTO applications_history SELECT NEW.*, 'I', CURRENT_USER, NOW();
        RETURN NEW;
    END IF;
    
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Create temporal trigger
DROP TRIGGER IF EXISTS applications_temporal_trigger ON applications;
CREATE TRIGGER applications_temporal_trigger
    AFTER INSERT OR UPDATE OR DELETE ON applications
    FOR EACH ROW EXECUTE FUNCTION applications_temporal_trigger();

-- =============================================================================
-- COMMENTS FOR DOCUMENTATION
-- =============================================================================

COMMENT ON MATERIALIZED VIEW application_statistics IS 'Monthly application statistics for reporting';
COMMENT ON MATERIALIZED VIEW waiting_list_statistics IS 'Waiting list statistics by area';
COMMENT ON MATERIALIZED VIEW user_activity_statistics IS 'User activity and performance metrics';

COMMENT ON FUNCTION refresh_materialized_views() IS 'Refreshes all materialized views concurrently';
COMMENT ON FUNCTION update_table_statistics() IS 'Updates table statistics for query optimizer';
COMMENT ON FUNCTION create_yearly_partitions(INTEGER) IS 'Creates yearly partitions for specified year';
COMMENT ON FUNCTION cleanup_system_logs() IS 'Removes system logs older than 90 days';

COMMENT ON VIEW query_performance IS 'Shows query performance statistics from pg_stat_statements';
COMMENT ON VIEW index_usage IS 'Shows index usage statistics and identifies unused indexes';
COMMENT ON VIEW table_statistics IS 'Shows table access and maintenance statistics';

-- Performance optimization complete
SELECT 'PostgreSQL performance optimizations applied successfully' AS status;