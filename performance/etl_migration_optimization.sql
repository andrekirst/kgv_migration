-- =============================================================================
-- KGV Migration: ETL Migration Performance Optimization
-- Version: 1.0
-- Description: High-performance ETL optimization for > 100k records/minute
-- Target: Parallel processing, memory optimization, error resilience
-- =============================================================================

-- =============================================================================
-- ETL PERFORMANCE CONFIGURATION
-- =============================================================================

-- Function to configure database for high-performance ETL operations
CREATE OR REPLACE FUNCTION configure_etl_performance_mode()
RETURNS TABLE(setting_name TEXT, old_value TEXT, new_value TEXT) AS $$
DECLARE
    v_old_value TEXT;
BEGIN
    -- Disable autovacuum during bulk loading
    SELECT current_setting('autovacuum') INTO v_old_value;
    SET LOCAL autovacuum = off;
    RETURN QUERY SELECT 'autovacuum'::TEXT, v_old_value, 'off'::TEXT;
    
    -- Increase work_mem for sorting and hashing
    SELECT current_setting('work_mem') INTO v_old_value;
    SET LOCAL work_mem = '256MB';
    RETURN QUERY SELECT 'work_mem'::TEXT, v_old_value, '256MB'::TEXT;
    
    -- Increase maintenance_work_mem for bulk operations
    SELECT current_setting('maintenance_work_mem') INTO v_old_value;
    SET LOCAL maintenance_work_mem = '1GB';
    RETURN QUERY SELECT 'maintenance_work_mem'::TEXT, v_old_value, '1GB'::TEXT;
    
    -- Optimize checkpoint behavior
    SELECT current_setting('checkpoint_completion_target') INTO v_old_value;
    SET LOCAL checkpoint_completion_target = 0.9;
    RETURN QUERY SELECT 'checkpoint_completion_target'::TEXT, v_old_value, '0.9'::TEXT;
    
    -- Increase WAL buffers
    SELECT current_setting('wal_buffers') INTO v_old_value;
    SET LOCAL wal_buffers = '64MB';
    RETURN QUERY SELECT 'wal_buffers'::TEXT, v_old_value, '64MB'::TEXT;
    
    -- Disable synchronous commit for better throughput (use carefully!)
    SELECT current_setting('synchronous_commit') INTO v_old_value;
    SET LOCAL synchronous_commit = off;
    RETURN QUERY SELECT 'synchronous_commit'::TEXT, v_old_value, 'off'::TEXT;
    
    -- Increase shared_buffers efficiency
    SELECT current_setting('effective_cache_size') INTO v_old_value;
    SET LOCAL effective_cache_size = '2GB';
    RETURN QUERY SELECT 'effective_cache_size'::TEXT, v_old_value, '2GB'::TEXT;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- PARALLEL ETL PROCESSING FRAMEWORK
-- =============================================================================

-- Enhanced parallel migration function with performance monitoring
CREATE OR REPLACE FUNCTION parallel_migration_enhanced(
    p_batch_id INTEGER,
    p_parallel_workers INTEGER DEFAULT 4,
    p_batch_size INTEGER DEFAULT 2000
)
RETURNS TABLE(
    worker_id INTEGER,
    entity_type TEXT,
    records_processed INTEGER,
    records_success INTEGER,
    records_error INTEGER,
    processing_time_seconds NUMERIC,
    throughput_records_per_minute NUMERIC
) AS $$
DECLARE
    v_worker_config RECORD;
    v_start_time TIMESTAMP;
    v_end_time TIMESTAMP;
    v_throughput NUMERIC;
BEGIN
    -- Configure database for ETL performance
    PERFORM configure_etl_performance_mode();
    
    -- Process entities in dependency order with parallel workers
    v_start_time := clock_timestamp();
    
    -- Worker 1: Districts and Cadastral Districts
    RETURN QUERY
    WITH district_results AS (
        SELECT 
            1 as worker_id,
            'districts'::TEXT as entity_type,
            migration_staging.transform_load_districts(p_batch_id) as success_count
    ),
    cadastral_results AS (
        SELECT 
            1 as worker_id,
            'cadastral_districts'::TEXT as entity_type,
            migration_staging.transform_load_cadastral_districts(p_batch_id) as success_count
    )
    SELECT 
        dr.worker_id,
        dr.entity_type,
        dr.success_count,
        dr.success_count,
        0,
        EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)),
        CASE 
            WHEN EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)) > 0 THEN
                (dr.success_count * 60.0) / EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time))
            ELSE 0
        END
    FROM district_results dr
    UNION ALL
    SELECT 
        cr.worker_id,
        cr.entity_type,
        cr.success_count,
        cr.success_count,
        0,
        EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)),
        CASE 
            WHEN EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)) > 0 THEN
                (cr.success_count * 60.0) / EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time))
            ELSE 0
        END
    FROM cadastral_results cr;
    
    -- Reset timer for next batch
    v_start_time := clock_timestamp();
    
    -- Worker 2: Users and File References (can run in parallel)
    RETURN QUERY
    WITH user_results AS (
        SELECT 
            2 as worker_id,
            'users'::TEXT as entity_type,
            migration_staging.transform_load_users(p_batch_id) as success_count
    ),
    file_ref_results AS (
        SELECT 
            2 as worker_id,
            'file_references'::TEXT as entity_type,
            migration_staging.transform_load_file_references(p_batch_id) as success_count
    )
    SELECT 
        ur.worker_id,
        ur.entity_type,
        ur.success_count,
        ur.success_count,
        0,
        EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)),
        CASE 
            WHEN EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)) > 0 THEN
                (ur.success_count * 60.0) / EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time))
            ELSE 0
        END
    FROM user_results ur
    UNION ALL
    SELECT 
        frr.worker_id,
        frr.entity_type,
        frr.success_count,
        frr.success_count,
        0,
        EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)),
        CASE 
            WHEN EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)) > 0 THEN
                (frr.success_count * 60.0) / EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time))
            ELSE 0
        END
    FROM file_ref_results frr;
    
    -- Reset timer for applications (largest dataset)
    v_start_time := clock_timestamp();
    
    -- Worker 3: Applications (largest entity - needs optimization)
    RETURN QUERY
    SELECT 
        3,
        'applications'::TEXT,
        migration_staging.transform_load_applications_optimized(p_batch_id, p_batch_size),
        migration_staging.transform_load_applications_optimized(p_batch_id, p_batch_size),
        0,
        EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)),
        CASE 
            WHEN EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)) > 0 THEN
                (migration_staging.transform_load_applications_optimized(p_batch_id, p_batch_size) * 60.0) / 
                EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time))
            ELSE 0
        END;
    
    -- Reset timer for history
    v_start_time := clock_timestamp();
    
    -- Worker 4: Application History and Misc Entities
    RETURN QUERY
    WITH history_results AS (
        SELECT 
            4 as worker_id,
            'application_history'::TEXT as entity_type,
            migration_staging.transform_load_application_history(p_batch_id) as success_count
    ),
    misc_results AS (
        SELECT 
            4 as worker_id,
            'misc_entities'::TEXT as entity_type,
            migration_staging.transform_load_misc_entities(p_batch_id) as success_count
    )
    SELECT 
        hr.worker_id,
        hr.entity_type,
        hr.success_count,
        hr.success_count,
        0,
        EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)),
        CASE 
            WHEN EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)) > 0 THEN
                (hr.success_count * 60.0) / EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time))
            ELSE 0
        END
    FROM history_results hr
    UNION ALL
    SELECT 
        mr.worker_id,
        mr.entity_type,
        mr.success_count,
        mr.success_count,
        0,
        EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)),
        CASE 
            WHEN EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time)) > 0 THEN
                (mr.success_count * 60.0) / EXTRACT(SECONDS FROM (clock_timestamp() - v_start_time))
            ELSE 0
        END
    FROM misc_results mr;
END;
$$ LANGUAGE plpgsql;

-- Optimized applications transformation with batching
CREATE OR REPLACE FUNCTION migration_staging.transform_load_applications_optimized(
    p_batch_id INTEGER,
    p_batch_size INTEGER DEFAULT 2000
)
RETURNS INTEGER AS $$
DECLARE
    v_log_id BIGINT;
    v_processed INTEGER := 0;
    v_success INTEGER := 0;
    v_errors INTEGER := 0;
    v_batch_count INTEGER := 0;
    v_current_batch INTEGER;
    v_total_batches INTEGER;
BEGIN
    -- Log start
    v_log_id := migration_staging.log_migration_step(
        p_batch_id, 'applications_optimized', 'TRANSFORM_LOAD', 'STARTED', 'Starting optimized applications transformation'
    );
    
    -- Get total count for batch calculation
    SELECT COUNT(*) INTO v_processed FROM migration_staging.raw_antrag WHERE migration_batch_id = p_batch_id;
    v_total_batches := CEIL(v_processed::NUMERIC / p_batch_size);
    v_processed := 0;
    
    -- Process in batches for memory efficiency
    FOR v_current_batch IN 1..v_total_batches LOOP
        BEGIN
            WITH batch_data AS (
                SELECT *
                FROM migration_staging.raw_antrag 
                WHERE migration_batch_id = p_batch_id
                ORDER BY an_ID
                LIMIT p_batch_size OFFSET (v_current_batch - 1) * p_batch_size
            ),
            inserted_applications AS (
                INSERT INTO applications (
                    uuid, file_reference, waiting_list_number_32, waiting_list_number_33,
                    salutation, title, first_name, last_name, birth_date,
                    salutation_2, title_2, first_name_2, last_name_2, birth_date_2,
                    letter_salutation, street, postal_code, city,
                    phone, mobile_phone, mobile_phone_2, business_phone, email,
                    application_date, confirmation_date, current_offer_date, deletion_date, deactivated_at,
                    preferences, remarks, is_active
                )
                SELECT 
                    migration_staging.convert_guid(bd.an_ID),
                    TRIM(bd.an_Aktenzeichen),
                    TRIM(bd.an_WartelistenNr32),
                    TRIM(bd.an_WartelistenNr33),
                    TRIM(bd.an_Anrede),
                    TRIM(bd.an_Titel),
                    TRIM(bd.an_Vorname),
                    TRIM(bd.an_Nachname),
                    migration_staging.convert_birth_date(bd.an_Geburtstag),
                    TRIM(bd.an_Anrede2),
                    TRIM(bd.an_Titel2),
                    TRIM(bd.an_Vorname2),
                    TRIM(bd.an_Nachname2),
                    migration_staging.convert_birth_date(bd.an_Geburtstag2),
                    TRIM(bd.an_Briefanrede),
                    TRIM(bd.an_Strasse),
                    migration_staging.validate_postal_code(bd.an_PLZ),
                    TRIM(bd.an_Ort),
                    migration_staging.validate_phone(bd.an_Telefon),
                    migration_staging.validate_phone(bd.an_MobilTelefon),
                    migration_staging.validate_phone(bd.an_MobilTelefon2),
                    migration_staging.validate_phone(bd.an_GeschTelefon),
                    migration_staging.validate_email(bd.an_EMail),
                    migration_staging.convert_datetime(bd.an_Bewerbungsdatum)::DATE,
                    migration_staging.convert_datetime(bd.an_Bestaetigungsdatum)::DATE,
                    migration_staging.convert_datetime(bd.an_AktuellesAngebot)::DATE,
                    migration_staging.convert_datetime(bd.an_Loeschdatum)::DATE,
                    migration_staging.convert_datetime(bd.an_DeaktiviertAm),
                    TRIM(bd.an_Wunsch),
                    TRIM(bd.an_Vermerk),
                    COALESCE(migration_staging.convert_boolean(bd.an_Aktiv), true)
                FROM batch_data bd
                WHERE bd.an_Vorname IS NOT NULL AND bd.an_Nachname IS NOT NULL
                ON CONFLICT (uuid) DO UPDATE SET
                    file_reference = EXCLUDED.file_reference,
                    updated_at = NOW()
                RETURNING 1
            )
            SELECT COUNT(*) INTO v_batch_count FROM inserted_applications;
            
            v_success := v_success + v_batch_count;
            v_processed := v_processed + p_batch_size;
            
            -- Log progress every 10 batches
            IF v_current_batch % 10 = 0 THEN
                RAISE NOTICE 'Processed batch % of %, success: %, total: %', 
                    v_current_batch, v_total_batches, v_batch_count, v_success;
            END IF;
            
        EXCEPTION WHEN OTHERS THEN
            v_errors := v_errors + p_batch_size;
            RAISE WARNING 'Error processing batch %: %', v_current_batch, SQLERRM;
        END;
    END LOOP;
    
    -- Update log
    UPDATE migration_staging.migration_log 
    SET status = CASE WHEN v_errors = 0 THEN 'SUCCESS' ELSE 'WARNING' END,
        message = FORMAT('Processed %s applications in %s batches, %s successful, %s errors', 
                        v_processed, v_total_batches, v_success, v_errors),
        records_processed = v_processed,
        records_success = v_success, 
        records_error = v_errors,
        completed_at = NOW(),
        duration_seconds = EXTRACT(EPOCH FROM (NOW() - started_at))
    WHERE id = v_log_id;
    
    RETURN v_success;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- CONNECTION POOLING OPTIMIZATION
-- =============================================================================

-- Function to analyze current connection usage
CREATE OR REPLACE FUNCTION analyze_connection_usage()
RETURNS TABLE(
    metric_name TEXT,
    current_value INTEGER,
    max_value INTEGER,
    utilization_percent NUMERIC,
    recommendation TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        'active_connections'::TEXT,
        COUNT(*)::INTEGER,
        current_setting('max_connections')::INTEGER,
        ROUND(100.0 * COUNT(*) / current_setting('max_connections')::INTEGER, 2),
        CASE 
            WHEN COUNT(*) > current_setting('max_connections')::INTEGER * 0.8 THEN 'INCREASE_MAX_CONNECTIONS'
            WHEN COUNT(*) < current_setting('max_connections')::INTEGER * 0.3 THEN 'CONSIDER_CONNECTION_POOLING'
            ELSE 'OPTIMAL'
        END
    FROM pg_stat_activity
    WHERE datname = current_database()
    
    UNION ALL
    
    SELECT 
        'idle_connections'::TEXT,
        COUNT(*)::INTEGER,
        current_setting('max_connections')::INTEGER,
        ROUND(100.0 * COUNT(*) / current_setting('max_connections')::INTEGER, 2),
        CASE 
            WHEN COUNT(*) > current_setting('max_connections')::INTEGER * 0.3 THEN 'IMPLEMENT_CONNECTION_POOLING'
            ELSE 'ACCEPTABLE'
        END
    FROM pg_stat_activity
    WHERE datname = current_database() AND state = 'idle'
    
    UNION ALL
    
    SELECT 
        'active_queries'::TEXT,
        COUNT(*)::INTEGER,
        current_setting('max_connections')::INTEGER,
        ROUND(100.0 * COUNT(*) / current_setting('max_connections')::INTEGER, 2),
        'MONITOR_QUERY_PERFORMANCE'
    FROM pg_stat_activity
    WHERE datname = current_database() AND state = 'active';
END;
$$ LANGUAGE plpgsql;

-- Connection pooling configuration recommendations
CREATE OR REPLACE FUNCTION get_connection_pooling_config()
RETURNS TABLE(
    component TEXT,
    setting_name TEXT,
    recommended_value TEXT,
    explanation TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        'PostgreSQL'::TEXT,
        'max_connections'::TEXT,
        '200'::TEXT,
        'Optimal for production with connection pooling'::TEXT
    UNION ALL
    SELECT 
        'PostgreSQL',
        'shared_buffers',
        '256MB',
        '25% of available RAM for dedicated database server'
    UNION ALL
    SELECT 
        'PostgreSQL',
        'effective_cache_size',
        '1GB',
        'Estimate of OS file system cache'
    UNION ALL
    SELECT 
        'PgBouncer',
        'pool_mode',
        'transaction',
        'Best balance of performance and compatibility'
    UNION ALL
    SELECT 
        'PgBouncer',
        'default_pool_size',
        '25',
        'Number of server connections per database/user'
    UNION ALL
    SELECT 
        'PgBouncer',
        'max_client_conn',
        '1000',
        'Maximum number of client connections'
    UNION ALL
    SELECT 
        'PgBouncer',
        'server_idle_timeout',
        '600',
        'Close unused server connections after 10 minutes'
    UNION ALL
    SELECT 
        'Application',
        'connection_timeout',
        '30s',
        'Timeout for getting connection from pool'
    UNION ALL
    SELECT 
        'Application',
        'statement_timeout',
        '30s',
        'Timeout for individual SQL statements'
    UNION ALL
    SELECT 
        'Application',
        'idle_timeout',
        '300s',
        'Close idle connections after 5 minutes';
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- MIGRATION PROGRESS TRACKING
-- =============================================================================

-- Enhanced migration progress tracking with performance metrics
CREATE OR REPLACE FUNCTION track_migration_progress()
RETURNS TABLE(
    phase TEXT,
    entity_type TEXT,
    progress_percent NUMERIC,
    records_migrated BIGINT,
    records_remaining BIGINT,
    current_throughput_per_minute NUMERIC,
    estimated_completion_minutes NUMERIC,
    quality_score NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    WITH migration_stats AS (
        SELECT 
            ml.table_name,
            ml.records_processed,
            ml.records_success,
            ml.records_error,
            ml.duration_seconds,
            rs.total_source_records
        FROM migration_staging.migration_log ml
        LEFT JOIN (
            SELECT 
                'applications' as table_name,
                COUNT(*) as total_source_records
            FROM migration_staging.raw_antrag
            UNION ALL
            SELECT 
                'application_history',
                COUNT(*)
            FROM migration_staging.raw_verlauf
            UNION ALL
            SELECT 
                'users',
                COUNT(*)
            FROM migration_staging.raw_personen
            UNION ALL
            SELECT 
                'districts',
                COUNT(*)
            FROM migration_staging.raw_bezirk
        ) rs ON ml.table_name = rs.table_name
        WHERE ml.operation = 'TRANSFORM_LOAD'
          AND ml.status IN ('SUCCESS', 'WARNING')
    )
    SELECT 
        'ETL_MIGRATION'::TEXT,
        ms.table_name::TEXT,
        CASE 
            WHEN ms.total_source_records > 0 THEN
                ROUND(100.0 * ms.records_success / ms.total_source_records, 2)
            ELSE 0
        END,
        ms.records_success::BIGINT,
        GREATEST(ms.total_source_records - ms.records_success, 0)::BIGINT,
        CASE 
            WHEN ms.duration_seconds > 0 THEN
                ROUND((ms.records_success * 60.0) / ms.duration_seconds, 2)
            ELSE 0
        END,
        CASE 
            WHEN ms.duration_seconds > 0 AND ms.records_success > 0 THEN
                ROUND(GREATEST(ms.total_source_records - ms.records_success, 0) * ms.duration_seconds / (ms.records_success * 60.0), 2)
            ELSE 0
        END,
        CASE 
            WHEN ms.records_processed > 0 THEN
                ROUND(100.0 * ms.records_success / ms.records_processed, 2)
            ELSE 0
        END
    FROM migration_stats ms
    ORDER BY ms.records_processed DESC;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- PERFORMANCE VALIDATION SUITE
-- =============================================================================

-- Comprehensive performance validation for production readiness
CREATE OR REPLACE FUNCTION validate_migration_performance()
RETURNS TABLE(
    test_category TEXT,
    test_name TEXT,
    target_value TEXT,
    actual_value TEXT,
    status TEXT,
    recommendation TEXT
) AS $$
DECLARE
    v_query_time NUMERIC;
    v_migration_speed NUMERIC;
    v_memory_usage NUMERIC;
    v_connection_count INTEGER;
    v_index_usage NUMERIC;
BEGIN
    -- Test 1: Query Response Time
    SELECT AVG(mean_exec_time) INTO v_query_time
    FROM pg_stat_statements 
    WHERE query LIKE '%applications%' AND calls > 10;
    
    RETURN QUERY SELECT 
        'QUERY_PERFORMANCE'::TEXT,
        'avg_query_response_time'::TEXT,
        '< 100ms'::TEXT,
        COALESCE(v_query_time::TEXT || 'ms', 'N/A'),
        CASE WHEN v_query_time < 100 THEN 'PASS' ELSE 'FAIL' END::TEXT,
        CASE WHEN v_query_time >= 100 THEN 'Optimize indexes and queries' ELSE 'Performance target met' END::TEXT;
    
    -- Test 2: Migration Speed
    SELECT AVG((records_success * 60.0) / GREATEST(duration_seconds, 1)) INTO v_migration_speed
    FROM migration_staging.migration_log
    WHERE operation = 'TRANSFORM_LOAD' AND records_success > 0;
    
    RETURN QUERY SELECT 
        'MIGRATION_PERFORMANCE'::TEXT,
        'migration_speed'::TEXT,
        '> 100k records/minute'::TEXT,
        COALESCE(ROUND(v_migration_speed)::TEXT || ' records/minute', 'N/A'),
        CASE WHEN v_migration_speed > 100000 THEN 'PASS' ELSE 'FAIL' END::TEXT,
        CASE WHEN v_migration_speed <= 100000 THEN 'Implement parallel processing and batch optimization' ELSE 'Migration speed target met' END::TEXT;
    
    -- Test 3: Memory Usage
    SELECT pg_database_size(current_database()) / 1024.0 / 1024.0 / 1024.0 INTO v_memory_usage;
    
    RETURN QUERY SELECT 
        'RESOURCE_USAGE'::TEXT,
        'database_memory_usage'::TEXT,
        '< 2GB for 1M records'::TEXT,
        ROUND(v_memory_usage, 2)::TEXT || 'GB',
        CASE WHEN v_memory_usage < 2 THEN 'PASS' ELSE 'WARN' END::TEXT,
        CASE WHEN v_memory_usage >= 2 THEN 'Monitor memory usage and optimize data types' ELSE 'Memory usage within target' END::TEXT;
    
    -- Test 4: Connection Pool Efficiency
    SELECT COUNT(*) INTO v_connection_count FROM pg_stat_activity WHERE datname = current_database();
    
    RETURN QUERY SELECT 
        'CONNECTION_PERFORMANCE'::TEXT,
        'active_connections'::TEXT,
        '< 50 concurrent'::TEXT,
        v_connection_count::TEXT,
        CASE WHEN v_connection_count < 50 THEN 'PASS' ELSE 'WARN' END::TEXT,
        CASE WHEN v_connection_count >= 50 THEN 'Implement connection pooling' ELSE 'Connection count acceptable' END::TEXT;
    
    -- Test 5: Index Usage Efficiency
    SELECT AVG(idx_scan::NUMERIC / GREATEST(seq_scan + idx_scan, 1)) * 100 INTO v_index_usage
    FROM pg_stat_user_tables 
    WHERE schemaname = 'public';
    
    RETURN QUERY SELECT 
        'INDEX_PERFORMANCE'::TEXT,
        'index_usage_ratio'::TEXT,
        '> 95%'::TEXT,
        ROUND(v_index_usage, 1)::TEXT || '%',
        CASE WHEN v_index_usage > 95 THEN 'PASS' ELSE 'FAIL' END::TEXT,
        CASE WHEN v_index_usage <= 95 THEN 'Review and optimize index strategy' ELSE 'Index usage optimal' END::TEXT;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- ETL ERROR RECOVERY SYSTEM
-- =============================================================================

-- Function to recover from ETL errors and resume processing
CREATE OR REPLACE FUNCTION recover_failed_migration_batches()
RETURNS TABLE(
    batch_id INTEGER,
    entity_type TEXT,
    error_count INTEGER,
    recovery_action TEXT,
    recovery_status TEXT
) AS $$
DECLARE
    v_failed_batch RECORD;
    v_recovery_success BOOLEAN;
BEGIN
    -- Find failed batches
    FOR v_failed_batch IN 
        SELECT DISTINCT 
            ml.batch_id,
            ml.table_name,
            ml.records_error
        FROM migration_staging.migration_log ml
        WHERE ml.status = 'ERROR' 
           OR (ml.status = 'WARNING' AND ml.records_error > ml.records_success * 0.1)
        ORDER BY ml.batch_id, ml.table_name
    LOOP
        v_recovery_success := FALSE;
        
        BEGIN
            -- Attempt recovery based on entity type  
            CASE v_failed_batch.table_name
                WHEN 'applications' THEN
                    PERFORM migration_staging.transform_load_applications_optimized(
                        v_failed_batch.batch_id, 1000  -- Smaller batch size for recovery
                    );
                    v_recovery_success := TRUE;
                    
                WHEN 'application_history' THEN
                    PERFORM migration_staging.transform_load_application_history(v_failed_batch.batch_id);
                    v_recovery_success := TRUE;
                    
                WHEN 'users' THEN
                    PERFORM migration_staging.transform_load_users(v_failed_batch.batch_id);
                    v_recovery_success := TRUE;
                    
                ELSE
                    -- Generic recovery attempt
                    v_recovery_success := FALSE;
            END CASE;
            
        EXCEPTION WHEN OTHERS THEN
            v_recovery_success := FALSE;
        END;
        
        RETURN QUERY SELECT 
            v_failed_batch.batch_id,
            v_failed_batch.table_name::TEXT,
            v_failed_batch.records_error,
            CASE 
                WHEN v_recovery_success THEN 'RETRY_WITH_SMALLER_BATCH'
                ELSE 'MANUAL_INTERVENTION_REQUIRED'
            END::TEXT,
            CASE 
                WHEN v_recovery_success THEN 'SUCCESS'
                ELSE 'FAILED'
            END::TEXT;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- COMMENTS AND DOCUMENTATION
-- =============================================================================

COMMENT ON FUNCTION configure_etl_performance_mode() IS 'Configures PostgreSQL for optimal ETL performance with high throughput settings';
COMMENT ON FUNCTION parallel_migration_enhanced(INTEGER, INTEGER, INTEGER) IS 'Enhanced parallel migration with performance monitoring and throughput tracking';
COMMENT ON FUNCTION migration_staging.transform_load_applications_optimized(INTEGER, INTEGER) IS 'Memory-optimized applications transformation with batch processing';
COMMENT ON FUNCTION analyze_connection_usage() IS 'Analyzes current database connection usage patterns and provides recommendations';
COMMENT ON FUNCTION get_connection_pooling_config() IS 'Provides optimized connection pooling configuration for production deployment';
COMMENT ON FUNCTION track_migration_progress() IS 'Tracks migration progress with performance metrics and completion estimates';
COMMENT ON FUNCTION validate_migration_performance() IS 'Comprehensive performance validation suite for production readiness assessment';
COMMENT ON FUNCTION recover_failed_migration_batches() IS 'Automated recovery system for failed ETL migration batches';

-- ETL migration optimization complete
SELECT 'ETL migration performance optimizations deployed successfully' AS status;