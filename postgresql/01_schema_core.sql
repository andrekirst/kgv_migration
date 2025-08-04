-- =============================================================================
-- KGV Migration: Modernisiertes PostgreSQL Schema
-- Version: 1.0
-- Description: Core schema for KGV (Kleingartenverein) management system
-- =============================================================================

-- Enable necessary extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
CREATE EXTENSION IF NOT EXISTS "btree_gin";

-- Create custom domains for business rules
CREATE DOMAIN email_address AS TEXT 
CHECK (VALUE ~* '^[A-Za-z0-9._%-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$');

CREATE DOMAIN phone_number AS VARCHAR(50)
CHECK (VALUE ~ '^[\+]?[0-9\-\s\(\)]{3,50}$');

CREATE DOMAIN postal_code AS VARCHAR(10)
CHECK (VALUE ~ '^[0-9]{5}$');

-- =============================================================================
-- CORE ENTITIES
-- =============================================================================

-- Districts (Bezirke) - Administrative regions
CREATE TABLE districts (
    id BIGSERIAL PRIMARY KEY,
    uuid UUID NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
    name VARCHAR(10) NOT NULL UNIQUE,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT districts_name_check CHECK (LENGTH(name) >= 1)
);

-- Cadastral districts (Katasterbezirke) - Land registry areas
CREATE TABLE cadastral_districts (
    id BIGSERIAL PRIMARY KEY,
    uuid UUID NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
    district_id BIGINT NOT NULL,
    code VARCHAR(10) NOT NULL,
    name VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_cadastral_districts_district 
        FOREIGN KEY (district_id) REFERENCES districts(id) 
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT uk_cadastral_districts_district_code 
        UNIQUE (district_id, code)
);

-- File reference numbers (Aktenzeichen) - Document reference system
CREATE TABLE file_references (
    id BIGSERIAL PRIMARY KEY,
    uuid UUID NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
    district_code VARCHAR(10) NOT NULL,
    number INTEGER NOT NULL,
    year INTEGER NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT file_references_year_check 
        CHECK (year >= 1900 AND year <= EXTRACT(YEAR FROM NOW()) + 10),
    CONSTRAINT file_references_number_check 
        CHECK (number > 0),
    CONSTRAINT uk_file_references_district_number_year 
        UNIQUE (district_code, number, year)
);

-- Entry numbers (Eingangsnummern) - Sequential entry tracking
CREATE TABLE entry_numbers (
    id BIGSERIAL PRIMARY KEY,
    uuid UUID NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
    district_code VARCHAR(10) NOT NULL,
    number INTEGER NOT NULL,
    year INTEGER NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT entry_numbers_year_check 
        CHECK (year >= 1900 AND year <= EXTRACT(YEAR FROM NOW()) + 10),
    CONSTRAINT entry_numbers_number_check 
        CHECK (number > 0),
    CONSTRAINT uk_entry_numbers_district_number_year 
        UNIQUE (district_code, number, year)
);

-- Applications (Anträge) - Core application data
CREATE TABLE applications (
    id BIGSERIAL PRIMARY KEY,
    uuid UUID NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
    file_reference VARCHAR(20),
    waiting_list_number_32 VARCHAR(20),
    waiting_list_number_33 VARCHAR(20),
    
    -- Primary applicant
    salutation VARCHAR(10),
    title VARCHAR(50),
    first_name VARCHAR(50),
    last_name VARCHAR(50),
    birth_date DATE,
    
    -- Secondary applicant (partner/spouse)
    salutation_2 VARCHAR(10),
    title_2 VARCHAR(50),
    first_name_2 VARCHAR(50),
    last_name_2 VARCHAR(50),
    birth_date_2 DATE,
    
    -- Address information
    letter_salutation VARCHAR(150),
    street VARCHAR(50),
    postal_code postal_code,
    city VARCHAR(50),
    
    -- Contact information
    phone phone_number,
    mobile_phone phone_number,
    mobile_phone_2 phone_number,
    business_phone phone_number,
    email email_address,
    
    -- Application timeline
    application_date DATE,
    confirmation_date DATE,
    current_offer_date DATE,
    deletion_date DATE,
    deactivated_at TIMESTAMP WITH TIME ZONE,
    
    -- Application details
    preferences TEXT,
    remarks TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    -- Metadata
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_by BIGINT,
    updated_by BIGINT,
    
    -- Business rules
    CONSTRAINT applications_dates_check 
        CHECK (
            application_date IS NULL OR 
            confirmation_date IS NULL OR 
            application_date <= confirmation_date
        ),
    CONSTRAINT applications_deletion_deactivation_check 
        CHECK (
            (deletion_date IS NULL AND deactivated_at IS NULL) OR
            (deletion_date IS NOT NULL OR deactivated_at IS NOT NULL)
        )
);

-- System users/employees (Personen) - User management
CREATE TABLE users (
    id BIGSERIAL PRIMARY KEY,
    uuid UUID NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
    salutation VARCHAR(10),
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    employee_number VARCHAR(7) UNIQUE,
    
    -- Organizational information
    department VARCHAR(10),
    room VARCHAR(10),
    phone VARCHAR(10),
    fax VARCHAR(10),
    email email_address,
    signature_code VARCHAR(5),
    signature_text VARCHAR(50),
    job_title VARCHAR(30),
    
    -- Permissions
    is_admin BOOLEAN NOT NULL DEFAULT false,
    can_administrate BOOLEAN NOT NULL DEFAULT false,
    can_manage_service_groups BOOLEAN NOT NULL DEFAULT false,
    can_manage_priorities_sla BOOLEAN NOT NULL DEFAULT false,
    can_manage_customers BOOLEAN NOT NULL DEFAULT false,
    
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT users_name_check 
        CHECK (LENGTH(TRIM(first_name)) > 0 AND LENGTH(TRIM(last_name)) > 0)
);

-- Application history/timeline (Verlauf) - Audit trail
CREATE TABLE application_history (
    id BIGSERIAL PRIMARY KEY,
    uuid UUID NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
    application_id BIGINT NOT NULL,
    action_type VARCHAR(4) NOT NULL,
    action_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    -- Land/plot information
    gemarkung VARCHAR(50),
    flur VARCHAR(20),
    parcel VARCHAR(20),
    size_info VARCHAR(20),
    
    -- Processing information
    case_worker VARCHAR(100),
    note VARCHAR(100),
    comment TEXT,
    user_id BIGINT,
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_application_history_application 
        FOREIGN KEY (application_id) REFERENCES applications(id) 
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_application_history_user 
        FOREIGN KEY (user_id) REFERENCES users(id) 
        ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT application_history_action_type_check 
        CHECK (action_type IN ('CREA', 'UPD', 'DEL', 'OFFT', 'ACPT', 'REJT', 'WAIT'))
);

-- Identifiers/codes (Kennungen) - System identifiers
CREATE TABLE identifiers (
    id BIGSERIAL PRIMARY KEY,
    uuid UUID NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
    name VARCHAR(50) NOT NULL,
    domain VARCHAR(50) NOT NULL,
    user_id BIGINT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_identifiers_user 
        FOREIGN KEY (user_id) REFERENCES users(id) 
        ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT uk_identifiers_name_domain 
        UNIQUE (name, domain)
);

-- Field mappings (Mischenfelder) - Document field mappings
CREATE TABLE field_mappings (
    id BIGSERIAL PRIMARY KEY,
    uuid UUID NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
    database_field VARCHAR(50) NOT NULL,
    document_field VARCHAR(50) NOT NULL,
    comment VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT uk_field_mappings_database_field 
        UNIQUE (database_field),
    CONSTRAINT uk_field_mappings_document_field 
        UNIQUE (document_field)
);

-- =============================================================================
-- INDEXES FOR PERFORMANCE
-- =============================================================================

-- Districts
CREATE INDEX idx_districts_name ON districts(name);
CREATE INDEX idx_districts_active ON districts(is_active) WHERE is_active = true;

-- Cadastral districts
CREATE INDEX idx_cadastral_districts_district_id ON cadastral_districts(district_id);
CREATE INDEX idx_cadastral_districts_code ON cadastral_districts(code);
CREATE INDEX idx_cadastral_districts_active ON cadastral_districts(is_active) WHERE is_active = true;

-- File references
CREATE INDEX idx_file_references_district_year ON file_references(district_code, year);
CREATE INDEX idx_file_references_year ON file_references(year);
CREATE INDEX idx_file_references_active ON file_references(is_active) WHERE is_active = true;

-- Entry numbers
CREATE INDEX idx_entry_numbers_district_year ON entry_numbers(district_code, year);
CREATE INDEX idx_entry_numbers_year ON entry_numbers(year);
CREATE INDEX idx_entry_numbers_active ON entry_numbers(is_active) WHERE is_active = true;

-- Applications - Performance critical indexes
CREATE INDEX idx_applications_file_reference ON applications(file_reference);
CREATE INDEX idx_applications_waiting_lists ON applications(waiting_list_number_32, waiting_list_number_33);
CREATE INDEX idx_applications_name_search ON applications USING gin ((first_name || ' ' || last_name) gin_trgm_ops);
CREATE INDEX idx_applications_dates ON applications(application_date, confirmation_date);
CREATE INDEX idx_applications_active ON applications(is_active) WHERE is_active = true;
CREATE INDEX idx_applications_created_at ON applications(created_at);
CREATE INDEX idx_applications_postal_code ON applications(postal_code);

-- Users
CREATE INDEX idx_users_employee_number ON users(employee_number);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_name_search ON users USING gin ((first_name || ' ' || last_name) gin_trgm_ops);
CREATE INDEX idx_users_active ON users(is_active) WHERE is_active = true;
CREATE INDEX idx_users_permissions ON users(is_admin, can_administrate);

-- Application history - Critical for audit queries
CREATE INDEX idx_application_history_application_id ON application_history(application_id);
CREATE INDEX idx_application_history_action_date ON application_history(action_date);
CREATE INDEX idx_application_history_action_type ON application_history(action_type);
CREATE INDEX idx_application_history_user_id ON application_history(user_id);

-- Identifiers
CREATE INDEX idx_identifiers_domain ON identifiers(domain);
CREATE INDEX idx_identifiers_user_id ON identifiers(user_id);
CREATE INDEX idx_identifiers_active ON identifiers(is_active) WHERE is_active = true;

-- Field mappings
CREATE INDEX idx_field_mappings_active ON field_mappings(is_active) WHERE is_active = true;

-- =============================================================================
-- TRIGGERS FOR AUDIT TRAIL
-- =============================================================================

-- Function to update timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply updated_at triggers
CREATE TRIGGER trigger_districts_updated_at 
    BEFORE UPDATE ON districts 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_cadastral_districts_updated_at 
    BEFORE UPDATE ON cadastral_districts 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_file_references_updated_at 
    BEFORE UPDATE ON file_references 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_entry_numbers_updated_at 
    BEFORE UPDATE ON entry_numbers 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_applications_updated_at 
    BEFORE UPDATE ON applications 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_users_updated_at 
    BEFORE UPDATE ON users 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_identifiers_updated_at 
    BEFORE UPDATE ON identifiers 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_field_mappings_updated_at 
    BEFORE UPDATE ON field_mappings 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================================================
-- COMMENTS FOR DOCUMENTATION
-- =============================================================================

COMMENT ON TABLE districts IS 'Administrative districts for KGV management';
COMMENT ON TABLE cadastral_districts IS 'Cadastral/land registry districts';
COMMENT ON TABLE file_references IS 'Official file reference numbers for applications';
COMMENT ON TABLE entry_numbers IS 'Sequential entry numbers for tracking';
COMMENT ON TABLE applications IS 'Core application data for garden plot requests';
COMMENT ON TABLE users IS 'System users and employees';
COMMENT ON TABLE application_history IS 'Complete audit trail for all application changes';
COMMENT ON TABLE identifiers IS 'System identifiers and codes';
COMMENT ON TABLE field_mappings IS 'Database to document field mappings';

-- Column comments for critical business fields
COMMENT ON COLUMN applications.waiting_list_number_32 IS 'Waiting list position for area 32';
COMMENT ON COLUMN applications.waiting_list_number_33 IS 'Waiting list position for area 33';
COMMENT ON COLUMN application_history.action_type IS 'CREA=Created, UPD=Updated, DEL=Deleted, OFFT=Offer Made, ACPT=Accepted, REJT=Rejected, WAIT=Waiting';