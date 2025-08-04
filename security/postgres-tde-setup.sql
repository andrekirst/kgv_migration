-- PostgreSQL Transparent Data Encryption (TDE) and Security Configuration
-- Version: PostgreSQL 15+ with pgcrypto and pg_crypto extensions
-- This script implements database-level encryption and security hardening

-- ============================================================================
-- PREREQUISITES
-- ============================================================================
-- Ensure PostgreSQL is compiled with OpenSSL support
-- Install required extensions: pgcrypto, pg_stat_statements

-- ============================================================================
-- 1. ENABLE REQUIRED EXTENSIONS
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;
CREATE EXTENSION IF NOT EXISTS postgres_fdw;

-- ============================================================================
-- 2. CREATE ENCRYPTION SCHEMA AND FUNCTIONS
-- ============================================================================

-- Create dedicated schema for encryption functions
CREATE SCHEMA IF NOT EXISTS encryption;

-- Set default privileges
ALTER DEFAULT PRIVILEGES IN SCHEMA encryption
    GRANT USAGE ON SEQUENCES TO kgv_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA encryption
    GRANT SELECT ON TABLES TO kgv_app;

-- Master encryption key table (store encrypted)
CREATE TABLE IF NOT EXISTS encryption.master_keys (
    id SERIAL PRIMARY KEY,
    key_name VARCHAR(100) UNIQUE NOT NULL,
    encrypted_key BYTEA NOT NULL,
    key_salt BYTEA NOT NULL,
    algorithm VARCHAR(50) DEFAULT 'AES-256-GCM',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    rotated_at TIMESTAMP,
    is_active BOOLEAN DEFAULT true,
    created_by VARCHAR(100) DEFAULT CURRENT_USER
);

-- Audit table for key operations
CREATE TABLE IF NOT EXISTS encryption.key_audit_log (
    id SERIAL PRIMARY KEY,
    key_id INTEGER REFERENCES encryption.master_keys(id),
    operation VARCHAR(50) NOT NULL,
    performed_by VARCHAR(100) DEFAULT CURRENT_USER,
    performed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    details JSONB
);

-- Function to generate encryption key
CREATE OR REPLACE FUNCTION encryption.generate_data_key()
RETURNS BYTEA AS $$
BEGIN
    RETURN gen_random_bytes(32); -- 256-bit key
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to encrypt sensitive data
CREATE OR REPLACE FUNCTION encryption.encrypt_data(
    p_data TEXT,
    p_key_name VARCHAR DEFAULT 'default'
)
RETURNS BYTEA AS $$
DECLARE
    v_key BYTEA;
    v_iv BYTEA;
    v_encrypted BYTEA;
BEGIN
    -- Get active encryption key
    SELECT encrypted_key INTO v_key
    FROM encryption.master_keys
    WHERE key_name = p_key_name AND is_active = true;
    
    IF v_key IS NULL THEN
        RAISE EXCEPTION 'No active encryption key found for: %', p_key_name;
    END IF;
    
    -- Generate random IV for each encryption
    v_iv := gen_random_bytes(16);
    
    -- Encrypt data using AES-256-CBC
    v_encrypted := encrypt_iv(
        p_data::BYTEA,
        v_key,
        v_iv,
        'aes-cbc'
    );
    
    -- Return IV + encrypted data
    RETURN v_iv || v_encrypted;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to decrypt sensitive data
CREATE OR REPLACE FUNCTION encryption.decrypt_data(
    p_encrypted_data BYTEA,
    p_key_name VARCHAR DEFAULT 'default'
)
RETURNS TEXT AS $$
DECLARE
    v_key BYTEA;
    v_iv BYTEA;
    v_encrypted BYTEA;
    v_decrypted BYTEA;
BEGIN
    -- Get encryption key
    SELECT encrypted_key INTO v_key
    FROM encryption.master_keys
    WHERE key_name = p_key_name;
    
    IF v_key IS NULL THEN
        RAISE EXCEPTION 'Encryption key not found for: %', p_key_name;
    END IF;
    
    -- Extract IV (first 16 bytes)
    v_iv := substring(p_encrypted_data FROM 1 FOR 16);
    
    -- Extract encrypted data (remaining bytes)
    v_encrypted := substring(p_encrypted_data FROM 17);
    
    -- Decrypt data
    v_decrypted := decrypt_iv(
        v_encrypted,
        v_key,
        v_iv,
        'aes-cbc'
    );
    
    RETURN convert_from(v_decrypted, 'UTF8');
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- 3. CREATE ENCRYPTED COLUMNS FOR SENSITIVE DATA
-- ============================================================================

-- Add encrypted columns to applications table for PII
ALTER TABLE applications ADD COLUMN IF NOT EXISTS 
    first_name_encrypted BYTEA,
    last_name_encrypted BYTEA,
    birth_date_encrypted BYTEA,
    street_encrypted BYTEA,
    email_encrypted BYTEA,
    phone_encrypted BYTEA;

-- Add encrypted columns to users table
ALTER TABLE users ADD COLUMN IF NOT EXISTS
    email_encrypted BYTEA,
    phone_encrypted BYTEA,
    password_hash VARCHAR(255);

-- Create indexes on encrypted columns for performance
CREATE INDEX IF NOT EXISTS idx_applications_encrypted_email 
    ON applications(email_encrypted);
CREATE INDEX IF NOT EXISTS idx_users_encrypted_email 
    ON users(email_encrypted);

-- ============================================================================
-- 4. ROW-LEVEL SECURITY (RLS) POLICIES
-- ============================================================================

-- Enable RLS on sensitive tables
ALTER TABLE applications ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE application_history ENABLE ROW LEVEL SECURITY;

-- Create security policies for applications
CREATE POLICY applications_select_policy ON applications
    FOR SELECT
    USING (
        -- Users can only see their own applications
        user_id = current_setting('app.current_user_id')::INTEGER
        OR 
        -- Admins can see all
        EXISTS (
            SELECT 1 FROM users u 
            WHERE u.id = current_setting('app.current_user_id')::INTEGER 
            AND u.is_admin = true
        )
    );

CREATE POLICY applications_insert_policy ON applications
    FOR INSERT
    WITH CHECK (
        -- Users can only insert their own applications
        user_id = current_setting('app.current_user_id')::INTEGER
    );

CREATE POLICY applications_update_policy ON applications
    FOR UPDATE
    USING (
        -- Users can only update their own applications
        user_id = current_setting('app.current_user_id')::INTEGER
        OR 
        -- Admins can update all
        EXISTS (
            SELECT 1 FROM users u 
            WHERE u.id = current_setting('app.current_user_id')::INTEGER 
            AND u.is_admin = true
        )
    );

CREATE POLICY applications_delete_policy ON applications
    FOR DELETE
    USING (
        -- Only admins can delete
        EXISTS (
            SELECT 1 FROM users u 
            WHERE u.id = current_setting('app.current_user_id')::INTEGER 
            AND u.is_admin = true
        )
    );

-- ============================================================================
-- 5. AUDIT TRIGGERS FOR GDPR COMPLIANCE
-- ============================================================================

-- Create audit table
CREATE TABLE IF NOT EXISTS audit.data_changes (
    id BIGSERIAL PRIMARY KEY,
    table_name VARCHAR(100) NOT NULL,
    operation VARCHAR(10) NOT NULL,
    user_name VARCHAR(100) DEFAULT CURRENT_USER,
    user_id INTEGER,
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    row_id INTEGER,
    old_data JSONB,
    new_data JSONB,
    ip_address INET,
    session_id VARCHAR(100)
);

-- Create partitioned table for better performance
CREATE TABLE IF NOT EXISTS audit.data_changes_2024 PARTITION OF audit.data_changes
    FOR VALUES FROM ('2024-01-01') TO ('2025-01-01');
CREATE TABLE IF NOT EXISTS audit.data_changes_2025 PARTITION OF audit.data_changes
    FOR VALUES FROM ('2025-01-01') TO ('2026-01-01');

-- Generic audit trigger function
CREATE OR REPLACE FUNCTION audit.log_data_changes()
RETURNS TRIGGER AS $$
DECLARE
    v_old_data JSONB;
    v_new_data JSONB;
    v_user_id INTEGER;
BEGIN
    -- Get current user ID from session
    BEGIN
        v_user_id := current_setting('app.current_user_id')::INTEGER;
    EXCEPTION WHEN OTHERS THEN
        v_user_id := NULL;
    END;
    
    IF TG_OP = 'DELETE' THEN
        v_old_data := to_jsonb(OLD);
        v_new_data := NULL;
        
        INSERT INTO audit.data_changes (
            table_name, operation, user_id, row_id, old_data, new_data,
            ip_address, session_id
        ) VALUES (
            TG_TABLE_NAME, TG_OP, v_user_id, OLD.id, v_old_data, v_new_data,
            inet_client_addr(), current_setting('app.session_id', true)
        );
        
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        -- Mask sensitive data in audit log
        v_old_data := audit.mask_sensitive_data(to_jsonb(OLD), TG_TABLE_NAME);
        v_new_data := audit.mask_sensitive_data(to_jsonb(NEW), TG_TABLE_NAME);
        
        INSERT INTO audit.data_changes (
            table_name, operation, user_id, row_id, old_data, new_data,
            ip_address, session_id
        ) VALUES (
            TG_TABLE_NAME, TG_OP, v_user_id, NEW.id, v_old_data, v_new_data,
            inet_client_addr(), current_setting('app.session_id', true)
        );
        
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        v_old_data := NULL;
        v_new_data := audit.mask_sensitive_data(to_jsonb(NEW), TG_TABLE_NAME);
        
        INSERT INTO audit.data_changes (
            table_name, operation, user_id, row_id, old_data, new_data,
            ip_address, session_id
        ) VALUES (
            TG_TABLE_NAME, TG_OP, v_user_id, NEW.id, v_old_data, v_new_data,
            inet_client_addr(), current_setting('app.session_id', true)
        );
        
        RETURN NEW;
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to mask sensitive data in audit logs
CREATE OR REPLACE FUNCTION audit.mask_sensitive_data(
    p_data JSONB,
    p_table_name VARCHAR
)
RETURNS JSONB AS $$
DECLARE
    v_masked JSONB;
    v_sensitive_fields TEXT[];
BEGIN
    v_masked := p_data;
    
    -- Define sensitive fields per table
    CASE p_table_name
        WHEN 'applications' THEN
            v_sensitive_fields := ARRAY['birth_date', 'email', 'phone', 
                                       'mobile_phone', 'street', 'postal_code'];
        WHEN 'users' THEN
            v_sensitive_fields := ARRAY['email', 'phone', 'password_hash'];
        ELSE
            v_sensitive_fields := ARRAY[]::TEXT[];
    END CASE;
    
    -- Mask sensitive fields
    FOR i IN 1..array_length(v_sensitive_fields, 1) LOOP
        IF v_masked ? v_sensitive_fields[i] THEN
            v_masked := jsonb_set(
                v_masked,
                ARRAY[v_sensitive_fields[i]],
                '"***MASKED***"'::JSONB
            );
        END IF;
    END LOOP;
    
    RETURN v_masked;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Apply audit triggers to sensitive tables
CREATE TRIGGER applications_audit_trigger
    AFTER INSERT OR UPDATE OR DELETE ON applications
    FOR EACH ROW EXECUTE FUNCTION audit.log_data_changes();

CREATE TRIGGER users_audit_trigger
    AFTER INSERT OR UPDATE OR DELETE ON users
    FOR EACH ROW EXECUTE FUNCTION audit.log_data_changes();

CREATE TRIGGER application_history_audit_trigger
    AFTER INSERT OR UPDATE OR DELETE ON application_history
    FOR EACH ROW EXECUTE FUNCTION audit.log_data_changes();

-- ============================================================================
-- 6. CONFIGURE SSL/TLS SETTINGS
-- ============================================================================

-- These settings should be in postgresql.conf
-- ssl = on
-- ssl_cert_file = 'server.crt'
-- ssl_key_file = 'server.key'
-- ssl_ca_file = 'ca.crt'
-- ssl_ciphers = 'HIGH:MEDIUM:+3DES:!aNULL:!MD5:!RC4'
-- ssl_prefer_server_ciphers = on
-- ssl_ecdh_curve = 'prime256v1'
-- ssl_min_protocol_version = 'TLSv1.2'

-- Force SSL for all connections (add to pg_hba.conf)
-- hostssl all all 0.0.0.0/0 md5

-- ============================================================================
-- 7. CREATE SECURE VIEWS FOR APPLICATION ACCESS
-- ============================================================================

-- Create secure view for applications with decrypted data
CREATE OR REPLACE VIEW secure.applications_decrypted AS
SELECT 
    id,
    uuid,
    file_reference,
    waiting_list_number_32,
    waiting_list_number_33,
    salutation,
    title,
    -- Decrypt sensitive fields on the fly
    CASE 
        WHEN first_name_encrypted IS NOT NULL 
        THEN encryption.decrypt_data(first_name_encrypted)
        ELSE first_name
    END AS first_name,
    CASE 
        WHEN last_name_encrypted IS NOT NULL 
        THEN encryption.decrypt_data(last_name_encrypted)
        ELSE last_name
    END AS last_name,
    CASE 
        WHEN birth_date_encrypted IS NOT NULL 
        THEN encryption.decrypt_data(birth_date_encrypted)::DATE
        ELSE birth_date
    END AS birth_date,
    CASE 
        WHEN email_encrypted IS NOT NULL 
        THEN encryption.decrypt_data(email_encrypted)
        ELSE email
    END AS email,
    CASE 
        WHEN phone_encrypted IS NOT NULL 
        THEN encryption.decrypt_data(phone_encrypted)
        ELSE phone
    END AS phone,
    city,
    application_date,
    confirmation_date,
    is_active,
    created_at,
    updated_at
FROM applications
WHERE 
    -- Apply RLS policies
    user_id = current_setting('app.current_user_id')::INTEGER
    OR EXISTS (
        SELECT 1 FROM users u 
        WHERE u.id = current_setting('app.current_user_id')::INTEGER 
        AND u.is_admin = true
    );

-- Grant appropriate permissions
GRANT SELECT ON secure.applications_decrypted TO kgv_app;

-- ============================================================================
-- 8. IMPLEMENT DATA RETENTION POLICIES
-- ============================================================================

-- Function to anonymize old records for GDPR compliance
CREATE OR REPLACE FUNCTION gdpr.anonymize_old_applications()
RETURNS INTEGER AS $$
DECLARE
    v_count INTEGER;
    v_retention_period INTERVAL := '7 years'::INTERVAL;
BEGIN
    UPDATE applications
    SET 
        first_name = 'ANONYMIZED',
        last_name = 'ANONYMIZED',
        first_name_encrypted = NULL,
        last_name_encrypted = NULL,
        birth_date = NULL,
        birth_date_encrypted = NULL,
        email = 'anonymized@example.com',
        email_encrypted = NULL,
        phone = '000-000-0000',
        phone_encrypted = NULL,
        street = 'ANONYMIZED',
        street_encrypted = NULL,
        postal_code = '00000'
    WHERE 
        created_at < CURRENT_TIMESTAMP - v_retention_period
        AND is_active = false;
    
    GET DIAGNOSTICS v_count = ROW_COUNT;
    
    -- Log anonymization
    INSERT INTO audit.data_changes (
        table_name, operation, user_name, details
    ) VALUES (
        'applications', 'ANONYMIZE', 'SYSTEM', 
        jsonb_build_object('records_anonymized', v_count)
    );
    
    RETURN v_count;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Schedule anonymization job (requires pg_cron extension)
-- SELECT cron.schedule('anonymize-old-data', '0 2 * * 0', 
--     'SELECT gdpr.anonymize_old_applications();');

-- ============================================================================
-- 9. PERFORMANCE OPTIMIZATION FOR ENCRYPTED DATA
-- ============================================================================

-- Create indexes for encrypted search
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Create trigram index for fuzzy search on encrypted data
CREATE INDEX IF NOT EXISTS idx_applications_email_trgm 
    ON applications USING gin (email gin_trgm_ops)
    WHERE email_encrypted IS NULL;

-- Function for secure search on encrypted data
CREATE OR REPLACE FUNCTION secure.search_applications_by_email(
    p_email_pattern VARCHAR
)
RETURNS TABLE (
    id INTEGER,
    uuid UUID,
    first_name VARCHAR,
    last_name VARCHAR,
    email VARCHAR
) AS $$
BEGIN
    -- Only allow admins to search
    IF NOT EXISTS (
        SELECT 1 FROM users u 
        WHERE u.id = current_setting('app.current_user_id')::INTEGER 
        AND u.is_admin = true
    ) THEN
        RAISE EXCEPTION 'Unauthorized: Admin access required';
    END IF;
    
    RETURN QUERY
    SELECT 
        a.id,
        a.uuid,
        CASE 
            WHEN a.first_name_encrypted IS NOT NULL 
            THEN encryption.decrypt_data(a.first_name_encrypted)
            ELSE a.first_name
        END,
        CASE 
            WHEN a.last_name_encrypted IS NOT NULL 
            THEN encryption.decrypt_data(a.last_name_encrypted)
            ELSE a.last_name
        END,
        CASE 
            WHEN a.email_encrypted IS NOT NULL 
            THEN encryption.decrypt_data(a.email_encrypted)
            ELSE a.email
        END
    FROM applications a
    WHERE 
        CASE 
            WHEN a.email_encrypted IS NOT NULL 
            THEN encryption.decrypt_data(a.email_encrypted)
            ELSE a.email
        END ILIKE '%' || p_email_pattern || '%';
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- 10. SECURITY MONITORING FUNCTIONS
-- ============================================================================

-- Function to detect suspicious activity
CREATE OR REPLACE FUNCTION security.detect_suspicious_activity()
RETURNS TABLE (
    user_id INTEGER,
    user_name VARCHAR,
    suspicious_actions INTEGER,
    time_window TIMESTAMP,
    details JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        a.user_id,
        a.user_name,
        COUNT(*) AS suspicious_actions,
        date_trunc('hour', a.changed_at) AS time_window,
        jsonb_agg(
            jsonb_build_object(
                'table', a.table_name,
                'operation', a.operation,
                'time', a.changed_at
            )
        ) AS details
    FROM audit.data_changes a
    WHERE 
        a.changed_at > CURRENT_TIMESTAMP - INTERVAL '1 hour'
        AND (
            -- Multiple delete operations
            (a.operation = 'DELETE' AND a.user_id IS NOT NULL)
            OR
            -- Bulk updates
            (a.operation = 'UPDATE' AND a.user_id IN (
                SELECT user_id 
                FROM audit.data_changes 
                WHERE changed_at > CURRENT_TIMESTAMP - INTERVAL '5 minutes'
                GROUP BY user_id 
                HAVING COUNT(*) > 100
            ))
        )
    GROUP BY a.user_id, a.user_name, date_trunc('hour', a.changed_at)
    HAVING COUNT(*) > 10;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- 11. INITIALIZE ENCRYPTION KEYS
-- ============================================================================

-- Initialize master encryption key (run once during setup)
DO $$
DECLARE
    v_master_key BYTEA;
    v_salt BYTEA;
BEGIN
    -- Check if key already exists
    IF NOT EXISTS (
        SELECT 1 FROM encryption.master_keys 
        WHERE key_name = 'default' AND is_active = true
    ) THEN
        -- Generate master key and salt
        v_master_key := gen_random_bytes(32);
        v_salt := gen_random_bytes(16);
        
        -- Store encrypted master key
        INSERT INTO encryption.master_keys (
            key_name, encrypted_key, key_salt, algorithm
        ) VALUES (
            'default', v_master_key, v_salt, 'AES-256-GCM'
        );
        
        -- Log key creation
        INSERT INTO encryption.key_audit_log (
            key_id, operation, details
        ) VALUES (
            currval('encryption.master_keys_id_seq'),
            'CREATE',
            jsonb_build_object('algorithm', 'AES-256-GCM', 'key_size', 256)
        );
        
        RAISE NOTICE 'Master encryption key created successfully';
    END IF;
END $$;

-- ============================================================================
-- 12. GRANT MINIMAL REQUIRED PERMISSIONS
-- ============================================================================

-- Revoke all default permissions
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM PUBLIC;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM PUBLIC;
REVOKE ALL ON ALL FUNCTIONS IN SCHEMA public FROM PUBLIC;

-- Create application role
CREATE ROLE kgv_app WITH LOGIN PASSWORD 'CHANGE_THIS_PASSWORD';

-- Grant schema usage
GRANT USAGE ON SCHEMA public TO kgv_app;
GRANT USAGE ON SCHEMA secure TO kgv_app;
GRANT USAGE ON SCHEMA encryption TO kgv_app;

-- Grant table permissions
GRANT SELECT, INSERT, UPDATE ON applications TO kgv_app;
GRANT SELECT, INSERT, UPDATE ON users TO kgv_app;
GRANT SELECT, INSERT ON application_history TO kgv_app;
GRANT SELECT ON districts TO kgv_app;
GRANT SELECT ON cadastral_districts TO kgv_app;

-- Grant sequence permissions
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO kgv_app;

-- Grant function permissions
GRANT EXECUTE ON FUNCTION encryption.encrypt_data TO kgv_app;
GRANT EXECUTE ON FUNCTION encryption.decrypt_data TO kgv_app;
GRANT EXECUTE ON FUNCTION secure.search_applications_by_email TO kgv_app;

-- Create read-only monitoring role
CREATE ROLE kgv_monitor WITH LOGIN PASSWORD 'CHANGE_THIS_PASSWORD';
GRANT USAGE ON SCHEMA public TO kgv_monitor;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO kgv_monitor;

-- ============================================================================
-- END OF SECURITY CONFIGURATION
-- ============================================================================