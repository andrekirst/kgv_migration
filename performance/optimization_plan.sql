-- =============================================================================
-- KGV Migration: Performance Optimization Plan
-- Version: 1.0
-- Description: Comprehensive performance optimization strategies
-- =============================================================================

-- =============================================================================
-- ADVANCED INDEXING STRATEGY
-- =============================================================================

-- Performance-critical composite indexes for common query patterns
-- These complement the basic indexes already created in the core schema

-- Applications - Waiting list queries (performance critical)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_waiting_list_32_active 
    ON applications(waiting_list_number_32, application_date) 
    WHERE is_active = true AND waiting_list_number_32 IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_waiting_list_33_active 
    ON applications(waiting_list_number_33, application_date) 
    WHERE is_active = true AND waiting_list_number_33 IS NOT NULL;

-- Applications - Date range queries for reporting
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_date_range 
    ON applications(application_date, confirmation_date, is_active) 
    WHERE application_date IS NOT NULL;

-- Applications - Location-based searches
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_location 
    ON applications(postal_code, city) 
    WHERE is_active = true AND postal_code IS NOT NULL;

-- Applications - Contact information searches
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_contact_search 
    ON applications USING gin (
        (COALESCE(phone, '') || ' ' || COALESCE(mobile_phone, '') || ' ' || COALESCE(email, '')) gin_trgm_ops
    ) WHERE is_active = true;

-- Application history - Efficient audit trail queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_application_history_audit 
    ON application_history(application_id, action_date DESC, action_type);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_application_history_recent 
    ON application_history(action_date DESC) 
    WHERE action_date >= NOW() - INTERVAL '1 year';

-- File references and entry numbers - Year-based partitioning support
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_file_references_year_district 
    ON file_references(year DESC, district_code, number);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_entry_numbers_year_district 
    ON entry_numbers(year DESC, district_code, number);

-- Users - Permission-based queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_users_permissions 
    ON users(is_active, is_admin, can_administrate) 
    WHERE is_active = true;

-- =============================================================================
-- PARTITIONING STRATEGY
-- =============================================================================

-- Application history partitioning by year (for large datasets)
-- This will be beneficial when history grows beyond 1M records

-- Create partitioned table for application history (new installations)
/*
CREATE TABLE application_history_partitioned (
    LIKE application_history INCLUDING ALL
) PARTITION BY RANGE (action_date);

-- Create partitions for current and future years
CREATE TABLE application_history_y2024 PARTITION OF application_history_partitioned
    FOR VALUES FROM ('2024-01-01') TO ('2025-01-01');

CREATE TABLE application_history_y2025 PARTITION OF application_history_partitioned
    FOR VALUES FROM ('2025-01-01') TO ('2026-01-01');

-- Function to automatically create future partitions
CREATE OR REPLACE FUNCTION create_application_history_partition(year_value INTEGER)
RETURNS BOOLEAN AS $$
DECLARE
    partition_name TEXT;
    start_date DATE;
    end_date DATE;
BEGIN
    partition_name := 'application_history_y' || year_value;
    start_date := (year_value || '-01-01')::DATE;
    end_date := ((year_value + 1) || '-01-01')::DATE;
    
    EXECUTE format('CREATE TABLE IF NOT EXISTS %I PARTITION OF application_history_partitioned
        FOR VALUES FROM (%L) TO (%L)', 
        partition_name, start_date, end_date);
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;
*/

-- =============================================================================
-- QUERY OPTIMIZATION VIEWS
-- =============================================================================

-- Materialized view for waiting list rankings (updated nightly)
CREATE MATERIALIZED VIEW IF NOT EXISTS waiting_list_rankings AS
WITH waiting_list_32 AS (
    SELECT 
        id,
        first_name,
        last_name,
        application_date,
        waiting_list_number_32,
        ROW_NUMBER() OVER (ORDER BY application_date ASC) as calculated_rank_32
    FROM applications 
    WHERE is_active = true 
      AND waiting_list_number_32 IS NOT NULL
      AND application_date IS NOT NULL
),
waiting_list_33 AS (
    SELECT 
        id,
        first_name,
        last_name,
        application_date,
        waiting_list_number_33,
        ROW_NUMBER() OVER (ORDER BY application_date ASC) as calculated_rank_33
    FROM applications 
    WHERE is_active = true 
      AND waiting_list_number_33 IS NOT NULL
      AND application_date IS NOT NULL
)
SELECT 
    COALESCE(w32.id, w33.id) as application_id,
    COALESCE(w32.first_name, w33.first_name) as first_name,
    COALESCE(w32.last_name, w33.last_name) as last_name,
    COALESCE(w32.application_date, w33.application_date) as application_date,
    w32.waiting_list_number_32,
    w32.calculated_rank_32,
    w33.waiting_list_number_33,
    w33.calculated_rank_33
FROM waiting_list_32 w32
FULL OUTER JOIN waiting_list_33 w33 ON w32.id = w33.id;

-- Unique index on materialized view
CREATE UNIQUE INDEX IF NOT EXISTS idx_waiting_list_rankings_app_id 
    ON waiting_list_rankings(application_id);

-- Index for ranking queries
CREATE INDEX IF NOT EXISTS idx_waiting_list_rankings_32 
    ON waiting_list_rankings(calculated_rank_32) 
    WHERE calculated_rank_32 IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_waiting_list_rankings_33 
    ON waiting_list_rankings(calculated_rank_33) 
    WHERE calculated_rank_33 IS NOT NULL;

-- Materialized view for application statistics (updated hourly)
CREATE MATERIALIZED VIEW IF NOT EXISTS application_statistics AS
SELECT 
    DATE_TRUNC('month', application_date) as month,
    COUNT(*) as total_applications,
    COUNT(*) FILTER (WHERE is_active = true) as active_applications,
    COUNT(*) FILTER (WHERE confirmation_date IS NOT NULL) as confirmed_applications,
    COUNT(*) FILTER (WHERE deletion_date IS NOT NULL) as deleted_applications,
    COUNT(*) FILTER (WHERE waiting_list_number_32 IS NOT NULL) as waiting_list_32_count,
    COUNT(*) FILTER (WHERE waiting_list_number_33 IS NOT NULL) as waiting_list_33_count,
    AVG(EXTRACT(DAYS FROM (confirmation_date - application_date))) FILTER (WHERE confirmation_date IS NOT NULL) as avg_processing_days
FROM applications
WHERE application_date IS NOT NULL
GROUP BY DATE_TRUNC('month', application_date)
ORDER BY month DESC;

-- Index for statistics queries
CREATE INDEX IF NOT EXISTS idx_application_statistics_month 
    ON application_statistics(month DESC);

-- =============================================================================
-- PERFORMANCE MONITORING FUNCTIONS
-- =============================================================================

-- Function to refresh materialized views
CREATE OR REPLACE FUNCTION refresh_performance_views()
RETURNS TABLE(view_name TEXT, refresh_time INTERVAL, rows_affected BIGINT) AS $$
DECLARE
    start_time TIMESTAMP;
    end_time TIMESTAMP;
    rows_count BIGINT;
BEGIN
    -- Refresh waiting list rankings
    start_time := clock_timestamp();
    REFRESH MATERIALIZED VIEW CONCURRENTLY waiting_list_rankings;
    end_time := clock_timestamp();
    GET DIAGNOSTICS rows_count = ROW_COUNT;
    
    RETURN QUERY SELECT 'waiting_list_rankings'::TEXT, end_time - start_time, rows_count;
    
    -- Refresh application statistics
    start_time := clock_timestamp();
    REFRESH MATERIALIZED VIEW CONCURRENTLY application_statistics;
    end_time := clock_timestamp();
    GET DIAGNOSTICS rows_count = ROW_COUNT;
    
    RETURN QUERY SELECT 'application_statistics'::TEXT, end_time - start_time, rows_count;
END;
$$ LANGUAGE plpgsql;

-- Function to analyze query performance
CREATE OR REPLACE FUNCTION analyze_query_performance()
RETURNS TABLE(
    table_name TEXT,
    total_size TEXT,
    index_usage_ratio NUMERIC,
    seq_scan_ratio NUMERIC,
    cache_hit_ratio NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        schemaname || '.' || tablename as table_name,
        pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as total_size,
        CASE 
            WHEN seq_scan + idx_scan = 0 THEN 0
            ELSE ROUND(100.0 * idx_scan / (seq_scan + idx_scan), 2)
        END as index_usage_ratio,
        CASE 
            WHEN seq_scan + idx_scan = 0 THEN 0
            ELSE ROUND(100.0 * seq_scan / (seq_scan + idx_scan), 2)
        END as seq_scan_ratio,
        CASE 
            WHEN heap_blks_read + heap_blks_hit = 0 THEN 0
            ELSE ROUND(100.0 * heap_blks_hit / (heap_blks_read + heap_blks_hit), 2)
        END as cache_hit_ratio
    FROM pg_stat_user_tables 
    WHERE schemaname = 'public'
    ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- CONNECTION POOLING AND CACHING CONFIGURATION
-- =============================================================================

-- Optimized PostgreSQL configuration recommendations
/*
-- postgresql.conf settings for KGV workload:

-- Memory settings
shared_buffers = '256MB'              # 25% of RAM for dedicated server
effective_cache_size = '1GB'         # Estimate of OS cache size
work_mem = '4MB'                      # Memory for sorts/hashes per connection
maintenance_work_mem = '64MB'         # Memory for maintenance operations

-- Query planning
random_page_cost = 1.1                # SSD storage assumption
effective_io_concurrency = 200        # Number of concurrent I/O operations

-- Write-ahead logging
wal_buffers = '16MB'                  # WAL buffer size
checkpoint_completion_target = 0.9    # Spread checkpoints over time
wal_writer_delay = '200ms'            # WAL writer sleep time

-- Connection and statement timeout
statement_timeout = '30s'             # Prevent runaway queries
idle_in_transaction_session_timeout = '10min'

-- Logging for performance monitoring
log_min_duration_statement = 1000     # Log queries taking > 1 second
log_line_prefix = '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h '
log_checkpoints = on
log_connections = on
log_disconnections = on
log_lock_waits = on

-- Statistics collection
track_activities = on
track_counts = on
track_io_timing = on
track_functions = 'all'
*/

-- =============================================================================
-- BATCH PROCESSING OPTIMIZATION
-- =============================================================================

-- Function for efficient bulk operations
CREATE OR REPLACE FUNCTION bulk_update_waiting_list_positions()
RETURNS INTEGER AS $$
DECLARE
    updated_count INTEGER := 0;
BEGIN
    -- Use CTE for efficient bulk updates
    WITH ranked_applications AS (
        SELECT 
            id,
            ROW_NUMBER() OVER (ORDER BY application_date ASC) as new_rank_32
        FROM applications 
        WHERE is_active = true 
          AND waiting_list_number_32 IS NOT NULL
          AND application_date IS NOT NULL
    )
    UPDATE applications 
    SET waiting_list_number_32 = ra.new_rank_32::VARCHAR(20),
        updated_at = NOW()
    FROM ranked_applications ra
    WHERE applications.id = ra.id
      AND applications.waiting_list_number_32::INTEGER != ra.new_rank_32;
    
    GET DIAGNOSTICS updated_count = ROW_COUNT;
    
    -- Update list 33 separately
    WITH ranked_applications AS (
        SELECT 
            id,
            ROW_NUMBER() OVER (ORDER BY application_date ASC) as new_rank_33
        FROM applications 
        WHERE is_active = true 
          AND waiting_list_number_33 IS NOT NULL
          AND application_date IS NOT NULL
    )
    UPDATE applications 
    SET waiting_list_number_33 = ra.new_rank_33::VARCHAR(20),
        updated_at = NOW()
    FROM ranked_applications ra
    WHERE applications.id = ra.id
      AND applications.waiting_list_number_33::INTEGER != ra.new_rank_33;
    
    GET DIAGNOSTICS updated_count = updated_count + ROW_COUNT;
    
    RETURN updated_count;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- MAINTENANCE PROCEDURES
-- =============================================================================

-- Function to perform routine maintenance
CREATE OR REPLACE FUNCTION perform_routine_maintenance()
RETURNS TABLE(operation TEXT, duration INTERVAL, details TEXT) AS $$
DECLARE
    start_time TIMESTAMP;
    end_time TIMESTAMP;
BEGIN
    -- Update table statistics
    start_time := clock_timestamp();
    ANALYZE;
    end_time := clock_timestamp();
    RETURN QUERY SELECT 'ANALYZE'::TEXT, end_time - start_time, 'Updated table statistics'::TEXT;
    
    -- Refresh materialized views
    start_time := clock_timestamp();
    PERFORM refresh_performance_views();
    end_time := clock_timestamp();
    RETURN QUERY SELECT 'REFRESH_VIEWS'::TEXT, end_time - start_time, 'Refreshed materialized views'::TEXT;
    
    -- Vacuum tables with high update frequency
    start_time := clock_timestamp();
    VACUUM (ANALYZE, VERBOSE) applications;
    end_time := clock_timestamp();
    RETURN QUERY SELECT 'VACUUM_APPLICATIONS'::TEXT, end_time - start_time, 'Vacuumed applications table'::TEXT;
    
    start_time := clock_timestamp();
    VACUUM (ANALYZE, VERBOSE) application_history;
    end_time := clock_timestamp();
    RETURN QUERY SELECT 'VACUUM_HISTORY'::TEXT, end_time - start_time, 'Vacuumed application history table'::TEXT;
    
    -- Reindex if necessary (based on index bloat)
    start_time := clock_timestamp();
    REINDEX INDEX CONCURRENTLY idx_applications_name_search;
    end_time := clock_timestamp();
    RETURN QUERY SELECT 'REINDEX_NAME_SEARCH'::TEXT, end_time - start_time, 'Reindexed name search index'::TEXT;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- PERFORMANCE TESTING QUERIES
-- =============================================================================

-- Create a view for common performance test queries
CREATE OR REPLACE VIEW performance_test_queries AS
SELECT 
    'waiting_list_32_top_100' as query_name,
    'SELECT * FROM applications WHERE is_active = true AND waiting_list_number_32 IS NOT NULL ORDER BY waiting_list_number_32::INTEGER LIMIT 100' as query_sql
UNION ALL SELECT 
    'applications_by_date_range',
    'SELECT COUNT(*) FROM applications WHERE application_date BETWEEN ''2024-01-01'' AND ''2024-12-31'''
UNION ALL SELECT 
    'name_search_partial',
    'SELECT id, first_name, last_name FROM applications WHERE (first_name || '' '' || last_name) ILIKE ''%schmidt%'' LIMIT 50'
UNION ALL SELECT 
    'application_history_recent',
    'SELECT ah.*, a.first_name, a.last_name FROM application_history ah JOIN applications a ON ah.application_id = a.id WHERE ah.action_date >= NOW() - INTERVAL ''30 days'' ORDER BY ah.action_date DESC LIMIT 100'
UNION ALL SELECT 
    'postal_code_aggregation',
    'SELECT postal_code, COUNT(*) as application_count FROM applications WHERE is_active = true AND postal_code IS NOT NULL GROUP BY postal_code ORDER BY application_count DESC'
UNION ALL SELECT 
    'user_permissions_check',
    'SELECT COUNT(*) FROM users WHERE is_active = true AND (is_admin = true OR can_administrate = true)';

-- =============================================================================
-- SCHEDULED MAINTENANCE TASKS
-- =============================================================================

-- Create a simple task scheduler table for maintenance
CREATE TABLE IF NOT EXISTS maintenance_schedule (
    id BIGSERIAL PRIMARY KEY,
    task_name VARCHAR(100) NOT NULL UNIQUE,
    schedule_expression VARCHAR(50) NOT NULL, -- Cron-like expression
    sql_command TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    last_run TIMESTAMP WITH TIME ZONE,
    next_run TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Insert standard maintenance tasks
INSERT INTO maintenance_schedule (task_name, schedule_expression, sql_command) VALUES
('refresh_waiting_list_rankings', '0 2 * * *', 'SELECT refresh_performance_views()'),
('update_waiting_list_positions', '0 3 * * 0', 'SELECT bulk_update_waiting_list_positions()'),
('routine_maintenance', '0 1 * * 0', 'SELECT perform_routine_maintenance()'),
('analyze_performance', '0 4 * * 1', 'SELECT analyze_query_performance()')
ON CONFLICT (task_name) DO UPDATE SET
    schedule_expression = EXCLUDED.schedule_expression,
    sql_command = EXCLUDED.sql_command;

-- =============================================================================
-- COMMENTS
-- =============================================================================

COMMENT ON MATERIALIZED VIEW waiting_list_rankings IS 'Pre-calculated waiting list rankings for performance';
COMMENT ON MATERIALIZED VIEW application_statistics IS 'Monthly application statistics for reporting';
COMMENT ON FUNCTION refresh_performance_views() IS 'Refreshes all materialized views used for performance';
COMMENT ON FUNCTION analyze_query_performance() IS 'Analyzes query performance metrics for all tables';
COMMENT ON FUNCTION bulk_update_waiting_list_positions() IS 'Efficiently updates waiting list positions in bulk';
COMMENT ON FUNCTION perform_routine_maintenance() IS 'Performs routine database maintenance tasks';
COMMENT ON VIEW performance_test_queries IS 'Standard queries for performance testing';
COMMENT ON TABLE maintenance_schedule IS 'Schedule for automated maintenance tasks';