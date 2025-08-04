-- =============================================================================
-- KGV Migration: Data Quality Checks and Validation
-- Version: 1.0
-- Description: Comprehensive data quality validation framework
-- =============================================================================

-- Create schema for data quality management
CREATE SCHEMA IF NOT EXISTS data_quality;

-- =============================================================================
-- DATA QUALITY RULE DEFINITIONS
-- =============================================================================

-- Table to store data quality rules
CREATE TABLE data_quality.rules (
    id BIGSERIAL PRIMARY KEY,
    rule_name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT NOT NULL,
    table_name VARCHAR(50) NOT NULL,
    column_name VARCHAR(50),
    rule_type VARCHAR(20) NOT NULL, -- 'NOT_NULL', 'UNIQUE', 'RANGE', 'PATTERN', 'REFERENCE', 'CUSTOM'
    rule_query TEXT NOT NULL, -- SQL query that returns violations
    severity VARCHAR(10) NOT NULL DEFAULT 'ERROR', -- 'ERROR', 'WARNING', 'INFO'
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT rules_rule_type_check 
        CHECK (rule_type IN ('NOT_NULL', 'UNIQUE', 'RANGE', 'PATTERN', 'REFERENCE', 'CUSTOM')),
    CONSTRAINT rules_severity_check 
        CHECK (severity IN ('ERROR', 'WARNING', 'INFO'))
);

-- Table to store quality check results
CREATE TABLE data_quality.check_results (
    id BIGSERIAL PRIMARY KEY,
    batch_id INTEGER NOT NULL,
    rule_id BIGINT NOT NULL,
    check_timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    violations_count INTEGER NOT NULL DEFAULT 0,
    sample_violations JSONB, -- Sample of violating records
    execution_time_ms INTEGER,
    status VARCHAR(20) NOT NULL DEFAULT 'SUCCESS', -- 'SUCCESS', 'ERROR', 'SKIPPED'
    error_message TEXT,
    
    CONSTRAINT fk_check_results_rule 
        FOREIGN KEY (rule_id) REFERENCES data_quality.rules(id) 
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT check_results_status_check 
        CHECK (status IN ('SUCCESS', 'ERROR', 'SKIPPED'))
);

-- Indexes for performance
CREATE INDEX idx_rules_active ON data_quality.rules(is_active) WHERE is_active = true;
CREATE INDEX idx_rules_table ON data_quality.rules(table_name);
CREATE INDEX idx_check_results_batch ON data_quality.check_results(batch_id);
CREATE INDEX idx_check_results_rule ON data_quality.check_results(rule_id);
CREATE INDEX idx_check_results_timestamp ON data_quality.check_results(check_timestamp);

-- Trigger for updated_at
CREATE TRIGGER trigger_rules_updated_at 
    BEFORE UPDATE ON data_quality.rules 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================================================
-- CORE DATA QUALITY RULES
-- =============================================================================

-- Insert standard data quality rules
INSERT INTO data_quality.rules (rule_name, description, table_name, column_name, rule_type, rule_query, severity) VALUES

-- NOT NULL validations for critical fields
('districts_name_not_null', 'District name must not be null', 'districts', 'name', 'NOT_NULL',
 'SELECT id, name FROM districts WHERE name IS NULL OR TRIM(name) = ''''', 'ERROR'),

('applications_name_not_null', 'Application first and last name must not be null', 'applications', 'first_name,last_name', 'NOT_NULL',
 'SELECT id, first_name, last_name FROM applications WHERE first_name IS NULL OR last_name IS NULL OR TRIM(first_name) = '''' OR TRIM(last_name) = ''''', 'ERROR'),

('users_name_not_null', 'User first and last name must not be null', 'users', 'first_name,last_name', 'NOT_NULL',
 'SELECT id, first_name, last_name FROM users WHERE first_name IS NULL OR last_name IS NULL OR TRIM(first_name) = '''' OR TRIM(last_name) = ''''', 'ERROR'),

-- UNIQUE validations
('districts_name_unique', 'District names must be unique', 'districts', 'name', 'UNIQUE',
 'SELECT name, COUNT(*) as count FROM districts WHERE is_active = true GROUP BY name HAVING COUNT(*) > 1', 'ERROR'),

('file_references_unique', 'File references must be unique per district/year', 'file_references', 'district_code,number,year', 'UNIQUE',
 'SELECT district_code, number, year, COUNT(*) as count FROM file_references WHERE is_active = true GROUP BY district_code, number, year HAVING COUNT(*) > 1', 'ERROR'),

('entry_numbers_unique', 'Entry numbers must be unique per district/year', 'entry_numbers', 'district_code,number,year', 'UNIQUE',
 'SELECT district_code, number, year, COUNT(*) as count FROM entry_numbers WHERE is_active = true GROUP BY district_code, number, year HAVING COUNT(*) > 1', 'ERROR'),

('users_employee_number_unique', 'Employee numbers must be unique', 'users', 'employee_number', 'UNIQUE',
 'SELECT employee_number, COUNT(*) as count FROM users WHERE employee_number IS NOT NULL AND is_active = true GROUP BY employee_number HAVING COUNT(*) > 1', 'ERROR'),

-- RANGE validations
('file_references_year_range', 'File reference year must be within valid range', 'file_references', 'year', 'RANGE',
 'SELECT id, year FROM file_references WHERE year < 1900 OR year > EXTRACT(YEAR FROM NOW()) + 10', 'ERROR'),

('entry_numbers_year_range', 'Entry number year must be within valid range', 'entry_numbers', 'year', 'RANGE',
 'SELECT id, year FROM entry_numbers WHERE year < 1900 OR year > EXTRACT(YEAR FROM NOW()) + 10', 'ERROR'),

('file_references_number_positive', 'File reference number must be positive', 'file_references', 'number', 'RANGE',
 'SELECT id, number FROM file_references WHERE number <= 0', 'ERROR'),

('entry_numbers_number_positive', 'Entry number must be positive', 'entry_numbers', 'number', 'RANGE',
 'SELECT id, number FROM entry_numbers WHERE number <= 0', 'ERROR'),

-- PATTERN validations
('applications_email_format', 'Email addresses must be valid', 'applications', 'email', 'PATTERN',
 'SELECT id, email FROM applications WHERE email IS NOT NULL AND email !~* ''^[A-Za-z0-9._%-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$''', 'WARNING'),

('users_email_format', 'User email addresses must be valid', 'users', 'email', 'PATTERN',
 'SELECT id, email FROM users WHERE email IS NOT NULL AND email !~* ''^[A-Za-z0-9._%-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$''', 'WARNING'),

('applications_postal_code_format', 'Postal codes must be 5 digits', 'applications', 'postal_code', 'PATTERN',
 'SELECT id, postal_code FROM applications WHERE postal_code IS NOT NULL AND postal_code !~ ''^[0-9]{5}$''', 'WARNING'),

-- REFERENCE validations
('cadastral_districts_district_reference', 'Cadastral districts must reference valid districts', 'cadastral_districts', 'district_id', 'REFERENCE',
 'SELECT cd.id, cd.district_id FROM cadastral_districts cd LEFT JOIN districts d ON cd.district_id = d.id WHERE d.id IS NULL', 'ERROR'),

('application_history_application_reference', 'Application history must reference valid applications', 'application_history', 'application_id', 'REFERENCE',
 'SELECT ah.id, ah.application_id FROM application_history ah LEFT JOIN applications a ON ah.application_id = a.id WHERE a.id IS NULL', 'ERROR'),

('application_history_user_reference', 'Application history user references must be valid', 'application_history', 'user_id', 'REFERENCE',
 'SELECT ah.id, ah.user_id FROM application_history ah LEFT JOIN users u ON ah.user_id = u.id WHERE ah.user_id IS NOT NULL AND u.id IS NULL', 'WARNING'),

-- CUSTOM business logic validations
('applications_date_consistency', 'Application confirmation date must be after application date', 'applications', 'application_date,confirmation_date', 'CUSTOM',
 'SELECT id, application_date, confirmation_date FROM applications WHERE application_date IS NOT NULL AND confirmation_date IS NOT NULL AND confirmation_date < application_date', 'WARNING'),

('applications_active_status_consistency', 'Active applications should not have deletion date', 'applications', 'is_active,deletion_date', 'CUSTOM',
 'SELECT id, is_active, deletion_date FROM applications WHERE is_active = true AND deletion_date IS NOT NULL', 'WARNING'),

('applications_waiting_list_format', 'Waiting list numbers should be numeric when present', 'applications', 'waiting_list_number_32,waiting_list_number_33', 'CUSTOM',
 'SELECT id, waiting_list_number_32, waiting_list_number_33 FROM applications WHERE (waiting_list_number_32 IS NOT NULL AND waiting_list_number_32 !~ ''^[0-9]+$'') OR (waiting_list_number_33 IS NOT NULL AND waiting_list_number_33 !~ ''^[0-9]+$'')', 'WARNING'),

('application_history_action_type_valid', 'Application history action types must be valid', 'application_history', 'action_type', 'CUSTOM',
 'SELECT id, action_type FROM application_history WHERE action_type NOT IN (''CREA'', ''UPD'', ''DEL'', ''OFFT'', ''ACPT'', ''REJT'', ''WAIT'')', 'WARNING'),

-- Data completeness checks
('applications_contact_info_completeness', 'Applications should have at least one contact method', 'applications', 'phone,mobile_phone,email', 'CUSTOM',
 'SELECT id, first_name, last_name FROM applications WHERE is_active = true AND (phone IS NULL OR TRIM(phone) = '''') AND (mobile_phone IS NULL OR TRIM(mobile_phone) = '''') AND (email IS NULL OR TRIM(email) = '''')', 'WARNING'),

('applications_address_completeness', 'Applications should have complete address information', 'applications', 'street,postal_code,city', 'CUSTOM',
 'SELECT id, first_name, last_name, street, postal_code, city FROM applications WHERE is_active = true AND (street IS NULL OR postal_code IS NULL OR city IS NULL)', 'WARNING')

ON CONFLICT (rule_name) DO UPDATE SET
    description = EXCLUDED.description,
    table_name = EXCLUDED.table_name,
    column_name = EXCLUDED.column_name,
    rule_type = EXCLUDED.rule_type,
    rule_query = EXCLUDED.rule_query,
    severity = EXCLUDED.severity,
    updated_at = NOW();

-- =============================================================================
-- DATA QUALITY CHECK EXECUTION FUNCTIONS
-- =============================================================================

-- Function to execute a single data quality rule
CREATE OR REPLACE FUNCTION data_quality.execute_rule(
    p_rule_id BIGINT,
    p_batch_id INTEGER
) RETURNS BIGINT AS $$
DECLARE
    v_rule RECORD;
    v_start_time TIMESTAMP;
    v_end_time TIMESTAMP;
    v_violations_count INTEGER := 0;
    v_sample_violations JSONB := '[]'::JSONB;
    v_result_id BIGINT;
    v_error_message TEXT;
BEGIN
    -- Get rule details
    SELECT * INTO v_rule FROM data_quality.rules WHERE id = p_rule_id AND is_active = true;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Rule with ID % not found or inactive', p_rule_id;
    END IF;
    
    v_start_time := clock_timestamp();
    
    BEGIN
        -- Execute the rule query to count violations
        EXECUTE FORMAT('SELECT COUNT(*) FROM (%s) violations', v_rule.rule_query) INTO v_violations_count;
        
        -- Get sample violations (max 10) for detailed analysis
        IF v_violations_count > 0 THEN
            EXECUTE FORMAT('SELECT jsonb_agg(row_to_json(t)) FROM (SELECT * FROM (%s) violations LIMIT 10) t', v_rule.rule_query) INTO v_sample_violations;
        END IF;
        
        v_end_time := clock_timestamp();
        
        -- Insert successful result
        INSERT INTO data_quality.check_results (
            batch_id, rule_id, violations_count, sample_violations, 
            execution_time_ms, status
        ) VALUES (
            p_batch_id, p_rule_id, v_violations_count, 
            COALESCE(v_sample_violations, '[]'::JSONB),
            EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time))::INTEGER,
            'SUCCESS'
        ) RETURNING id INTO v_result_id;
        
        -- Log result
        IF v_violations_count > 0 THEN
            RAISE NOTICE 'Rule "%" found % violations (%)', 
                v_rule.rule_name, v_violations_count, v_rule.severity;
        END IF;
        
    EXCEPTION WHEN OTHERS THEN
        v_error_message := SQLERRM;
        v_end_time := clock_timestamp();
        
        -- Insert error result
        INSERT INTO data_quality.check_results (
            batch_id, rule_id, violations_count, execution_time_ms, 
            status, error_message
        ) VALUES (
            p_batch_id, p_rule_id, 0,
            EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time))::INTEGER,
            'ERROR', v_error_message
        ) RETURNING id INTO v_result_id;
        
        RAISE WARNING 'Error executing rule "%": %', v_rule.rule_name, v_error_message;
    END;
    
    RETURN v_result_id;
END;
$$ LANGUAGE plpgsql;

-- Function to execute all active data quality rules
CREATE OR REPLACE FUNCTION data_quality.execute_all_rules(
    p_batch_id INTEGER,
    p_table_filter VARCHAR(50) DEFAULT NULL,
    p_severity_filter VARCHAR(10) DEFAULT NULL
) RETURNS TABLE(
    rule_name VARCHAR(100),
    table_name VARCHAR(50),
    severity VARCHAR(10),
    violations_count INTEGER,
    status VARCHAR(20),
    execution_time_ms INTEGER,
    error_message TEXT
) AS $$
DECLARE
    v_rule RECORD;
    v_total_rules INTEGER := 0;
    v_successful_rules INTEGER := 0;
    v_failed_rules INTEGER := 0;
    v_total_violations INTEGER := 0;
BEGIN
    RAISE NOTICE 'Starting data quality check execution for batch %', p_batch_id;
    
    -- Execute all matching rules
    FOR v_rule IN 
        SELECT r.id, r.rule_name, r.table_name, r.severity
        FROM data_quality.rules r
        WHERE r.is_active = true
          AND (p_table_filter IS NULL OR r.table_name = p_table_filter)
          AND (p_severity_filter IS NULL OR r.severity = p_severity_filter)
        ORDER BY 
            CASE r.severity 
                WHEN 'ERROR' THEN 1 
                WHEN 'WARNING' THEN 2 
                WHEN 'INFO' THEN 3 
            END,
            r.rule_name
    LOOP
        v_total_rules := v_total_rules + 1;
        
        -- Execute the rule
        PERFORM data_quality.execute_rule(v_rule.id, p_batch_id);
    END LOOP;
    
    -- Return summary results
    RETURN QUERY
    SELECT 
        r.rule_name,
        r.table_name,
        r.severity,
        cr.violations_count,
        cr.status,
        cr.execution_time_ms,
        cr.error_message
    FROM data_quality.rules r
    JOIN data_quality.check_results cr ON r.id = cr.rule_id
    WHERE cr.batch_id = p_batch_id
      AND (p_table_filter IS NULL OR r.table_name = p_table_filter)
      AND (p_severity_filter IS NULL OR r.severity = p_severity_filter)
    ORDER BY 
        CASE r.severity 
            WHEN 'ERROR' THEN 1 
            WHEN 'WARNING' THEN 2 
            WHEN 'INFO' THEN 3 
        END,
        cr.violations_count DESC,
        r.rule_name;
    
    -- Summary statistics
    SELECT 
        COUNT(*) as total,
        COUNT(*) FILTER (WHERE cr.status = 'SUCCESS') as successful,
        COUNT(*) FILTER (WHERE cr.status = 'ERROR') as failed,
        COALESCE(SUM(cr.violations_count), 0) as total_violations
    INTO v_total_rules, v_successful_rules, v_failed_rules, v_total_violations
    FROM data_quality.check_results cr
    JOIN data_quality.rules r ON cr.rule_id = r.id
    WHERE cr.batch_id = p_batch_id
      AND (p_table_filter IS NULL OR r.table_name = p_table_filter)
      AND (p_severity_filter IS NULL OR r.severity = p_severity_filter);
    
    RAISE NOTICE 'Data quality check completed: % rules executed, % successful, % failed, % total violations',
        v_total_rules, v_successful_rules, v_failed_rules, v_total_violations;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- REPORTING AND ANALYSIS FUNCTIONS
-- =============================================================================

-- Function to generate data quality report
CREATE OR REPLACE FUNCTION data_quality.generate_report(
    p_batch_id INTEGER
) RETURNS TABLE(
    severity VARCHAR(10),
    total_rules INTEGER,
    rules_with_violations INTEGER,
    total_violations BIGINT,
    avg_execution_time_ms NUMERIC,
    worst_rule VARCHAR(100),
    worst_violations INTEGER
) AS $$
BEGIN
    RETURN QUERY
    WITH rule_stats AS (
        SELECT 
            r.severity,
            COUNT(*) as total_rules,
            COUNT(*) FILTER (WHERE cr.violations_count > 0) as rules_with_violations,
            COALESCE(SUM(cr.violations_count), 0) as total_violations,
            ROUND(AVG(cr.execution_time_ms), 2) as avg_execution_time_ms,
            ROW_NUMBER() OVER (PARTITION BY r.severity ORDER BY cr.violations_count DESC) as rn,
            r.rule_name,
            cr.violations_count
        FROM data_quality.rules r
        JOIN data_quality.check_results cr ON r.id = cr.rule_id
        WHERE cr.batch_id = p_batch_id
        GROUP BY r.severity, r.rule_name, cr.violations_count
    )
    SELECT 
        rs.severity,
        rs.total_rules,
        rs.rules_with_violations,
        rs.total_violations,
        rs.avg_execution_time_ms,
        CASE WHEN rs.rn = 1 THEN rs.rule_name ELSE NULL END as worst_rule,
        CASE WHEN rs.rn = 1 THEN rs.violations_count ELSE NULL END as worst_violations
    FROM rule_stats rs
    WHERE rs.rn = 1
    ORDER BY 
        CASE rs.severity 
            WHEN 'ERROR' THEN 1 
            WHEN 'WARNING' THEN 2 
            WHEN 'INFO' THEN 3 
        END;
END;
$$ LANGUAGE plpgsql;

-- Function to get detailed violations for a specific rule
CREATE OR REPLACE FUNCTION data_quality.get_rule_violations(
    p_rule_name VARCHAR(100),
    p_batch_id INTEGER
) RETURNS TABLE(
    violations_count INTEGER,
    sample_violations JSONB,
    execution_time_ms INTEGER,
    check_timestamp TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        cr.violations_count,
        cr.sample_violations,
        cr.execution_time_ms,
        cr.check_timestamp
    FROM data_quality.check_results cr
    JOIN data_quality.rules r ON cr.rule_id = r.id
    WHERE r.rule_name = p_rule_name
      AND cr.batch_id = p_batch_id
    ORDER BY cr.check_timestamp DESC
    LIMIT 1;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- DATA QUALITY MONITORING VIEWS
-- =============================================================================

-- View for current data quality status
CREATE OR REPLACE VIEW data_quality.current_status AS
SELECT 
    r.table_name,
    r.severity,
    COUNT(*) as total_rules,
    COUNT(*) FILTER (WHERE cr.violations_count > 0) as rules_with_violations,
    COALESCE(SUM(cr.violations_count), 0) as total_violations,
    MAX(cr.check_timestamp) as last_check
FROM data_quality.rules r
LEFT JOIN data_quality.check_results cr ON r.id = cr.rule_id
WHERE r.is_active = true
GROUP BY r.table_name, r.severity
ORDER BY r.table_name, 
    CASE r.severity 
        WHEN 'ERROR' THEN 1 
        WHEN 'WARNING' THEN 2 
        WHEN 'INFO' THEN 3 
    END;

-- View for data quality trends
CREATE OR REPLACE VIEW data_quality.quality_trends AS
SELECT 
    cr.batch_id,
    cr.check_timestamp::DATE as check_date,
    r.severity,
    COUNT(*) as rules_executed,
    COUNT(*) FILTER (WHERE cr.violations_count > 0) as rules_with_violations,
    COALESCE(SUM(cr.violations_count), 0) as total_violations
FROM data_quality.check_results cr
JOIN data_quality.rules r ON cr.rule_id = r.id
WHERE cr.status = 'SUCCESS'
GROUP BY cr.batch_id, cr.check_timestamp::DATE, r.severity
ORDER BY cr.check_timestamp::DATE DESC, 
    CASE r.severity 
        WHEN 'ERROR' THEN 1 
        WHEN 'WARNING' THEN 2 
        WHEN 'INFO' THEN 3 
    END;

-- =============================================================================
-- COMMENTS
-- =============================================================================

COMMENT ON SCHEMA data_quality IS 'Data quality management and validation framework';
COMMENT ON TABLE data_quality.rules IS 'Stores data quality rule definitions';
COMMENT ON TABLE data_quality.check_results IS 'Stores results of data quality check executions';
COMMENT ON FUNCTION data_quality.execute_rule(BIGINT, INTEGER) IS 'Executes a single data quality rule';
COMMENT ON FUNCTION data_quality.execute_all_rules(INTEGER, VARCHAR, VARCHAR) IS 'Executes all active data quality rules with optional filtering';
COMMENT ON FUNCTION data_quality.generate_report(INTEGER) IS 'Generates comprehensive data quality report';
COMMENT ON FUNCTION data_quality.get_rule_violations(VARCHAR, INTEGER) IS 'Gets detailed violation information for a specific rule';
COMMENT ON VIEW data_quality.current_status IS 'Current data quality status overview';
COMMENT ON VIEW data_quality.quality_trends IS 'Data quality trends over time';