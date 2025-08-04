-- =============================================================================
-- KGV Migration: Automated Migration Scripts
-- Version: 1.0
-- Description: Automated SQL scripts to support migration process
-- =============================================================================

-- =============================================================================
-- MIGRATION ORCHESTRATION FUNCTIONS
-- =============================================================================

-- Function to validate migration prerequisites
CREATE OR REPLACE FUNCTION migration_staging.validate_migration_prerequisites()
RETURNS TABLE(
    check_name TEXT,
    status TEXT,
    details TEXT,
    is_blocking BOOLEAN
) AS $$
BEGIN
    RETURN QUERY
    -- Check PostgreSQL version
    SELECT 
        'postgresql_version' as check_name,
        CASE 
            WHEN current_setting('server_version_num')::INTEGER >= 150000 THEN 'PASS'
            ELSE 'FAIL'
        END as status,
        'PostgreSQL version: ' || version() as details,
        true as is_blocking
    
    UNION ALL
    
    -- Check required extensions
    SELECT 
        'required_extensions',
        CASE 
            WHEN COUNT(*) = 3 THEN 'PASS'
            ELSE 'FAIL'
        END,
        'Available extensions: ' || string_agg(extname, ', '),
        true
    FROM pg_extension 
    WHERE extname IN ('uuid-ossp', 'pg_trgm', 'btree_gin')
    
    UNION ALL
    
    -- Check disk space
    SELECT 
        'disk_space',
        CASE 
            WHEN pg_size_pretty(pg_database_size(current_database())) < '10 GB' THEN 'PASS'
            ELSE 'WARN'
        END,
        'Current database size: ' || pg_size_pretty(pg_database_size(current_database())),
        false
    
    UNION ALL
    
    -- Check table readiness
    SELECT 
        'target_tables',
        CASE 
            WHEN COUNT(*) >= 8 THEN 'PASS'
            ELSE 'FAIL'
        END,
        'Created tables: ' || COUNT(*)::TEXT,
        true
    FROM information_schema.tables 
    WHERE table_schema = 'public' 
      AND table_name IN ('districts', 'applications', 'users', 'file_references', 
                        'entry_numbers', 'cadastral_districts', 'application_history', 'identifiers')
    
    UNION ALL
    
    -- Check staging schema
    SELECT 
        'staging_schema',
        CASE 
            WHEN COUNT(*) > 0 THEN 'PASS'
            ELSE 'FAIL'
        END,
        'Staging tables available: ' || COUNT(*)::TEXT,
        true
    FROM information_schema.tables 
    WHERE table_schema = 'migration_staging'
    
    UNION ALL
    
    -- Check functions
    SELECT 
        'migration_functions',
        CASE 
            WHEN COUNT(*) >= 5 THEN 'PASS'
            ELSE 'FAIL'
        END,
        'Migration functions available: ' || COUNT(*)::TEXT,
        true
    FROM information_schema.routines 
    WHERE routine_schema = 'migration_staging' 
      AND routine_name LIKE '%transform_load%';
END;
$$ LANGUAGE plpgsql;

-- Function to initialize migration batch
CREATE OR REPLACE FUNCTION migration_staging.initialize_migration_batch()
RETURNS TABLE(
    batch_id INTEGER,
    initialization_status TEXT,
    prerequisites_passed BOOLEAN,
    blocking_issues TEXT[]
) AS $$
DECLARE
    v_batch_id INTEGER;
    v_prerequisites_passed BOOLEAN := true;
    v_blocking_issues TEXT[] := ARRAY[]::TEXT[];
    v_prereq RECORD;
BEGIN
    -- Generate batch ID based on timestamp
    v_batch_id := EXTRACT(EPOCH FROM NOW())::INTEGER;
    
    -- Check prerequisites
    FOR v_prereq IN 
        SELECT * FROM migration_staging.validate_migration_prerequisites()
    LOOP
        IF v_prereq.is_blocking AND v_prereq.status != 'PASS' THEN
            v_prerequisites_passed := false;
            v_blocking_issues := array_append(v_blocking_issues, v_prereq.check_name || ': ' || v_prereq.details);
        END IF;
    END LOOP;
    
    -- Log initialization
    PERFORM migration_staging.log_migration_step(
        v_batch_id,
        'INITIALIZATION',
        'INITIALIZE',
        CASE WHEN v_prerequisites_passed THEN 'SUCCESS' ELSE 'FAILED' END,
        CASE 
            WHEN v_prerequisites_passed THEN 'Migration prerequisites validated successfully'
            ELSE 'Migration prerequisites failed: ' || array_to_string(v_blocking_issues, '; ')
        END
    );
    
    RETURN QUERY SELECT 
        v_batch_id,
        CASE WHEN v_prerequisites_passed THEN 'READY' ELSE 'BLOCKED' END,
        v_prerequisites_passed,
        v_blocking_issues;
END;
$$ LANGUAGE plpgsql;

-- Function to finalize migration batch
CREATE OR REPLACE FUNCTION migration_staging.finalize_migration_batch(
    p_batch_id INTEGER
) RETURNS TABLE(
    final_status TEXT,
    total_records_processed BIGINT,
    total_records_success BIGINT,
    total_records_error BIGINT,
    total_duration_seconds INTEGER,
    data_quality_score NUMERIC
) AS $$
DECLARE
    v_stats RECORD;
    v_quality_score NUMERIC;
BEGIN
    -- Calculate migration statistics
    SELECT 
        SUM(records_processed) as total_processed,
        SUM(records_success) as total_success,
        SUM(records_error) as total_error,
        SUM(duration_seconds) as total_duration
    INTO v_stats
    FROM migration_staging.migration_log
    WHERE batch_id = p_batch_id
      AND operation = 'TRANSFORM_LOAD';
    
    -- Calculate data quality score (percentage of successful records)
    v_quality_score := CASE 
        WHEN v_stats.total_processed > 0 THEN 
            ROUND(100.0 * v_stats.total_success / v_stats.total_processed, 2)
        ELSE 0
    END;
    
    -- Log finalization
    PERFORM migration_staging.log_migration_step(
        p_batch_id,
        'FINALIZATION',
        'FINALIZE',
        CASE 
            WHEN v_quality_score >= 99.0 THEN 'SUCCESS'
            WHEN v_quality_score >= 95.0 THEN 'WARNING'
            ELSE 'FAILED'
        END,
        format('Migration completed with %s%% success rate', v_quality_score)
    );
    
    RETURN QUERY SELECT 
        CASE 
            WHEN v_quality_score >= 99.0 THEN 'SUCCESS'
            WHEN v_quality_score >= 95.0 THEN 'WARNING'
            ELSE 'FAILED'
        END as final_status,
        COALESCE(v_stats.total_processed, 0),
        COALESCE(v_stats.total_success, 0),
        COALESCE(v_stats.total_error, 0),
        COALESCE(v_stats.total_duration, 0),
        v_quality_score;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- DATA COMPARISON AND VALIDATION FUNCTIONS
-- =============================================================================

-- Function to compare record counts between staging and target tables
CREATE OR REPLACE FUNCTION migration_staging.compare_record_counts(
    p_batch_id INTEGER
) RETURNS TABLE(
    entity_name TEXT,
    staging_count BIGINT,
    target_count BIGINT,
    difference BIGINT,
    match_status TEXT
) AS $$
BEGIN
    RETURN QUERY
    WITH staging_counts AS (
        SELECT 'districts' as entity, COUNT(*) as staging_count FROM migration_staging.raw_bezirk WHERE migration_batch_id = p_batch_id
        UNION ALL
        SELECT 'applications', COUNT(*) FROM migration_staging.raw_antrag WHERE migration_batch_id = p_batch_id
        UNION ALL
        SELECT 'users', COUNT(*) FROM migration_staging.raw_personen WHERE migration_batch_id = p_batch_id
        UNION ALL
        SELECT 'file_references', COUNT(*) FROM migration_staging.raw_aktenzeichen WHERE migration_batch_id = p_batch_id
        UNION ALL
        SELECT 'entry_numbers', COUNT(*) FROM migration_staging.raw_eingangsnummer WHERE migration_batch_id = p_batch_id
        UNION ALL
        SELECT 'cadastral_districts', COUNT(*) FROM migration_staging.raw_katasterbezirk WHERE migration_batch_id = p_batch_id
        UNION ALL
        SELECT 'application_history', COUNT(*) FROM migration_staging.raw_verlauf WHERE migration_batch_id = p_batch_id
        UNION ALL
        SELECT 'identifiers', COUNT(*) FROM migration_staging.raw_kennungen WHERE migration_batch_id = p_batch_id
    ),
    target_counts AS (
        SELECT 'districts' as entity, COUNT(*) as target_count FROM districts
        UNION ALL
        SELECT 'applications', COUNT(*) FROM applications
        UNION ALL
        SELECT 'users', COUNT(*) FROM users
        UNION ALL
        SELECT 'file_references', COUNT(*) FROM file_references
        UNION ALL
        SELECT 'entry_numbers', COUNT(*) FROM entry_numbers
        UNION ALL
        SELECT 'cadastral_districts', COUNT(*) FROM cadastral_districts
        UNION ALL
        SELECT 'application_history', COUNT(*) FROM application_history
        UNION ALL
        SELECT 'identifiers', COUNT(*) FROM identifiers
    )
    SELECT 
        sc.entity,
        sc.staging_count,
        tc.target_count,
        tc.target_count - sc.staging_count as difference,
        CASE 
            WHEN tc.target_count = sc.staging_count THEN 'EXACT_MATCH'
            WHEN tc.target_count > sc.staging_count * 0.95 THEN 'ACCEPTABLE'
            ELSE 'MISMATCH'
        END as match_status
    FROM staging_counts sc
    JOIN target_counts tc ON sc.entity = tc.entity
    ORDER BY sc.entity;
END;
$$ LANGUAGE plpgsql;

-- Function to validate critical business data integrity
CREATE OR REPLACE FUNCTION migration_staging.validate_business_data_integrity(
    p_batch_id INTEGER
) RETURNS TABLE(
    validation_name TEXT,
    status TEXT,
    details TEXT,
    sample_data JSONB
) AS $$
BEGIN
    RETURN QUERY
    -- Validate waiting list number integrity
    WITH waiting_list_validation AS (
        SELECT 
            'waiting_list_32_sequence' as validation_name,
            CASE 
                WHEN COUNT(*) = 0 THEN 'PASS'
                ELSE 'FAIL'
            END as status,
            CASE 
                WHEN COUNT(*) = 0 THEN 'All waiting list 32 numbers are sequential'
                ELSE format('%s gaps found in waiting list 32 sequence', COUNT(*))
            END as details,
            CASE 
                WHEN COUNT(*) > 0 THEN jsonb_agg(row_to_json(t)) 
                ELSE '[]'::JSONB 
            END as sample_data
        FROM (
            SELECT 
                id, 
                waiting_list_number_32,
                ROW_NUMBER() OVER (ORDER BY application_date) as expected_rank
            FROM applications 
            WHERE is_active = true 
              AND waiting_list_number_32 IS NOT NULL
              AND waiting_list_number_32 != ROW_NUMBER() OVER (ORDER BY application_date)::TEXT
            LIMIT 5
        ) t
    ),
    file_reference_validation AS (
        SELECT 
            'file_reference_uniqueness',
            CASE 
                WHEN COUNT(*) = 0 THEN 'PASS'
                ELSE 'FAIL'
            END,
            CASE 
                WHEN COUNT(*) = 0 THEN 'All file references are unique'
                ELSE format('%s duplicate file references found', COUNT(*))
            END,
            CASE 
                WHEN COUNT(*) > 0 THEN jsonb_agg(row_to_json(t))
                ELSE '[]'::JSONB
            END
        FROM (
            SELECT district_code, number, year, COUNT(*) as duplicate_count
            FROM file_references
            GROUP BY district_code, number, year
            HAVING COUNT(*) > 1
            LIMIT 5
        ) t
    ),
    application_dates_validation AS (
        SELECT 
            'application_date_consistency',
            CASE 
                WHEN COUNT(*) = 0 THEN 'PASS'
                ELSE 'WARN'
            END,
            CASE 
                WHEN COUNT(*) = 0 THEN 'All application dates are consistent'
                ELSE format('%s applications have confirmation before application date', COUNT(*))
            END,
            CASE 
                WHEN COUNT(*) > 0 THEN jsonb_agg(row_to_json(t))
                ELSE '[]'::JSONB
            END
        FROM (
            SELECT id, application_date, confirmation_date
            FROM applications
            WHERE application_date IS NOT NULL 
              AND confirmation_date IS NOT NULL
              AND confirmation_date < application_date
            LIMIT 5
        ) t
    )
    SELECT * FROM waiting_list_validation
    UNION ALL
    SELECT * FROM file_reference_validation
    UNION ALL
    SELECT * FROM application_dates_validation;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- ROLLBACK SUPPORT FUNCTIONS
-- =============================================================================

-- Function to create rollback checkpoint
CREATE OR REPLACE FUNCTION migration_staging.create_rollback_checkpoint(
    p_batch_id INTEGER,
    p_checkpoint_name TEXT
) RETURNS BIGINT AS $$
DECLARE
    v_checkpoint_id BIGINT;
BEGIN
    -- Create recovery checkpoint
    INSERT INTO backup_management.recovery_checkpoints (
        checkpoint_name,
        description,
        checkpoint_time,
        lsn_position,
        metadata
    ) VALUES (
        p_checkpoint_name,
        format('Rollback checkpoint for migration batch %s', p_batch_id),
        NOW(),
        pg_current_wal_lsn(),
        jsonb_build_object(
            'batch_id', p_batch_id,
            'checkpoint_type', 'ROLLBACK',
            'database_size', pg_database_size(current_database())
        )
    ) RETURNING id INTO v_checkpoint_id;
    
    -- Log checkpoint creation
    PERFORM migration_staging.log_migration_step(
        p_batch_id,
        'ROLLBACK_CHECKPOINT',
        'CREATE',
        'SUCCESS',
        format('Rollback checkpoint created: %s (ID: %s)', p_checkpoint_name, v_checkpoint_id)
    );
    
    RETURN v_checkpoint_id;
END;
$$ LANGUAGE plpgsql;

-- Function to execute migration rollback
CREATE OR REPLACE FUNCTION migration_staging.execute_rollback(
    p_batch_id INTEGER,
    p_rollback_type TEXT DEFAULT 'PARTIAL' -- 'PARTIAL', 'FULL'
) RETURNS TABLE(
    rollback_step TEXT,
    status TEXT,
    records_affected BIGINT,
    details TEXT
) AS $$
DECLARE
    v_start_time TIMESTAMP := NOW();
    v_records_deleted BIGINT;
BEGIN
    -- Log rollback start
    PERFORM migration_staging.log_migration_step(
        p_batch_id,
        'ROLLBACK',
        'STARTED',
        'SUCCESS',
        format('Starting %s rollback for batch %s', p_rollback_type, p_batch_id)
    );
    
    IF p_rollback_type = 'FULL' THEN
        -- Full rollback: Remove all migrated data
        
        -- Delete application history
        DELETE FROM application_history 
        WHERE created_at >= (
            SELECT MIN(started_at) 
            FROM migration_staging.migration_log 
            WHERE batch_id = p_batch_id
        );
        GET DIAGNOSTICS v_records_deleted = ROW_COUNT;
        RETURN QUERY SELECT 'delete_application_history'::TEXT, 'SUCCESS'::TEXT, v_records_deleted, 'Application history records deleted'::TEXT;
        
        -- Delete applications
        DELETE FROM applications 
        WHERE created_at >= (
            SELECT MIN(started_at) 
            FROM migration_staging.migration_log 
            WHERE batch_id = p_batch_id
        );
        GET DIAGNOSTICS v_records_deleted = ROW_COUNT;
        RETURN QUERY SELECT 'delete_applications', 'SUCCESS', v_records_deleted, 'Application records deleted';
        
        -- Delete other entities
        DELETE FROM identifiers 
        WHERE created_at >= (
            SELECT MIN(started_at) 
            FROM migration_staging.migration_log 
            WHERE batch_id = p_batch_id
        );
        GET DIAGNOSTICS v_records_deleted = ROW_COUNT;
        RETURN QUERY SELECT 'delete_identifiers', 'SUCCESS', v_records_deleted, 'Identifier records deleted';
        
        DELETE FROM field_mappings 
        WHERE created_at >= (
            SELECT MIN(started_at) 
            FROM migration_staging.migration_log 
            WHERE batch_id = p_batch_id
        );
        GET DIAGNOSTICS v_records_deleted = ROW_COUNT;
        RETURN QUERY SELECT 'delete_field_mappings', 'SUCCESS', v_records_deleted, 'Field mapping records deleted';
        
        DELETE FROM file_references 
        WHERE created_at >= (
            SELECT MIN(started_at) 
            FROM migration_staging.migration_log 
            WHERE batch_id = p_batch_id
        );
        GET DIAGNOSTICS v_records_deleted = ROW_COUNT;
        RETURN QUERY SELECT 'delete_file_references', 'SUCCESS', v_records_deleted, 'File reference records deleted';
        
        DELETE FROM entry_numbers 
        WHERE created_at >= (
            SELECT MIN(started_at) 
            FROM migration_staging.migration_log 
            WHERE batch_id = p_batch_id
        );
        GET DIAGNOSTICS v_records_deleted = ROW_COUNT;
        RETURN QUERY SELECT 'delete_entry_numbers', 'SUCCESS', v_records_deleted, 'Entry number records deleted';
        
        DELETE FROM users 
        WHERE created_at >= (
            SELECT MIN(started_at) 
            FROM migration_staging.migration_log 
            WHERE batch_id = p_batch_id
        );
        GET DIAGNOSTICS v_records_deleted = ROW_COUNT;
        RETURN QUERY SELECT 'delete_users', 'SUCCESS', v_records_deleted, 'User records deleted';
        
        DELETE FROM cadastral_districts 
        WHERE created_at >= (
            SELECT MIN(started_at) 
            FROM migration_staging.migration_log 
            WHERE batch_id = p_batch_id
        );
        GET DIAGNOSTICS v_records_deleted = ROW_COUNT;
        RETURN QUERY SELECT 'delete_cadastral_districts', 'SUCCESS', v_records_deleted, 'Cadastral district records deleted';
        
        DELETE FROM districts 
        WHERE created_at >= (
            SELECT MIN(started_at) 
            FROM migration_staging.migration_log 
            WHERE batch_id = p_batch_id
        );
        GET DIAGNOSTICS v_records_deleted = ROW_COUNT;
        RETURN QUERY SELECT 'delete_districts', 'SUCCESS', v_records_deleted, 'District records deleted';
        
    ELSE
        -- Partial rollback: Mark records as inactive instead of deleting
        UPDATE applications 
        SET is_active = false, 
            updated_at = NOW()
        WHERE created_at >= (
            SELECT MIN(started_at) 
            FROM migration_staging.migration_log 
            WHERE batch_id = p_batch_id
        );
        GET DIAGNOSTICS v_records_deleted = ROW_COUNT;
        RETURN QUERY SELECT 'deactivate_applications', 'SUCCESS', v_records_deleted, 'Application records deactivated';
        
    END IF;
    
    -- Clean up staging data
    DELETE FROM migration_staging.raw_antrag WHERE migration_batch_id = p_batch_id;
    DELETE FROM migration_staging.raw_bezirk WHERE migration_batch_id = p_batch_id;
    DELETE FROM migration_staging.raw_aktenzeichen WHERE migration_batch_id = p_batch_id;
    DELETE FROM migration_staging.raw_eingangsnummer WHERE migration_batch_id = p_batch_id;
    DELETE FROM migration_staging.raw_katasterbezirk WHERE migration_batch_id = p_batch_id;
    DELETE FROM migration_staging.raw_kennungen WHERE migration_batch_id = p_batch_id;
    DELETE FROM migration_staging.raw_mischenfelder WHERE migration_batch_id = p_batch_id;
    DELETE FROM migration_staging.raw_personen WHERE migration_batch_id = p_batch_id;
    DELETE FROM migration_staging.raw_verlauf WHERE migration_batch_id = p_batch_id;
    DELETE FROM migration_staging.raw_bezirke_katasterbezirke WHERE migration_batch_id = p_batch_id;
    
    RETURN QUERY SELECT 'cleanup_staging_data', 'SUCCESS', 0::BIGINT, 'Staging data cleaned up';
    
    -- Log rollback completion
    PERFORM migration_staging.log_migration_step(
        p_batch_id,
        'ROLLBACK',
        'COMPLETED',
        'SUCCESS',
        format('%s rollback completed in %s seconds', p_rollback_type, EXTRACT(EPOCH FROM (NOW() - v_start_time)))
    );
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- MIGRATION REPORTING FUNCTIONS
-- =============================================================================

-- Function to generate migration summary report
CREATE OR REPLACE FUNCTION migration_staging.generate_migration_report(
    p_batch_id INTEGER
) RETURNS TABLE(
    section TEXT,
    metric TEXT,
    value TEXT,
    status TEXT
) AS $$
BEGIN
    RETURN QUERY
    -- Migration overview
    SELECT 
        'Overview' as section,
        'Batch ID' as metric,
        p_batch_id::TEXT as value,
        'INFO' as status
    
    UNION ALL
    
    SELECT 
        'Overview',
        'Migration Start Time',
        MIN(started_at)::TEXT,
        'INFO'
    FROM migration_staging.migration_log 
    WHERE batch_id = p_batch_id
    
    UNION ALL
    
    SELECT 
        'Overview',
        'Migration End Time',
        MAX(completed_at)::TEXT,
        'INFO'
    FROM migration_staging.migration_log 
    WHERE batch_id = p_batch_id
    
    UNION ALL
    
    SELECT 
        'Overview',
        'Total Duration (seconds)',
        EXTRACT(EPOCH FROM (MAX(completed_at) - MIN(started_at)))::TEXT,
        'INFO'
    FROM migration_staging.migration_log 
    WHERE batch_id = p_batch_id
    
    UNION ALL
    
    -- Data volume metrics
    SELECT 
        'Data Volume',
        'Total Records Processed',
        SUM(records_processed)::TEXT,
        CASE WHEN SUM(records_processed) > 0 THEN 'SUCCESS' ELSE 'WARNING' END
    FROM migration_staging.migration_log 
    WHERE batch_id = p_batch_id AND operation = 'TRANSFORM_LOAD'
    
    UNION ALL
    
    SELECT 
        'Data Volume',
        'Total Records Success',
        SUM(records_success)::TEXT,
        CASE WHEN SUM(records_success) = SUM(records_processed) THEN 'SUCCESS' ELSE 'WARNING' END
    FROM migration_staging.migration_log 
    WHERE batch_id = p_batch_id AND operation = 'TRANSFORM_LOAD'
    
    UNION ALL
    
    SELECT 
        'Data Volume',
        'Total Records Error',
        SUM(records_error)::TEXT,
        CASE WHEN SUM(records_error) = 0 THEN 'SUCCESS' ELSE 'ERROR' END
    FROM migration_staging.migration_log 
    WHERE batch_id = p_batch_id AND operation = 'TRANSFORM_LOAD'
    
    UNION ALL
    
    SELECT 
        'Data Volume',
        'Success Rate (%)',
        ROUND(100.0 * SUM(records_success) / NULLIF(SUM(records_processed), 0), 2)::TEXT,
        CASE 
            WHEN ROUND(100.0 * SUM(records_success) / NULLIF(SUM(records_processed), 0), 2) >= 99 THEN 'SUCCESS'
            WHEN ROUND(100.0 * SUM(records_success) / NULLIF(SUM(records_processed), 0), 2) >= 95 THEN 'WARNING'
            ELSE 'ERROR'
        END
    FROM migration_staging.migration_log 
    WHERE batch_id = p_batch_id AND operation = 'TRANSFORM_LOAD'
    
    UNION ALL
    
    -- Performance metrics
    SELECT 
        'Performance',
        'Average Processing Speed (records/second)',
        ROUND(AVG(records_processed::NUMERIC / NULLIF(duration_seconds, 0)), 2)::TEXT,
        'INFO'
    FROM migration_staging.migration_log 
    WHERE batch_id = p_batch_id AND operation = 'TRANSFORM_LOAD' AND duration_seconds > 0
    
    UNION ALL
    
    -- Quality metrics
    SELECT 
        'Quality',
        'Data Quality Rules Executed',
        COUNT(*)::TEXT,
        CASE WHEN COUNT(*) > 0 THEN 'SUCCESS' ELSE 'WARNING' END
    FROM data_quality.check_results 
    WHERE batch_id = p_batch_id
    
    UNION ALL
    
    SELECT 
        'Quality',
        'Quality Rules Passed',
        COUNT(*) FILTER (WHERE violations_count = 0)::TEXT,
        'INFO'
    FROM data_quality.check_results 
    WHERE batch_id = p_batch_id
    
    UNION ALL
    
    SELECT 
        'Quality',
        'Total Quality Violations',
        SUM(violations_count)::TEXT,
        CASE WHEN SUM(violations_count) = 0 THEN 'SUCCESS' ELSE 'WARNING' END
    FROM data_quality.check_results 
    WHERE batch_id = p_batch_id;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- COMMENTS
-- =============================================================================

COMMENT ON FUNCTION migration_staging.validate_migration_prerequisites() IS 'Validates all prerequisites before starting migration';
COMMENT ON FUNCTION migration_staging.initialize_migration_batch() IS 'Initializes a new migration batch with prerequisite validation';
COMMENT ON FUNCTION migration_staging.finalize_migration_batch(INTEGER) IS 'Finalizes migration batch and calculates success metrics';
COMMENT ON FUNCTION migration_staging.compare_record_counts(INTEGER) IS 'Compares record counts between staging and target tables';
COMMENT ON FUNCTION migration_staging.validate_business_data_integrity(INTEGER) IS 'Validates critical business data integrity after migration';
COMMENT ON FUNCTION migration_staging.create_rollback_checkpoint(INTEGER, TEXT) IS 'Creates rollback checkpoint for migration recovery';
COMMENT ON FUNCTION migration_staging.execute_rollback(INTEGER, TEXT) IS 'Executes migration rollback (partial or full)';
COMMENT ON FUNCTION migration_staging.generate_migration_report(INTEGER) IS 'Generates comprehensive migration summary report';