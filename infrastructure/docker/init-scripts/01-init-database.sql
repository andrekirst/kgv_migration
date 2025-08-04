-- KGV Database Initialization Script
-- This script initializes the PostgreSQL database for the KGV Migration project

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
CREATE EXTENSION IF NOT EXISTS "btree_gin";

-- Create additional user for application (if different from admin)
-- Password should be provided via environment variable
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'kgv_app') THEN
        -- Password will be set via separate secure script or environment
        CREATE ROLE kgv_app WITH LOGIN;
        -- Password should be set using: ALTER ROLE kgv_app WITH PASSWORD 'secure_password';
    END IF;
END
$$;

-- Grant necessary permissions
GRANT CONNECT ON DATABASE kgv_development TO kgv_app;
GRANT USAGE ON SCHEMA public TO kgv_app;
GRANT CREATE ON SCHEMA public TO kgv_app;

-- Create audit schema for tracking changes
CREATE SCHEMA IF NOT EXISTS audit;
GRANT USAGE ON SCHEMA audit TO kgv_app;
GRANT CREATE ON SCHEMA audit TO kgv_app;

-- Create a function for automatic timestamps
CREATE OR REPLACE FUNCTION trigger_set_timestamp()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create audit table function
CREATE OR REPLACE FUNCTION audit.create_audit_table(target_table regclass)
RETURNS void AS $$
DECLARE
    audit_table_name text;
BEGIN
    audit_table_name := 'audit.' || target_table::text || '_audit';
    
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I (
            audit_id SERIAL PRIMARY KEY,
            table_name TEXT NOT NULL,
            operation TEXT NOT NULL,
            old_values JSONB,
            new_values JSONB,
            changed_by TEXT DEFAULT current_user,
            changed_at TIMESTAMP DEFAULT NOW()
        )', audit_table_name);
END;
$$ LANGUAGE plpgsql;

-- Create generic audit trigger function
CREATE OR REPLACE FUNCTION audit.audit_trigger()
RETURNS TRIGGER AS $$
DECLARE
    old_values JSONB := '{}';
    new_values JSONB := '{}';
    table_name TEXT := TG_TABLE_SCHEMA || '.' || TG_TABLE_NAME;
BEGIN
    IF TG_OP = 'UPDATE' THEN
        old_values := row_to_json(OLD);
        new_values := row_to_json(NEW);
    ELSIF TG_OP = 'DELETE' THEN
        old_values := row_to_json(OLD);
    ELSIF TG_OP = 'INSERT' THEN
        new_values := row_to_json(NEW);
    END IF;
    
    INSERT INTO audit.audit_log (table_name, operation, old_values, new_values)
    VALUES (table_name, TG_OP, old_values, new_values);
    
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

-- Create main audit log table
CREATE TABLE IF NOT EXISTS audit.audit_log (
    audit_id SERIAL PRIMARY KEY,
    table_name TEXT NOT NULL,
    operation TEXT NOT NULL,
    old_values JSONB,
    new_values JSONB,
    changed_by TEXT DEFAULT current_user,
    changed_at TIMESTAMP DEFAULT NOW()
);

-- Create indexes for audit table
CREATE INDEX IF NOT EXISTS idx_audit_log_table_name ON audit.audit_log(table_name);
CREATE INDEX IF NOT EXISTS idx_audit_log_changed_at ON audit.audit_log(changed_at);
CREATE INDEX IF NOT EXISTS idx_audit_log_operation ON audit.audit_log(operation);

-- Grant permissions on audit schema
GRANT ALL ON ALL TABLES IN SCHEMA audit TO kgv_app;
GRANT ALL ON ALL SEQUENCES IN SCHEMA audit TO kgv_app;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA audit TO kgv_app;

COMMENT ON SCHEMA audit IS 'Schema for audit logging and change tracking';
COMMENT ON TABLE audit.audit_log IS 'Main audit log table for tracking all database changes';

-- Log the initialization
INSERT INTO audit.audit_log (table_name, operation, new_values) 
VALUES ('system', 'INIT', '{"message": "Database initialized for KGV Migration project", "timestamp": "' || NOW() || '"}');

-- Create a view for recent audit activity
CREATE OR REPLACE VIEW audit.recent_activity AS
SELECT 
    audit_id,
    table_name,
    operation,
    changed_by,
    changed_at,
    CASE 
        WHEN old_values IS NOT NULL AND new_values IS NOT NULL THEN 'UPDATE'
        WHEN old_values IS NULL AND new_values IS NOT NULL THEN 'INSERT'
        WHEN old_values IS NOT NULL AND new_values IS NULL THEN 'DELETE'
        ELSE operation
    END as change_type
FROM audit.audit_log
ORDER BY changed_at DESC
LIMIT 100;

GRANT SELECT ON audit.recent_activity TO kgv_app;