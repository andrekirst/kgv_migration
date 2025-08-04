-- =============================================================================
-- KGV Migration: Monitoring and Alerting Configuration
-- Version: 1.0
-- Description: Comprehensive monitoring and alerting system for KGV database
-- =============================================================================

-- Create schema for monitoring
CREATE SCHEMA IF NOT EXISTS monitoring;

-- =============================================================================
-- MONITORING TABLES
-- =============================================================================

-- Table to store monitoring metrics
CREATE TABLE monitoring.metrics (
    id BIGSERIAL PRIMARY KEY,
    metric_name VARCHAR(100) NOT NULL,
    metric_category VARCHAR(50) NOT NULL, -- 'PERFORMANCE', 'AVAILABILITY', 'CAPACITY', 'BUSINESS'
    metric_value NUMERIC NOT NULL,
    metric_unit VARCHAR(20), -- 'count', 'seconds', 'bytes', 'percent', etc.
    tags JSONB, -- Additional metadata
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT metrics_category_check 
        CHECK (metric_category IN ('PERFORMANCE', 'AVAILABILITY', 'CAPACITY', 'BUSINESS', 'SYSTEM'))
);

-- Table for alert rules
CREATE TABLE monitoring.alert_rules (
    id BIGSERIAL PRIMARY KEY,
    rule_name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    metric_name VARCHAR(100) NOT NULL,
    condition_operator VARCHAR(10) NOT NULL, -- '>', '<', '>=', '<=', '=', '!='
    threshold_value NUMERIC NOT NULL,
    severity VARCHAR(10) NOT NULL DEFAULT 'WARNING', -- 'CRITICAL', 'WARNING', 'INFO'
    evaluation_window INTERVAL NOT NULL DEFAULT '5 minutes',
    notification_channels TEXT[], -- email addresses, webhook URLs, etc.
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT alert_rules_operator_check 
        CHECK (condition_operator IN ('>', '<', '>=', '<=', '=', '!=')),
    CONSTRAINT alert_rules_severity_check 
        CHECK (severity IN ('CRITICAL', 'WARNING', 'INFO'))
);

-- Table for alert history
CREATE TABLE monitoring.alert_history (
    id BIGSERIAL PRIMARY KEY,
    rule_id BIGINT NOT NULL,
    alert_state VARCHAR(20) NOT NULL, -- 'TRIGGERED', 'RESOLVED', 'ACKNOWLEDGED'
    metric_value NUMERIC NOT NULL,
    threshold_value NUMERIC NOT NULL,
    alert_message TEXT,
    notification_sent BOOLEAN NOT NULL DEFAULT false,
    triggered_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    resolved_at TIMESTAMP WITH TIME ZONE,
    acknowledged_at TIMESTAMP WITH TIME ZONE,
    acknowledged_by VARCHAR(100),
    
    CONSTRAINT fk_alert_history_rule 
        FOREIGN KEY (rule_id) REFERENCES monitoring.alert_rules(id) 
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT alert_history_state_check 
        CHECK (alert_state IN ('TRIGGERED', 'RESOLVED', 'ACKNOWLEDGED'))
);

-- Indexes for monitoring tables
CREATE INDEX idx_metrics_name_timestamp ON monitoring.metrics(metric_name, timestamp DESC);
CREATE INDEX idx_metrics_category ON monitoring.metrics(metric_category);
CREATE INDEX idx_metrics_timestamp ON monitoring.metrics(timestamp DESC);
CREATE INDEX idx_alert_rules_active ON monitoring.alert_rules(is_active) WHERE is_active = true;
CREATE INDEX idx_alert_history_rule_triggered ON monitoring.alert_history(rule_id, triggered_at DESC);
CREATE INDEX idx_alert_history_state ON monitoring.alert_history(alert_state);

-- Trigger for updated_at
CREATE TRIGGER trigger_alert_rules_updated_at 
    BEFORE UPDATE ON monitoring.alert_rules 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================================================
-- METRIC COLLECTION FUNCTIONS
-- =============================================================================

-- Function to collect database performance metrics
CREATE OR REPLACE FUNCTION monitoring.collect_database_metrics()
RETURNS INTEGER AS $$
DECLARE
    v_metrics_collected INTEGER := 0;
    v_db_size BIGINT;
    v_connection_count INTEGER;
    v_active_queries INTEGER;
    v_cache_hit_ratio NUMERIC;
    v_table_stats RECORD;
BEGIN
    -- Database size
    SELECT pg_database_size(current_database()) INTO v_db_size;
    INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit, tags)
    VALUES ('database_size_bytes', 'CAPACITY', v_db_size, 'bytes', '{"database": "' || current_database() || '"}'::JSONB);
    v_metrics_collected := v_metrics_collected + 1;
    
    -- Connection count
    SELECT COUNT(*) INTO v_connection_count FROM pg_stat_activity WHERE datname = current_database();
    INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit, tags)
    VALUES ('active_connections', 'SYSTEM', v_connection_count, 'count', '{"database": "' || current_database() || '"}'::JSONB);
    v_metrics_collected := v_metrics_collected + 1;
    
    -- Active queries
    SELECT COUNT(*) INTO v_active_queries FROM pg_stat_activity WHERE datname = current_database() AND state = 'active';
    INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit, tags)
    VALUES ('active_queries', 'PERFORMANCE', v_active_queries, 'count', '{"database": "' || current_database() || '"}'::JSONB);
    v_metrics_collected := v_metrics_collected + 1;
    
    -- Cache hit ratio
    SELECT 
        CASE 
            WHEN heap_blks_read + heap_blks_hit = 0 THEN 100
            ELSE ROUND(100.0 * heap_blks_hit / (heap_blks_read + heap_blks_hit), 2)
        END INTO v_cache_hit_ratio
    FROM (
        SELECT SUM(heap_blks_read) as heap_blks_read, SUM(heap_blks_hit) as heap_blks_hit
        FROM pg_stat_user_tables
    ) cache_stats;
    
    INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit, tags)
    VALUES ('cache_hit_ratio', 'PERFORMANCE', v_cache_hit_ratio, 'percent', '{"database": "' || current_database() || '"}'::JSONB);
    v_metrics_collected := v_metrics_collected + 1;
    
    -- Table-specific metrics for key tables
    FOR v_table_stats IN 
        SELECT 
            schemaname, 
            tablename,
            n_tup_ins + n_tup_upd + n_tup_del as total_modifications,
            n_tup_ins as inserts,
            n_tup_upd as updates,
            n_tup_del as deletes,
            seq_scan,
            idx_scan,
            n_live_tup as live_tuples
        FROM pg_stat_user_tables 
        WHERE tablename IN ('applications', 'application_history', 'districts', 'users')
    LOOP
        -- Table modification activity
        INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit, tags)
        VALUES (
            'table_modifications_total', 
            'SYSTEM', 
            v_table_stats.total_modifications, 
            'count',
            jsonb_build_object('table', v_table_stats.tablename, 'schema', v_table_stats.schemaname)
        );
        
        -- Index usage ratio
        INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit, tags)
        VALUES (
            'table_index_usage_ratio', 
            'PERFORMANCE', 
            CASE 
                WHEN v_table_stats.seq_scan + v_table_stats.idx_scan = 0 THEN 0 
                ELSE ROUND(100.0 * v_table_stats.idx_scan / (v_table_stats.seq_scan + v_table_stats.idx_scan), 2)
            END, 
            'percent',
            jsonb_build_object('table', v_table_stats.tablename, 'schema', v_table_stats.schemaname)
        );
        
        -- Live tuples count
        INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit, tags)
        VALUES (
            'table_live_tuples', 
            'CAPACITY', 
            v_table_stats.live_tuples, 
            'count',
            jsonb_build_object('table', v_table_stats.tablename, 'schema', v_table_stats.schemaname)
        );
        v_metrics_collected := v_metrics_collected + 3;
    END LOOP;
    
    RETURN v_metrics_collected;
END;
$$ LANGUAGE plpgsql;

-- Function to collect business metrics
CREATE OR REPLACE FUNCTION monitoring.collect_business_metrics()
RETURNS INTEGER AS $$
DECLARE
    v_metrics_collected INTEGER := 0;
    v_active_applications INTEGER;
    v_waiting_list_32_count INTEGER;
    v_waiting_list_33_count INTEGER;
    v_applications_today INTEGER;
    v_applications_this_month INTEGER;
    v_avg_processing_days NUMERIC;
BEGIN
    -- Active applications count
    SELECT COUNT(*) INTO v_active_applications FROM applications WHERE is_active = true;
    INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit)
    VALUES ('active_applications_total', 'BUSINESS', v_active_applications, 'count');
    v_metrics_collected := v_metrics_collected + 1;
    
    -- Waiting list counts
    SELECT COUNT(*) INTO v_waiting_list_32_count FROM applications 
    WHERE is_active = true AND waiting_list_number_32 IS NOT NULL;
    INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit)
    VALUES ('waiting_list_32_count', 'BUSINESS', v_waiting_list_32_count, 'count');
    
    SELECT COUNT(*) INTO v_waiting_list_33_count FROM applications 
    WHERE is_active = true AND waiting_list_number_33 IS NOT NULL;
    INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit)
    VALUES ('waiting_list_33_count', 'BUSINESS', v_waiting_list_33_count, 'count');
    v_metrics_collected := v_metrics_collected + 2;
    
    -- Daily applications
    SELECT COUNT(*) INTO v_applications_today FROM applications 
    WHERE application_date = CURRENT_DATE;
    INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit)
    VALUES ('applications_today', 'BUSINESS', v_applications_today, 'count');
    
    -- Monthly applications
    SELECT COUNT(*) INTO v_applications_this_month FROM applications 
    WHERE application_date >= DATE_TRUNC('month', CURRENT_DATE);
    INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit)
    VALUES ('applications_this_month', 'BUSINESS', v_applications_this_month, 'count');
    v_metrics_collected := v_metrics_collected + 2;
    
    -- Average processing time
    SELECT 
        COALESCE(AVG(EXTRACT(DAYS FROM (confirmation_date - application_date))), 0)
    INTO v_avg_processing_days
    FROM applications 
    WHERE confirmation_date IS NOT NULL 
      AND application_date IS NOT NULL
      AND confirmation_date >= NOW() - INTERVAL '30 days';
    
    INSERT INTO monitoring.metrics (metric_name, metric_category, metric_value, metric_unit)
    VALUES ('avg_processing_time_days', 'BUSINESS', v_avg_processing_days, 'days');
    v_metrics_collected := v_metrics_collected + 1;
    
    RETURN v_metrics_collected;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- ALERT MANAGEMENT FUNCTIONS
-- =============================================================================

-- Function to evaluate alert rules
CREATE OR REPLACE FUNCTION monitoring.evaluate_alert_rules()
RETURNS INTEGER AS $$
DECLARE
    v_rule RECORD;
    v_current_value NUMERIC;
    v_alerts_triggered INTEGER := 0;
    v_alert_condition BOOLEAN;
    v_existing_alert_id BIGINT;
BEGIN
    -- Evaluate each active alert rule
    FOR v_rule IN 
        SELECT * FROM monitoring.alert_rules WHERE is_active = true
    LOOP
        -- Get current metric value within evaluation window
        SELECT metric_value INTO v_current_value
        FROM monitoring.metrics
        WHERE metric_name = v_rule.metric_name
          AND timestamp >= NOW() - v_rule.evaluation_window
        ORDER BY timestamp DESC
        LIMIT 1;
        
        -- Skip if no recent data
        CONTINUE WHEN v_current_value IS NULL;
        
        -- Evaluate condition
        v_alert_condition := CASE v_rule.condition_operator
            WHEN '>' THEN v_current_value > v_rule.threshold_value
            WHEN '<' THEN v_current_value < v_rule.threshold_value
            WHEN '>=' THEN v_current_value >= v_rule.threshold_value
            WHEN '<=' THEN v_current_value <= v_rule.threshold_value
            WHEN '=' THEN v_current_value = v_rule.threshold_value
            WHEN '!=' THEN v_current_value != v_rule.threshold_value
            ELSE false
        END;
        
        -- Check for existing unresolved alert
        SELECT id INTO v_existing_alert_id
        FROM monitoring.alert_history
        WHERE rule_id = v_rule.id
          AND alert_state = 'TRIGGERED'
          AND resolved_at IS NULL
        ORDER BY triggered_at DESC
        LIMIT 1;
        
        IF v_alert_condition THEN
            -- Trigger new alert if none exists
            IF v_existing_alert_id IS NULL THEN
                INSERT INTO monitoring.alert_history (
                    rule_id, alert_state, metric_value, threshold_value, alert_message
                ) VALUES (
                    v_rule.id,
                    'TRIGGERED',
                    v_current_value,
                    v_rule.threshold_value,
                    format('Alert: %s %s %s (current: %s)', 
                           v_rule.metric_name, 
                           v_rule.condition_operator, 
                           v_rule.threshold_value,
                           v_current_value)
                );
                v_alerts_triggered := v_alerts_triggered + 1;
            END IF;
        ELSE
            -- Resolve existing alert if condition no longer met
            IF v_existing_alert_id IS NOT NULL THEN
                UPDATE monitoring.alert_history
                SET alert_state = 'RESOLVED',
                    resolved_at = NOW()
                WHERE id = v_existing_alert_id;
            END IF;
        END IF;
    END LOOP;
    
    RETURN v_alerts_triggered;
END;
$$ LANGUAGE plpgsql;

-- Function to acknowledge alert
CREATE OR REPLACE FUNCTION monitoring.acknowledge_alert(
    p_alert_id BIGINT,
    p_acknowledged_by VARCHAR(100)
) RETURNS BOOLEAN AS $$
BEGIN
    UPDATE monitoring.alert_history
    SET alert_state = 'ACKNOWLEDGED',
        acknowledged_at = NOW(),
        acknowledged_by = p_acknowledged_by
    WHERE id = p_alert_id
      AND alert_state = 'TRIGGERED';
    
    RETURN FOUND;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- MONITORING VIEWS
-- =============================================================================

-- View for current system health
CREATE OR REPLACE VIEW monitoring.system_health AS
WITH latest_metrics AS (
    SELECT DISTINCT ON (metric_name)
        metric_name,
        metric_value,
        metric_unit,
        timestamp,
        tags
    FROM monitoring.metrics
    WHERE timestamp >= NOW() - INTERVAL '1 hour'
    ORDER BY metric_name, timestamp DESC
)
SELECT 
    'Database Size' as health_check,
    pg_size_pretty(metric_value::BIGINT) as current_value,
    CASE 
        WHEN metric_value > 10 * 1024^3 THEN 'WARNING'  -- > 10GB
        WHEN metric_value > 20 * 1024^3 THEN 'CRITICAL' -- > 20GB
        ELSE 'OK'
    END as status
FROM latest_metrics WHERE metric_name = 'database_size_bytes'

UNION ALL

SELECT 
    'Active Connections',
    metric_value::TEXT,
    CASE 
        WHEN metric_value > 80 THEN 'WARNING'
        WHEN metric_value > 100 THEN 'CRITICAL'
        ELSE 'OK'
    END
FROM latest_metrics WHERE metric_name = 'active_connections'

UNION ALL

SELECT 
    'Cache Hit Ratio',
    metric_value::TEXT || '%',
    CASE 
        WHEN metric_value < 95 THEN 'WARNING'
        WHEN metric_value < 90 THEN 'CRITICAL'
        ELSE 'OK'
    END
FROM latest_metrics WHERE metric_name = 'cache_hit_ratio'

UNION ALL

SELECT 
    'Active Applications',
    metric_value::TEXT,
    'INFO'
FROM latest_metrics WHERE metric_name = 'active_applications_total';

-- View for active alerts
CREATE OR REPLACE VIEW monitoring.active_alerts AS
SELECT 
    ar.rule_name,
    ar.severity,
    ah.alert_message,
    ah.metric_value,
    ah.threshold_value,
    ah.triggered_at,
    EXTRACT(EPOCH FROM (NOW() - ah.triggered_at))/60 as minutes_since_triggered
FROM monitoring.alert_history ah
JOIN monitoring.alert_rules ar ON ah.rule_id = ar.id
WHERE ah.alert_state = 'TRIGGERED'
  AND ah.resolved_at IS NULL
ORDER BY ah.triggered_at DESC;

-- View for metric trends
CREATE OR REPLACE VIEW monitoring.metric_trends AS
SELECT 
    metric_name,
    metric_category,
    DATE_TRUNC('hour', timestamp) as hour,
    AVG(metric_value) as avg_value,
    MIN(metric_value) as min_value,
    MAX(metric_value) as max_value,
    COUNT(*) as sample_count
FROM monitoring.metrics
WHERE timestamp >= NOW() - INTERVAL '24 hours'
GROUP BY metric_name, metric_category, DATE_TRUNC('hour', timestamp)
ORDER BY metric_name, hour DESC;

-- =============================================================================
-- STANDARD ALERT RULES
-- =============================================================================

-- Insert standard alert rules
INSERT INTO monitoring.alert_rules (rule_name, description, metric_name, condition_operator, threshold_value, severity, evaluation_window, notification_channels) VALUES

-- Database performance alerts
('high_active_connections', 'Too many active database connections', 'active_connections', '>', 80, 'WARNING', '5 minutes', ARRAY['admin@kgv.local']),
('critical_active_connections', 'Critical number of database connections', 'active_connections', '>', 100, 'CRITICAL', '2 minutes', ARRAY['admin@kgv.local', 'oncall@kgv.local']),
('low_cache_hit_ratio', 'Database cache hit ratio is low', 'cache_hit_ratio', '<', 95, 'WARNING', '10 minutes', ARRAY['admin@kgv.local']),
('critical_cache_hit_ratio', 'Database cache hit ratio is critically low', 'cache_hit_ratio', '<', 90, 'CRITICAL', '5 minutes', ARRAY['admin@kgv.local', 'oncall@kgv.local']),

-- Capacity alerts
('database_size_warning', 'Database size is growing large', 'database_size_bytes', '>', 10737418240, 'WARNING', '1 hour', ARRAY['admin@kgv.local']), -- 10GB
('database_size_critical', 'Database size is critically large', 'database_size_bytes', '>', 21474836480, 'CRITICAL', '30 minutes', ARRAY['admin@kgv.local', 'oncall@kgv.local']), -- 20GB

-- Business process alerts
('no_applications_today', 'No applications received today', 'applications_today', '=', 0, 'WARNING', '4 hours', ARRAY['business@kgv.local']),
('high_processing_time', 'Application processing time is high', 'avg_processing_time_days', '>', 14, 'WARNING', '1 hour', ARRAY['business@kgv.local']),
('very_high_processing_time', 'Application processing time is very high', 'avg_processing_time_days', '>', 30, 'CRITICAL', '30 minutes', ARRAY['business@kgv.local', 'management@kgv.local'])

ON CONFLICT (rule_name) DO UPDATE SET
    description = EXCLUDED.description,
    metric_name = EXCLUDED.metric_name,
    condition_operator = EXCLUDED.condition_operator,
    threshold_value = EXCLUDED.threshold_value,
    severity = EXCLUDED.severity,
    evaluation_window = EXCLUDED.evaluation_window,
    notification_channels = EXCLUDED.notification_channels,
    updated_at = NOW();

-- =============================================================================
-- MONITORING AUTOMATION
-- =============================================================================

-- Function for complete monitoring cycle
CREATE OR REPLACE FUNCTION monitoring.run_monitoring_cycle()
RETURNS TABLE(
    step TEXT,
    result INTEGER,
    duration_ms INTEGER,
    status TEXT
) AS $$
DECLARE
    v_start_time TIMESTAMP;
    v_end_time TIMESTAMP;
    v_db_metrics INTEGER;
    v_business_metrics INTEGER;
    v_alerts_triggered INTEGER;
BEGIN
    -- Collect database metrics
    v_start_time := clock_timestamp();
    v_db_metrics := monitoring.collect_database_metrics();
    v_end_time := clock_timestamp();
    RETURN QUERY SELECT 'collect_database_metrics'::TEXT, v_db_metrics, 
                        EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time))::INTEGER, 'SUCCESS'::TEXT;
    
    -- Collect business metrics
    v_start_time := clock_timestamp();
    v_business_metrics := monitoring.collect_business_metrics();
    v_end_time := clock_timestamp();
    RETURN QUERY SELECT 'collect_business_metrics', v_business_metrics, 
                        EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time))::INTEGER, 'SUCCESS';
    
    -- Evaluate alerts
    v_start_time := clock_timestamp();
    v_alerts_triggered := monitoring.evaluate_alert_rules();
    v_end_time := clock_timestamp();
    RETURN QUERY SELECT 'evaluate_alert_rules', v_alerts_triggered, 
                        EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time))::INTEGER, 'SUCCESS';
END;
$$ LANGUAGE plpgsql;

-- Function to cleanup old monitoring data
CREATE OR REPLACE FUNCTION monitoring.cleanup_old_data(
    p_retention_days INTEGER DEFAULT 30
) RETURNS INTEGER AS $$
DECLARE
    v_deleted_count INTEGER;
BEGIN
    -- Delete old metrics
    DELETE FROM monitoring.metrics 
    WHERE timestamp < NOW() - (p_retention_days || ' days')::INTERVAL;
    GET DIAGNOSTICS v_deleted_count = ROW_COUNT;
    
    -- Delete old resolved alerts
    DELETE FROM monitoring.alert_history
    WHERE resolved_at IS NOT NULL 
      AND resolved_at < NOW() - (p_retention_days || ' days')::INTERVAL;
    
    RETURN v_deleted_count;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- HEALTH CHECK FUNCTIONS
-- =============================================================================

-- Function for application health check endpoint
CREATE OR REPLACE FUNCTION monitoring.health_check()
RETURNS TABLE(
    component TEXT,
    status TEXT,
    details TEXT,
    last_check TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    -- Database connectivity
    SELECT 
        'database' as component,
        'healthy' as status,
        'Database connection successful' as details,
        NOW() as last_check
    
    UNION ALL
    
    -- Recent data activity
    SELECT 
        'data_activity',
        CASE 
            WHEN MAX(created_at) >= NOW() - INTERVAL '24 hours' THEN 'healthy'
            WHEN MAX(created_at) >= NOW() - INTERVAL '7 days' THEN 'degraded'
            ELSE 'unhealthy'
        END,
        'Last data activity: ' || COALESCE(MAX(created_at)::TEXT, 'never'),
        NOW()
    FROM applications
    
    UNION ALL
    
    -- Critical table counts
    SELECT 
        'data_integrity',
        CASE 
            WHEN COUNT(*) > 0 THEN 'healthy'
            ELSE 'unhealthy'
        END,
        'Active applications: ' || COUNT(*),
        NOW()
    FROM applications WHERE is_active = true
    
    UNION ALL
    
    -- System resources
    SELECT 
        'system_resources',
        CASE 
            WHEN pg_database_size(current_database()) < 21474836480 THEN 'healthy' -- < 20GB
            ELSE 'degraded'
        END,
        'Database size: ' || pg_size_pretty(pg_database_size(current_database())),
        NOW();
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- COMMENTS
-- =============================================================================

COMMENT ON SCHEMA monitoring IS 'Comprehensive monitoring and alerting system';
COMMENT ON TABLE monitoring.metrics IS 'Stores time-series monitoring metrics';
COMMENT ON TABLE monitoring.alert_rules IS 'Defines alert rules and thresholds';
COMMENT ON TABLE monitoring.alert_history IS 'Tracks alert state changes and notifications';
COMMENT ON FUNCTION monitoring.collect_database_metrics() IS 'Collects database performance and system metrics';
COMMENT ON FUNCTION monitoring.collect_business_metrics() IS 'Collects business-specific KPI metrics';  
COMMENT ON FUNCTION monitoring.evaluate_alert_rules() IS 'Evaluates all active alert rules and triggers alerts';
COMMENT ON FUNCTION monitoring.run_monitoring_cycle() IS 'Executes complete monitoring cycle';
COMMENT ON FUNCTION monitoring.health_check() IS 'Provides application health status for load balancers';
COMMENT ON VIEW monitoring.system_health IS 'Current system health overview';
COMMENT ON VIEW monitoring.active_alerts IS 'Currently active/unresolved alerts';
COMMENT ON VIEW monitoring.metric_trends IS 'Hourly metric trends for the last 24 hours';