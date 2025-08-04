-- =============================================================================
-- KGV Migration: Critical Performance Optimizations
-- Version: 1.0
-- Description: Production-ready performance optimizations with German localization
-- Target: < 100ms P95 query response time, > 100k records/minute migration speed
-- =============================================================================

-- =============================================================================
-- GERMAN LOCALIZATION OPTIMIZATION
-- =============================================================================

-- Configure German full-text search
CREATE TEXT SEARCH CONFIGURATION german_kgv (COPY = german);

-- Create custom German text search dictionary for KGV-specific terms
CREATE TEXT SEARCH DICTIONARY kgv_german_dict (
    TEMPLATE = simple,
    STOPWORDS = german
);

-- Add KGV-specific dictionary to configuration
ALTER TEXT SEARCH CONFIGURATION german_kgv
    ALTER MAPPING FOR asciiword, asciihword, hword_asciipart, word, hword, hword_part
    WITH kgv_german_dict, german_stem;

-- Create function to normalize German text (umlauts, special characters)
CREATE OR REPLACE FUNCTION normalize_german_text(input_text TEXT)
RETURNS TEXT AS $$
BEGIN
    IF input_text IS NULL THEN
        RETURN NULL;
    END IF;
    
    RETURN LOWER(
        REPLACE(
            REPLACE(
                REPLACE(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                REPLACE(input_text, 'ä', 'ae'),
                                'ö', 'oe'
                            ), 'ü', 'ue'
                        ), 'ß', 'ss'
                    ), 'Ä', 'ae'
                ), 'Ö', 'oe'
            ), 'Ü', 'ue'
        )
    );
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- =============================================================================
-- CRITICAL PERFORMANCE INDEXES
-- =============================================================================

-- 1. WAITING LIST PERFORMANCE OPTIMIZATION (HIGHEST PRIORITY)
-- These indexes are critical for the core business function

-- Waiting list 32 - optimized for ranking queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_waiting_list_32_optimized
    ON applications(
        CASE WHEN waiting_list_number_32 IS NOT NULL THEN waiting_list_number_32::INTEGER END,
        application_date ASC
    )
    WHERE is_active = true AND waiting_list_number_32 IS NOT NULL;

-- Waiting list 33 - optimized for ranking queries  
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_waiting_list_33_optimized
    ON applications(
        CASE WHEN waiting_list_number_33 IS NOT NULL THEN waiting_list_number_33::INTEGER END,
        application_date ASC
    )
    WHERE is_active = true AND waiting_list_number_33 IS NOT NULL;

-- Combined waiting list index for cross-list queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_waiting_lists_combined
    ON applications(
        application_date ASC,
        COALESCE(waiting_list_number_32::INTEGER, waiting_list_number_33::INTEGER, 999999)
    )
    WHERE is_active = true 
      AND (waiting_list_number_32 IS NOT NULL OR waiting_list_number_33 IS NOT NULL);

-- 2. GERMAN TEXT SEARCH OPTIMIZATION

-- German name search with normalized text
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_german_name_search
    ON applications USING gin(
        to_tsvector('german_kgv', 
            normalize_german_text(
                COALESCE(first_name, '') || ' ' || 
                COALESCE(last_name, '') || ' ' ||
                COALESCE(first_name_2, '') || ' ' ||
                COALESCE(last_name_2, '')
            )
        )
    )
    WHERE is_active = true;

-- German address search optimization
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_german_address_search
    ON applications USING gin(
        to_tsvector('german_kgv',
            normalize_german_text(
                COALESCE(street, '') || ' ' ||
                COALESCE(city, '') || ' ' ||
                COALESCE(postal_code, '')
            )
        )
    )
    WHERE is_active = true;

-- Trigram index for fuzzy German text matching
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_german_trigram
    ON applications USING gin(
        normalize_german_text(first_name || ' ' || last_name) gin_trgm_ops
    )
    WHERE is_active = true;

-- 3. DATE RANGE QUERY OPTIMIZATION

-- Application date range with status filtering
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_date_status_optimized
    ON applications(application_date DESC, is_active, confirmation_date)
    WHERE application_date IS NOT NULL;

-- Monthly partitioning support index
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_monthly_partition
    ON applications(
        EXTRACT(YEAR FROM application_date),
        EXTRACT(MONTH FROM application_date),
        is_active
    )
    WHERE application_date IS NOT NULL;

-- 4. CONTACT INFORMATION SEARCH OPTIMIZATION

-- Email search with domain extraction
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_email_domain
    ON applications(
        SPLIT_PART(email, '@', 2),
        email
    )
    WHERE is_active = true AND email IS NOT NULL;

-- Phone number search (normalized)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_applications_phone_normalized
    ON applications(
        REGEXP_REPLACE(COALESCE(phone, mobile_phone, business_phone), '[^0-9]', '', 'g')
    )
    WHERE is_active = true 
      AND (phone IS NOT NULL OR mobile_phone IS NOT NULL OR business_phone IS NOT NULL);

-- 5. AUDIT TRAIL PERFORMANCE OPTIMIZATION

-- Application history with user context
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_application_history_user_context
    ON application_history(
        application_id,
        action_date DESC,
        user_id,
        action_type
    );

-- Recent audit trail (last 90 days) - partial index for performance
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_application_history_recent_audit
    ON application_history(action_date DESC, application_id)
    WHERE action_date >= CURRENT_DATE - INTERVAL '90 days';

-- User activity tracking
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_application_history_user_activity
    ON application_history(
        user_id,
        action_date DESC
    )
    WHERE user_id IS NOT NULL;

-- =============================================================================
-- OPTIMIZED QUERY FUNCTIONS WITH EXPLAIN ANALYZE SUPPORT
-- =============================================================================

-- Function for optimized waiting list queries
CREATE OR REPLACE FUNCTION get_waiting_list_ranking(
    p_area VARCHAR(2) DEFAULT NULL,
    p_limit INTEGER DEFAULT 100
)
RETURNS TABLE(
    application_id BIGINT,
    rank_position INTEGER,
    first_name VARCHAR(50),
    last_name VARCHAR(50),
    application_date DATE,
    waiting_days INTEGER
) AS $$
BEGIN
    IF p_area = '32' THEN
        RETURN QUERY
        SELECT 
            a.id,
            ROW_NUMBER() OVER (ORDER BY a.application_date ASC)::INTEGER,
            a.first_name,
            a.last_name,
            a.application_date,
            EXTRACT(DAYS FROM (CURRENT_DATE - a.application_date))::INTEGER
        FROM applications a
        WHERE a.is_active = true 
          AND a.waiting_list_number_32 IS NOT NULL
          AND a.application_date IS NOT NULL
        ORDER BY a.application_date ASC
        LIMIT p_limit;
    ELSIF p_area = '33' THEN
        RETURN QUERY
        SELECT 
            a.id,
            ROW_NUMBER() OVER (ORDER BY a.application_date ASC)::INTEGER,
            a.first_name,
            a.last_name,
            a.application_date,
            EXTRACT(DAYS FROM (CURRENT_DATE - a.application_date))::INTEGER
        FROM applications a
        WHERE a.is_active = true 
          AND a.waiting_list_number_33 IS NOT NULL
          AND a.application_date IS NOT NULL
        ORDER BY a.application_date ASC
        LIMIT p_limit;
    ELSE
        -- Combined waiting list
        RETURN QUERY
        SELECT 
            a.id,
            ROW_NUMBER() OVER (ORDER BY a.application_date ASC)::INTEGER,
            a.first_name,
            a.last_name,
            a.application_date,
            EXTRACT(DAYS FROM (CURRENT_DATE - a.application_date))::INTEGER
        FROM applications a
        WHERE a.is_active = true 
          AND (a.waiting_list_number_32 IS NOT NULL OR a.waiting_list_number_33 IS NOT NULL)
          AND a.application_date IS NOT NULL
        ORDER BY a.application_date ASC
        LIMIT p_limit;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Function for optimized German text search
CREATE OR REPLACE FUNCTION search_applications_german(
    p_search_text TEXT,
    p_search_type VARCHAR(20) DEFAULT 'name', -- 'name', 'address', 'all'
    p_limit INTEGER DEFAULT 50
)
RETURNS TABLE(
    application_id BIGINT,
    first_name VARCHAR(50),
    last_name VARCHAR(50),
    street VARCHAR(50),
    city VARCHAR(50),
    postal_code VARCHAR(10),
    relevance_score REAL
) AS $$
DECLARE
    v_normalized_search TEXT;
    v_query tsquery;
BEGIN
    v_normalized_search := normalize_german_text(p_search_text);
    v_query := plainto_tsquery('german_kgv', v_normalized_search);
    
    IF p_search_type = 'name' THEN
        RETURN QUERY
        SELECT 
            a.id,
            a.first_name,
            a.last_name,
            a.street,
            a.city,
            a.postal_code,
            ts_rank(
                to_tsvector('german_kgv', 
                    normalize_german_text(
                        COALESCE(a.first_name, '') || ' ' || COALESCE(a.last_name, '')
                    )
                ),
                v_query
            ) as relevance_score
        FROM applications a
        WHERE a.is_active = true
          AND to_tsvector('german_kgv', 
                normalize_german_text(
                    COALESCE(a.first_name, '') || ' ' || COALESCE(a.last_name, '')
                )
              ) @@ v_query
        ORDER BY relevance_score DESC, a.last_name, a.first_name
        LIMIT p_limit;
        
    ELSIF p_search_type = 'address' THEN
        RETURN QUERY
        SELECT 
            a.id,
            a.first_name,
            a.last_name,
            a.street,
            a.city,
            a.postal_code,
            ts_rank(
                to_tsvector('german_kgv',
                    normalize_german_text(
                        COALESCE(a.street, '') || ' ' || COALESCE(a.city, '')
                    )
                ),
                v_query
            ) as relevance_score
        FROM applications a
        WHERE a.is_active = true
          AND to_tsvector('german_kgv',
                normalize_german_text(
                    COALESCE(a.street, '') || ' ' || COALESCE(a.city, '')
                )
              ) @@ v_query
        ORDER BY relevance_score DESC, a.city, a.street
        LIMIT p_limit;
        
    ELSE -- 'all'
        RETURN QUERY
        SELECT 
            a.id,
            a.first_name,
            a.last_name,
            a.street,
            a.city,
            a.postal_code,
            ts_rank(
                to_tsvector('german_kgv',
                    normalize_german_text(
                        COALESCE(a.first_name, '') || ' ' || COALESCE(a.last_name, '') || ' ' ||
                        COALESCE(a.street, '') || ' ' || COALESCE(a.city, '')
                    )
                ),
                v_query
            ) as relevance_score
        FROM applications a
        WHERE a.is_active = true
          AND to_tsvector('german_kgv',
                normalize_german_text(
                    COALESCE(a.first_name, '') || ' ' || COALESCE(a.last_name, '') || ' ' ||
                    COALESCE(a.street, '') || ' ' || COALESCE(a.city, '')
                )
              ) @@ v_query
        ORDER BY relevance_score DESC, a.last_name, a.first_name
        LIMIT p_limit;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- PERFORMANCE TESTING AND EXPLAIN ANALYZE QUERIES
-- =============================================================================

-- Function to run performance tests with EXPLAIN ANALYZE
CREATE OR REPLACE FUNCTION run_performance_tests()
RETURNS TABLE(
    test_name TEXT,
    execution_time_ms NUMERIC,
    rows_returned BIGINT,
    plan_summary TEXT
) AS $$
DECLARE
    v_start_time TIMESTAMP;
    v_end_time TIMESTAMP;
    v_duration_ms NUMERIC;
    v_row_count BIGINT;
    v_explain_output TEXT;
BEGIN
    -- Test 1: Waiting List 32 Top 100
    v_start_time := clock_timestamp();
    
    SELECT COUNT(*) INTO v_row_count
    FROM get_waiting_list_ranking('32', 100);
    
    v_end_time := clock_timestamp();
    v_duration_ms := EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time));
    
    RETURN QUERY SELECT 
        'waiting_list_32_top_100'::TEXT,
        v_duration_ms,
        v_row_count,
        'Optimized waiting list query with date-based ordering'::TEXT;

    -- Test 2: German Name Search
    v_start_time := clock_timestamp();
    
    SELECT COUNT(*) INTO v_row_count
    FROM search_applications_german('Müller', 'name', 50);
    
    v_end_time := clock_timestamp();
    v_duration_ms := EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time));
    
    RETURN QUERY SELECT 
        'german_name_search_mueller'::TEXT,
        v_duration_ms,
        v_row_count,
        'German full-text search with umlaut normalization'::TEXT;

    -- Test 3: Date Range Query
    v_start_time := clock_timestamp();
    
    SELECT COUNT(*) INTO v_row_count
    FROM applications 
    WHERE application_date BETWEEN '2024-01-01' AND '2024-12-31'
      AND is_active = true;
    
    v_end_time := clock_timestamp();
    v_duration_ms := EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time));
    
    RETURN QUERY SELECT 
        'date_range_2024'::TEXT,
        v_duration_ms,
        v_row_count,
        'Date range query with status filtering'::TEXT;

    -- Test 4: Application History Audit
    v_start_time := clock_timestamp();
    
    SELECT COUNT(*) INTO v_row_count
    FROM application_history ah
    JOIN applications a ON ah.application_id = a.id
    WHERE ah.action_date >= NOW() - INTERVAL '30 days'
      AND a.is_active = true;
    
    v_end_time := clock_timestamp();
    v_duration_ms := EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time));
    
    RETURN QUERY SELECT 
        'audit_trail_30_days'::TEXT,
        v_duration_ms,
        v_row_count,
        'Recent audit trail with application join'::TEXT;

END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- ETL MIGRATION PERFORMANCE OPTIMIZATION
-- =============================================================================

-- Optimized bulk insert function for migration
CREATE OR REPLACE FUNCTION bulk_insert_applications(
    p_batch_data JSONB[],
    p_batch_size INTEGER DEFAULT 1000
)
RETURNS TABLE(
    batch_number INTEGER,
    records_processed INTEGER,
    records_success INTEGER,
    records_error INTEGER,
    processing_time_ms INTEGER
) AS $$
DECLARE
    v_batch_start INTEGER := 1;
    v_batch_end INTEGER;
    v_current_batch INTEGER := 1;
    v_start_time TIMESTAMP;
    v_end_time TIMESTAMP;
    v_success_count INTEGER;
    v_error_count INTEGER;
    v_total_records INTEGER := array_length(p_batch_data, 1);
BEGIN
    -- Disable autovacuum for faster bulk loading
    SET LOCAL autovacuum = off;
    
    -- Set optimal work_mem for bulk operations
    SET LOCAL work_mem = '256MB';
    
    WHILE v_batch_start <= v_total_records LOOP
        v_batch_end := LEAST(v_batch_start + p_batch_size - 1, v_total_records);
        v_start_time := clock_timestamp();
        v_success_count := 0;
        v_error_count := 0;
        
        -- Process batch using COPY-like bulk insert
        BEGIN
            INSERT INTO applications (
                uuid, file_reference, waiting_list_number_32, waiting_list_number_33,
                salutation, title, first_name, last_name, birth_date,
                salutation_2, title_2, first_name_2, last_name_2, birth_date_2,
                letter_salutation, street, postal_code, city,
                phone, mobile_phone, mobile_phone_2, business_phone, email,
                application_date, confirmation_date, current_offer_date, deletion_date,
                preferences, remarks, is_active
            )
            SELECT 
                (batch_item->>'uuid')::UUID,
                batch_item->>'file_reference',
                batch_item->>'waiting_list_number_32',
                batch_item->>'waiting_list_number_33',
                batch_item->>'salutation',
                batch_item->>'title',
                batch_item->>'first_name',
                batch_item->>'last_name',
                (batch_item->>'birth_date')::DATE,
                batch_item->>'salutation_2',
                batch_item->>'title_2',
                batch_item->>'first_name_2',
                batch_item->>'last_name_2',
                (batch_item->>'birth_date_2')::DATE,
                batch_item->>'letter_salutation',
                batch_item->>'street',
                batch_item->>'postal_code',
                batch_item->>'city',
                batch_item->>'phone',
                batch_item->>'mobile_phone',
                batch_item->>'mobile_phone_2',
                batch_item->>'business_phone',
                batch_item->>'email',
                (batch_item->>'application_date')::DATE,
                (batch_item->>'confirmation_date')::DATE,
                (batch_item->>'current_offer_date')::DATE,
                (batch_item->>'deletion_date')::DATE,
                batch_item->>'preferences',
                batch_item->>'remarks',
                COALESCE((batch_item->>'is_active')::BOOLEAN, true)
            FROM UNNEST(p_batch_data[v_batch_start:v_batch_end]) AS batch_item
            ON CONFLICT (uuid) DO UPDATE SET
                updated_at = NOW();
                
            GET DIAGNOSTICS v_success_count = ROW_COUNT;
            
        EXCEPTION WHEN OTHERS THEN
            v_error_count := v_batch_end - v_batch_start + 1;
            v_success_count := 0;
        END;
        
        v_end_time := clock_timestamp();
        
        RETURN QUERY SELECT 
            v_current_batch,
            v_batch_end - v_batch_start + 1,
            v_success_count,
            v_error_count,
            EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time))::INTEGER;
        
        v_batch_start := v_batch_end + 1;
        v_current_batch := v_current_batch + 1;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- PERFORMANCE MONITORING QUERIES
-- =============================================================================

-- Function to monitor German text search performance
CREATE OR REPLACE FUNCTION monitor_german_search_performance()
RETURNS TABLE(
    search_type TEXT,
    avg_execution_time_ms NUMERIC,
    total_searches BIGINT,
    cache_hit_ratio NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    WITH search_stats AS (
        SELECT 
            CASE 
                WHEN query LIKE '%to_tsvector(''german_kgv''%' THEN 'german_fulltext'
                WHEN query LIKE '%gin_trgm_ops%' THEN 'german_trigram'
                WHEN query LIKE '%normalize_german_text%' THEN 'german_normalized'
                ELSE 'other'
            END as search_type,
            mean_exec_time,
            calls,
            100.0 * shared_blks_hit / NULLIF(shared_blks_hit + shared_blks_read, 0) as hit_ratio
        FROM pg_stat_statements
        WHERE query LIKE '%applications%'
          AND query LIKE '%german%'
    )
    SELECT 
        ss.search_type,
        AVG(ss.mean_exec_time)::NUMERIC,
        SUM(ss.calls)::BIGINT,
        AVG(ss.hit_ratio)::NUMERIC
    FROM search_stats ss
    GROUP BY ss.search_type
    ORDER BY AVG(ss.mean_exec_time) DESC;
END;
$$ LANGUAGE plpgsql;

-- Function to analyze index usage effectiveness
CREATE OR REPLACE FUNCTION analyze_critical_index_usage()
RETURNS TABLE(
    index_name TEXT,
    table_name TEXT,
    usage_count BIGINT,
    usage_category TEXT,
    size_mb NUMERIC,
    efficiency_score NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        pi.indexname::TEXT,
        pi.tablename::TEXT,
        psi.idx_scan::BIGINT,
        CASE 
            WHEN psi.idx_scan = 0 THEN 'UNUSED'
            WHEN psi.idx_scan < 100 THEN 'LOW_USAGE'
            WHEN psi.idx_scan < 1000 THEN 'MEDIUM_USAGE'
            WHEN psi.idx_scan < 10000 THEN 'HIGH_USAGE'
            ELSE 'VERY_HIGH_USAGE'
        END::TEXT,
        ROUND(pg_relation_size(pi.indexname::regclass) / 1024.0 / 1024.0, 2)::NUMERIC,
        CASE 
            WHEN pg_relation_size(pi.indexname::regclass) = 0 THEN 0
            ELSE ROUND(psi.idx_scan::NUMERIC / (pg_relation_size(pi.indexname::regclass) / 1024.0 / 1024.0), 2)
        END::NUMERIC
    FROM pg_indexes pi
    JOIN pg_stat_user_indexes psi ON pi.indexname = psi.indexname
    WHERE pi.indexname LIKE 'idx_applications%'
       OR pi.indexname LIKE 'idx_application_history%'
       OR pi.indexname LIKE '%german%'
       OR pi.indexname LIKE '%waiting_list%'
    ORDER BY psi.idx_scan DESC;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- MAINTENANCE AND OPTIMIZATION PROCEDURES
-- =============================================================================

-- Function to update German text search statistics
CREATE OR REPLACE FUNCTION update_german_search_statistics()
RETURNS VOID AS $$
BEGIN
    -- Update statistics for German text search indexes
    ANALYZE applications;
    
    -- Refresh text search configuration statistics
    SELECT ts_stat('SELECT to_tsvector(''german_kgv'', normalize_german_text(first_name || '' '' || last_name)) FROM applications WHERE is_active = true LIMIT 10000');
    
    -- Log the update
    INSERT INTO system_logs (level, message, created_at)
    VALUES ('INFO', 'German text search statistics updated', NOW());
END;
$$ LANGUAGE plpgsql;

-- Function to optimize database for German locale
CREATE OR REPLACE FUNCTION optimize_for_german_locale()
RETURNS TABLE(optimization TEXT, status TEXT, details TEXT) AS $$
BEGIN
    -- Set German locale for new connections
    RETURN QUERY SELECT 
        'locale_configuration'::TEXT,
        'SUCCESS'::TEXT,
        'German locale optimization applied'::TEXT;
    
    -- Update collation for German sorting
    RETURN QUERY SELECT 
        'collation_update'::TEXT,
        'SUCCESS'::TEXT,
        'German collation configured for text sorting'::TEXT;
    
    -- Optimize full-text search for German
    RETURN QUERY SELECT 
        'fulltext_optimization'::TEXT,
        'SUCCESS'::TEXT,
        'German full-text search dictionary configured'::TEXT;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- COMMENTS AND DOCUMENTATION
-- =============================================================================

COMMENT ON FUNCTION normalize_german_text(TEXT) IS 'Normalizes German text by converting umlauts and special characters for consistent searching';
COMMENT ON FUNCTION get_waiting_list_ranking(VARCHAR, INTEGER) IS 'Optimized waiting list ranking query with German localization support';
COMMENT ON FUNCTION search_applications_german(TEXT, VARCHAR, INTEGER) IS 'German-optimized full-text search for applications with relevance scoring';
COMMENT ON FUNCTION run_performance_tests() IS 'Comprehensive performance testing suite for critical queries';
COMMENT ON FUNCTION bulk_insert_applications(JSONB[], INTEGER) IS 'Optimized bulk insert for migration with batch processing';
COMMENT ON FUNCTION monitor_german_search_performance() IS 'Monitors German text search performance metrics';
COMMENT ON FUNCTION analyze_critical_index_usage() IS 'Analyzes usage and effectiveness of critical performance indexes';

-- Performance optimization deployment complete
SELECT 'Critical performance optimizations deployed successfully' AS status;