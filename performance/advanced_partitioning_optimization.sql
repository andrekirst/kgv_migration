-- =============================================================================
-- KGV Migration: Advanced Partitioning and Memory Optimization
-- Version: 1.0
-- Description: Production-ready partitioning strategy for high-volume operations
-- Target: Memory usage < 2GB for 1M records, improved query performance
-- =============================================================================

-- =============================================================================
-- PARTITIONING STRATEGY ANALYSIS AND IMPLEMENTATION
-- =============================================================================

-- Function to analyze current table sizes and growth patterns
CREATE OR REPLACE FUNCTION analyze_partitioning_candidates()
RETURNS TABLE(
    table_name TEXT,
    current_size_mb NUMERIC,
    row_count BIGINT,
    avg_row_size_bytes NUMERIC,
    growth_rate_rows_per_day NUMERIC,
    partition_recommendation TEXT
) AS $$
BEGIN
    RETURN QUERY
    WITH table_stats AS (
        SELECT 
            'applications'::TEXT as table_name,
            pg_total_relation_size('applications') / 1024.0 / 1024.0 as size_mb,
            COUNT(*) as row_count,
            pg_total_relation_size('applications') / GREATEST(COUNT(*), 1) as avg_row_size
        FROM applications
        
        UNION ALL
        
        SELECT 
            'application_history'::TEXT,
            pg_total_relation_size('application_history') / 1024.0 / 1024.0,
            COUNT(*),
            pg_total_relation_size('application_history') / GREATEST(COUNT(*), 1)
        FROM application_history
    ),
    growth_stats AS (
        SELECT 
            'applications'::TEXT as table_name,
            CASE 
                WHEN COUNT(DISTINCT DATE(created_at)) > 1 THEN
                    COUNT(*)::NUMERIC / COUNT(DISTINCT DATE(created_at))
                ELSE 0
            END as daily_growth
        FROM applications
        WHERE created_at >= NOW() - INTERVAL '30 days'
        
        UNION ALL
        
        SELECT 
            'application_history'::TEXT,
            CASE 
                WHEN COUNT(DISTINCT DATE(created_at)) > 1 THEN
                    COUNT(*)::NUMERIC / COUNT(DISTINCT DATE(created_at))
                ELSE 0
            END
        FROM application_history
        WHERE created_at >= NOW() - INTERVAL '30 days'
    )
    SELECT 
        ts.table_name,
        ROUND(ts.size_mb, 2),
        ts.row_count,
        ROUND(ts.avg_row_size, 2),
        ROUND(COALESCE(gs.daily_growth, 0), 2),
        CASE 
            WHEN ts.size_mb > 1000 OR ts.row_count > 100000 THEN 'HIGH_PRIORITY_PARTITION'
            WHEN ts.size_mb > 500 OR ts.row_count > 50000 THEN 'MEDIUM_PRIORITY_PARTITION'
            WHEN COALESCE(gs.daily_growth, 0) > 1000 THEN 'GROWTH_BASED_PARTITION'
            ELSE 'NO_PARTITIONING_NEEDED'
        END
    FROM table_stats ts
    LEFT JOIN growth_stats gs ON ts.table_name = gs.table_name;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- ENHANCED PARTITIONING IMPLEMENTATION
-- =============================================================================

-- Create enhanced application history partitioning (by year and quarter)
-- This provides better performance for audit queries while managing partition size

CREATE TABLE IF NOT EXISTS application_history_partitioned (
    LIKE application_history INCLUDING ALL EXCLUDING INDEXES
) PARTITION BY RANGE (action_date);

-- Create quarterly partitions for better performance balance
CREATE TABLE IF NOT EXISTS application_history_2024_q1 PARTITION OF application_history_partitioned
    FOR VALUES FROM ('2024-01-01') TO ('2024-04-01');

CREATE TABLE IF NOT EXISTS application_history_2024_q2 PARTITION OF application_history_partitioned
    FOR VALUES FROM ('2024-04-01') TO ('2024-07-01');

CREATE TABLE IF NOT EXISTS application_history_2024_q3 PARTITION OF application_history_partitioned
    FOR VALUES FROM ('2024-07-01') TO ('2024-10-01');

CREATE TABLE IF NOT EXISTS application_history_2024_q4 PARTITION OF application_history_partitioned
    FOR VALUES FROM ('2024-10-01') TO ('2025-01-01');

CREATE TABLE IF NOT EXISTS application_history_2025_q1 PARTITION OF application_history_partitioned
    FOR VALUES FROM ('2025-01-01') TO ('2025-04-01');

CREATE TABLE IF NOT EXISTS application_history_2025_q2 PARTITION OF application_history_partitioned
    FOR VALUES FROM ('2025-04-01') TO ('2025-07-01');

CREATE TABLE IF NOT EXISTS application_history_2025_q3 PARTITION OF application_history_partitioned
    FOR VALUES FROM ('2025-07-01') TO ('2025-10-01');

CREATE TABLE IF NOT EXISTS application_history_2025_q4 PARTITION OF application_history_partitioned
    FOR VALUES FROM ('2025-10-01') TO ('2026-01-01');

-- Default partition for future dates
CREATE TABLE IF NOT EXISTS application_history_default PARTITION OF application_history_partitioned DEFAULT;

-- Function to automatically create future partitions
CREATE OR REPLACE FUNCTION create_application_history_quarterly_partitions(
    p_year INTEGER,
    p_quarter INTEGER DEFAULT NULL
)
RETURNS BOOLEAN AS $$
DECLARE
    v_quarter INTEGER;
    v_partition_name TEXT;
    v_start_date DATE;
    v_end_date DATE;
BEGIN
    -- If quarter not specified, create all quarters for the year
    IF p_quarter IS NULL THEN
        FOR v_quarter IN 1..4 LOOP
            PERFORM create_application_history_quarterly_partitions(p_year, v_quarter);
        END LOOP;
        RETURN TRUE;
    END IF;
    
    -- Calculate partition boundaries
    v_partition_name := 'application_history_' || p_year || '_q' || p_quarter;
    v_start_date := (p_year || '-' || (((p_quarter - 1) * 3) + 1) || '-01')::DATE;
    v_end_date := v_start_date + INTERVAL '3 months';
    
    -- Create partition if it doesn't exist
    BEGIN
        EXECUTE format(
            'CREATE TABLE IF NOT EXISTS %I PARTITION OF application_history_partitioned
             FOR VALUES FROM (%L) TO (%L)',
            v_partition_name, v_start_date, v_end_date
        );
        
        -- Create indexes on the new partition
        EXECUTE format(
            'CREATE INDEX IF NOT EXISTS %I ON %I (application_id, action_date DESC)',
            'idx_' || v_partition_name || '_app_date', v_partition_name
        );
        
        EXECUTE format(
            'CREATE INDEX IF NOT EXISTS %I ON %I (action_date DESC) WHERE user_id IS NOT NULL',
            'idx_' || v_partition_name || '_date_user', v_partition_name
        );
        
        RETURN TRUE;
    EXCEPTION WHEN OTHERS THEN
        RAISE WARNING 'Failed to create partition %: %', v_partition_name, SQLERRM;
        RETURN FALSE;
    END;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- MEMORY OPTIMIZATION STRATEGIES
-- =============================================================================

-- Function to optimize memory usage for large result sets
CREATE OR REPLACE FUNCTION optimize_memory_settings_for_session()
RETURNS VOID AS $$
BEGIN
    -- Optimize work_mem for sorting and hashing operations
    SET LOCAL work_mem = '64MB';
    
    -- Optimize maintenance_work_mem for bulk operations
    SET LOCAL maintenance_work_mem = '256MB';
    
    -- Optimize effective_cache_size for query planning
    SET LOCAL effective_cache_size = '1GB';
    
    -- Optimize random_page_cost for SSD storage
    SET LOCAL random_page_cost = 1.1;
    
    -- Optimize shared_buffers utilization
    SET LOCAL effective_io_concurrency = 200;
END;
$$ LANGUAGE plpgsql;

-- Memory-efficient cursor-based pagination for large datasets
CREATE OR REPLACE FUNCTION get_applications_paginated(
    p_cursor_application_date DATE DEFAULT NULL,
    p_cursor_application_id BIGINT DEFAULT NULL,
    p_page_size INTEGER DEFAULT 1000,
    p_filters JSONB DEFAULT '{}'::JSONB
)
RETURNS TABLE(
    application_id BIGINT,
    first_name VARCHAR(50),
    last_name VARCHAR(50),
    application_date DATE,
    is_active BOOLEAN,
    cursor_date DATE,
    cursor_id BIGINT
) AS $$
DECLARE
    v_where_clause TEXT := '';
    v_query TEXT;
BEGIN
    -- Build dynamic WHERE clause from filters
    IF p_filters ? 'postal_code' THEN
        v_where_clause := v_where_clause || ' AND postal_code = ' || quote_literal(p_filters->>'postal_code');
    END IF;
    
    IF p_filters ? 'city' THEN
        v_where_clause := v_where_clause || ' AND city ILIKE ' || quote_literal('%' || (p_filters->>'city') || '%');
    END IF;
    
    IF p_filters ? 'waiting_list' THEN
        IF p_filters->>'waiting_list' = '32' THEN
            v_where_clause := v_where_clause || ' AND waiting_list_number_32 IS NOT NULL';
        ELSIF p_filters->>'waiting_list' = '33' THEN
            v_where_clause := v_where_clause || ' AND waiting_list_number_33 IS NOT NULL';
        END IF;
    END IF;
    
    -- Build cursor-based pagination query
    v_query := '
        SELECT 
            id,
            first_name,
            last_name,
            application_date,
            is_active,
            application_date as cursor_date,
            id as cursor_id
        FROM applications 
        WHERE is_active = true' || v_where_clause;
    
    -- Add cursor condition for pagination
    IF p_cursor_application_date IS NOT NULL AND p_cursor_application_id IS NOT NULL THEN
        v_query := v_query || '
            AND (application_date > ' || quote_literal(p_cursor_application_date) || '
                 OR (application_date = ' || quote_literal(p_cursor_application_date) || ' 
                     AND id > ' || p_cursor_application_id || '))';
    END IF;
    
    v_query := v_query || '
        ORDER BY application_date ASC, id ASC
        LIMIT ' || p_page_size;
    
    RETURN QUERY EXECUTE v_query;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- BULK OPERATIONS OPTIMIZATION
-- =============================================================================

-- Memory-efficient bulk update function
CREATE OR REPLACE FUNCTION bulk_update_applications_optimized(
    p_updates JSONB[],
    p_batch_size INTEGER DEFAULT 500
)
RETURNS TABLE(
    batch_number INTEGER,
    records_updated INTEGER,
    processing_time_ms INTEGER,
    memory_usage_mb NUMERIC
) AS $$
DECLARE
    v_batch_start INTEGER := 1;
    v_batch_end INTEGER;
    v_current_batch INTEGER := 1;
    v_start_time TIMESTAMP;
    v_end_time TIMESTAMP;
    v_updated_count INTEGER;
    v_total_updates INTEGER := array_length(p_updates, 1);
    v_memory_before NUMERIC;
    v_memory_after NUMERIC;
BEGIN
    -- Optimize session for bulk operations
    PERFORM optimize_memory_settings_for_session();
    
    WHILE v_batch_start <= v_total_updates LOOP
        v_batch_end := LEAST(v_batch_start + p_batch_size - 1, v_total_updates);
        v_start_time := clock_timestamp();
        
        -- Get memory usage before batch
        SELECT pg_size_pretty(pg_database_size(current_database()))::NUMERIC INTO v_memory_before;
        
        -- Process batch with optimized query
        WITH update_data AS (
            SELECT 
                (update_item->>'id')::BIGINT as application_id,
                update_item->>'first_name' as new_first_name,
                update_item->>'last_name' as new_last_name,
                update_item->>'email' as new_email,
                update_item->>'phone' as new_phone
            FROM UNNEST(p_updates[v_batch_start:v_batch_end]) AS update_item
        )
        UPDATE applications 
        SET 
            first_name = COALESCE(ud.new_first_name, applications.first_name),
            last_name = COALESCE(ud.new_last_name, applications.last_name),
            email = COALESCE(ud.new_email, applications.email),
            phone = COALESCE(ud.new_phone, applications.phone),
            updated_at = NOW()
        FROM update_data ud
        WHERE applications.id = ud.application_id;
        
        GET DIAGNOSTICS v_updated_count = ROW_COUNT;
        
        v_end_time := clock_timestamp();
        
        -- Get memory usage after batch
        SELECT pg_size_pretty(pg_database_size(current_database()))::NUMERIC INTO v_memory_after;
        
        RETURN QUERY SELECT 
            v_current_batch,
            v_updated_count,
            EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time))::INTEGER,
            COALESCE(v_memory_after - v_memory_before, 0);
        
        v_batch_start := v_batch_end + 1;
        v_current_batch := v_current_batch + 1;
        
        -- Force garbage collection every 10 batches
        IF v_current_batch % 10 = 0 THEN
            PERFORM pg_stat_reset();
        END IF;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- PARALLEL PROCESSING OPTIMIZATION
-- =============================================================================

-- Function to enable parallel processing for large operations
CREATE OR REPLACE FUNCTION configure_parallel_processing()
RETURNS TABLE(setting_name TEXT, current_value TEXT, recommended_value TEXT) AS $$
BEGIN
    RETURN QUERY
    SELECT 'max_parallel_workers'::TEXT, current_setting('max_parallel_workers'), '8'::TEXT
    UNION ALL
    SELECT 'max_parallel_workers_per_gather', current_setting('max_parallel_workers_per_gather'), '4'
    UNION ALL
    SELECT 'parallel_tuple_cost', current_setting('parallel_tuple_cost'), '0.1'
    UNION ALL
    SELECT 'parallel_setup_cost', current_setting('parallel_setup_cost'), '1000'
    UNION ALL
    SELECT 'min_parallel_table_scan_size', current_setting('min_parallel_table_scan_size'), '8MB'
    UNION ALL
    SELECT 'min_parallel_index_scan_size', current_setting('min_parallel_index_scan_size'), '512kB';
END;
$$ LANGUAGE plpgsql;

-- Parallel-optimized aggregation queries
CREATE OR REPLACE FUNCTION get_application_statistics_parallel()
RETURNS TABLE(
    metric_name TEXT,
    metric_value NUMERIC,
    computation_time_ms INTEGER
) AS $$
DECLARE
    v_start_time TIMESTAMP;
    v_end_time TIMESTAMP;
BEGIN
    -- Enable parallel processing for this session
    SET LOCAL max_parallel_workers_per_gather = 4;
    SET LOCAL parallel_tuple_cost = 0.1;
    
    -- Total active applications
    v_start_time := clock_timestamp();
    
    RETURN QUERY
    SELECT 
        'total_active_applications'::TEXT,
        COUNT(*)::NUMERIC,
        EXTRACT(MILLISECONDS FROM (clock_timestamp() - v_start_time))::INTEGER
    FROM applications 
    WHERE is_active = true;
    
    -- Applications by year (parallel aggregation)
    v_start_time := clock_timestamp();
    
    RETURN QUERY
    SELECT 
        'applications_by_year'::TEXT,
        COUNT(*)::NUMERIC,
        EXTRACT(MILLISECONDS FROM (clock_timestamp() - v_start_time))::INTEGER
    FROM applications 
    WHERE application_date >= '2024-01-01'
      AND is_active = true;
    
    -- Waiting list statistics (parallel processing)
    v_start_time := clock_timestamp();
    
    RETURN QUERY
    SELECT 
        'waiting_list_total'::TEXT,
        COUNT(*)::NUMERIC,
        EXTRACT(MILLISECONDS FROM (clock_timestamp() - v_start_time))::INTEGER
    FROM applications 
    WHERE is_active = true
      AND (waiting_list_number_32 IS NOT NULL OR waiting_list_number_33 IS NOT NULL);
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- VACUUM AND MAINTENANCE OPTIMIZATION
-- =============================================================================

-- Intelligent vacuum strategy based on table activity
CREATE OR REPLACE FUNCTION intelligent_vacuum_strategy()
RETURNS TABLE(
    table_name TEXT,
    vacuum_type TEXT,
    estimated_duration TEXT,
    priority INTEGER
) AS $$
BEGIN
    RETURN QUERY
    WITH table_activity AS (
        SELECT 
            schemaname || '.' || tablename as full_name,
            tablename,
            n_tup_ins + n_tup_upd + n_tup_del as total_changes,
            n_dead_tup,
            n_live_tup,
            COALESCE(last_autovacuum, last_vacuum, '1970-01-01'::TIMESTAMPTZ) as last_vacuum_time
        FROM pg_stat_user_tables
        WHERE schemaname = 'public'
    )
    SELECT 
        ta.full_name::TEXT,
        CASE 
            WHEN ta.n_dead_tup > ta.n_live_tup * 0.2 THEN 'VACUUM FULL'
            WHEN ta.n_dead_tup > ta.n_live_tup * 0.1 THEN 'VACUUM ANALYZE'
            WHEN ta.total_changes > 10000 THEN 'ANALYZE'
            ELSE 'NO_ACTION'
        END::TEXT,
        CASE 
            WHEN ta.n_dead_tup > ta.n_live_tup * 0.2 THEN '30-60 minutes'
            WHEN ta.n_dead_tup > ta.n_live_tup * 0.1 THEN '5-15 minutes'
            WHEN ta.total_changes > 10000 THEN '1-5 minutes'
            ELSE 'N/A'
        END::TEXT,
        CASE 
            WHEN ta.n_dead_tup > ta.n_live_tup * 0.2 THEN 1
            WHEN ta.n_dead_tup > ta.n_live_tup * 0.1 THEN 2
            WHEN ta.total_changes > 10000 THEN 3
            ELSE 4
        END::INTEGER
    FROM table_activity ta
    ORDER BY 
        CASE 
            WHEN ta.n_dead_tup > ta.n_live_tup * 0.2 THEN 1
            WHEN ta.n_dead_tup > ta.n_live_tup * 0.1 THEN 2
            WHEN ta.total_changes > 10000 THEN 3
            ELSE 4
        END;
END;
$$ LANGUAGE plpgsql;

-- Function to perform optimized maintenance
CREATE OR REPLACE FUNCTION perform_optimized_maintenance()
RETURNS TABLE(
    operation TEXT,
    table_name TEXT,
    duration_seconds INTEGER,
    memory_usage_change_mb NUMERIC,
    status TEXT
) AS $$
DECLARE
    v_maintenance_task RECORD;
    v_start_time TIMESTAMP;
    v_end_time TIMESTAMP;
    v_memory_before NUMERIC;
    v_memory_after NUMERIC;
BEGIN
    -- Get memory usage before maintenance
    SELECT pg_database_size(current_database()) / 1024.0 / 1024.0 INTO v_memory_before;
    
    -- Process each maintenance task
    FOR v_maintenance_task IN 
        SELECT * FROM intelligent_vacuum_strategy() 
        WHERE vacuum_type != 'NO_ACTION' 
        ORDER BY priority
    LOOP
        v_start_time := clock_timestamp();
        
        BEGIN
            CASE v_maintenance_task.vacuum_type
                WHEN 'VACUUM FULL' THEN
                    EXECUTE 'VACUUM FULL ANALYZE ' || v_maintenance_task.table_name;
                WHEN 'VACUUM ANALYZE' THEN
                    EXECUTE 'VACUUM ANALYZE ' || v_maintenance_task.table_name;
                WHEN 'ANALYZE' THEN
                    EXECUTE 'ANALYZE ' || v_maintenance_task.table_name;
            END CASE;
            
            v_end_time := clock_timestamp();
            
            -- Get memory usage after operation
            SELECT pg_database_size(current_database()) / 1024.0 / 1024.0 INTO v_memory_after;
            
            RETURN QUERY SELECT 
                v_maintenance_task.vacuum_type::TEXT,
                v_maintenance_task.table_name::TEXT,
                EXTRACT(SECONDS FROM (v_end_time - v_start_time))::INTEGER,
                (v_memory_after - v_memory_before)::NUMERIC,
                'SUCCESS'::TEXT;
                
        EXCEPTION WHEN OTHERS THEN
            v_end_time := clock_timestamp();
            
            RETURN QUERY SELECT 
                v_maintenance_task.vacuum_type::TEXT,
                v_maintenance_task.table_name::TEXT,
                EXTRACT(SECONDS FROM (v_end_time - v_start_time))::INTEGER,
                0::NUMERIC,
                ('ERROR: ' || SQLERRM)::TEXT;
        END;
        
        v_memory_before := v_memory_after;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- PARTITION PRUNING OPTIMIZATION
-- =============================================================================

-- Function to analyze partition pruning effectiveness
CREATE OR REPLACE FUNCTION analyze_partition_pruning()
RETURNS TABLE(
    partition_name TEXT,
    partition_size_mb NUMERIC,
    row_count BIGINT,
    pruning_effectiveness TEXT,
    last_accessed TIMESTAMPTZ
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        schemaname || '.' || tablename as partition_name,
        pg_total_relation_size(schemaname||'.'||tablename) / 1024.0 / 1024.0 as size_mb,
        n_live_tup as row_count,
        CASE 
            WHEN seq_scan = 0 AND idx_scan > 0 THEN 'EXCELLENT'
            WHEN seq_scan < idx_scan THEN 'GOOD'
            WHEN seq_scan = idx_scan THEN 'FAIR'
            ELSE 'POOR'
        END as pruning_effectiveness,
        GREATEST(last_seq_scan, last_idx_scan) as last_accessed
    FROM pg_stat_user_tables
    WHERE tablename LIKE 'application_history_%'
      AND tablename != 'application_history'
    ORDER BY size_mb DESC;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- COMMENTS AND DOCUMENTATION
-- =============================================================================

COMMENT ON FUNCTION analyze_partitioning_candidates() IS 'Analyzes tables for partitioning potential based on size and growth';
COMMENT ON FUNCTION create_application_history_quarterly_partitions(INTEGER, INTEGER) IS 'Creates quarterly partitions for application history';
COMMENT ON FUNCTION optimize_memory_settings_for_session() IS 'Optimizes PostgreSQL memory settings for current session';
COMMENT ON FUNCTION get_applications_paginated(DATE, BIGINT, INTEGER, JSONB) IS 'Memory-efficient cursor-based pagination for large datasets';
COMMENT ON FUNCTION bulk_update_applications_optimized(JSONB[], INTEGER) IS 'Memory-optimized bulk update with monitoring';
COMMENT ON FUNCTION configure_parallel_processing() IS 'Shows current and recommended parallel processing settings';
COMMENT ON FUNCTION get_application_statistics_parallel() IS 'Parallel-optimized aggregation queries';
COMMENT ON FUNCTION intelligent_vacuum_strategy() IS 'Intelligent vacuum strategy based on table activity';
COMMENT ON FUNCTION perform_optimized_maintenance() IS 'Performs optimized maintenance based on intelligent analysis';
COMMENT ON FUNCTION analyze_partition_pruning() IS 'Analyzes partition pruning effectiveness';

-- Partitioning and memory optimization complete
SELECT 'Advanced partitioning and memory optimizations deployed successfully' AS status;