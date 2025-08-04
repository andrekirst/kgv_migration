-- =============================================================================
-- KGV Migration: SQL Server to PostgreSQL Data Type Mapping
-- Version: 1.0
-- Description: Data type conversion and mapping rules
-- =============================================================================

-- =============================================================================
-- DATA TYPE MAPPING REFERENCE
-- =============================================================================

/*
SQL Server Type          PostgreSQL Type         Notes
----------------         ------------------      -------------------------
uniqueidentifier         UUID                    Use uuid_generate_v4()
varchar(n)              VARCHAR(n) or TEXT       TEXT for variable length
char(n)                 CHAR(n)                 Fixed length
int                     INTEGER                  32-bit integer
datetime                TIMESTAMP WITH TIME ZONE ISO standard with timezone
bit                     BOOLEAN                  true/false instead of 1/0

GUID Conversion Strategy:
- SQL Server UUIDs will be converted to PostgreSQL UUIDs
- New records will use uuid_generate_v4() for compatibility
- Existing GUIDs will be preserved during migration
*/

-- =============================================================================
-- MIGRATION STAGING TABLES
-- =============================================================================

-- Create schema for migration staging
CREATE SCHEMA IF NOT EXISTS migration_staging;

-- Staging table for SQL Server data with original structure
CREATE TABLE migration_staging.raw_aktenzeichen (
    az_ID VARCHAR(36),  -- GUID as string for initial import
    az_Bezirk VARCHAR(10),
    az_Nummer INTEGER,
    az_Jahr INTEGER,
    migration_batch_id INTEGER,
    migration_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE migration_staging.raw_antrag (
    an_ID VARCHAR(36),
    an_Aktenzeichen VARCHAR(20),
    an_WartelistenNr32 VARCHAR(20),
    an_WartelistenNr33 VARCHAR(20),
    an_Anrede VARCHAR(10),
    an_Titel VARCHAR(50),
    an_Vorname VARCHAR(50),
    an_Nachname VARCHAR(50),
    an_Anrede2 VARCHAR(10),
    an_Titel2 VARCHAR(50),
    an_Vorname2 VARCHAR(50),
    an_Nachname2 VARCHAR(50),
    an_Briefanrede VARCHAR(150),
    an_Strasse VARCHAR(50),
    an_PLZ VARCHAR(10),
    an_Ort VARCHAR(50),
    an_Telefon VARCHAR(50),
    an_MobilTelefon VARCHAR(50),
    an_GeschTelefon VARCHAR(50),
    an_Bewerbungsdatum VARCHAR(23),  -- datetime as string
    an_Bestaetigungsdatum VARCHAR(23),
    an_AktuellesAngebot VARCHAR(23),
    an_Loeschdatum VARCHAR(23),
    an_Wunsch VARCHAR(600),
    an_Vermerk VARCHAR(2000),
    an_Aktiv CHAR(1),
    an_DeaktiviertAm VARCHAR(23),
    an_Geburtstag VARCHAR(100),
    an_Geburtstag2 VARCHAR(100),
    an_MobilTelefon2 VARCHAR(50),
    an_EMail VARCHAR(100),
    migration_batch_id INTEGER,
    migration_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE migration_staging.raw_bezirk (
    bez_ID VARCHAR(36),
    bez_Name VARCHAR(10),
    migration_batch_id INTEGER,
    migration_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE migration_staging.raw_bezirke_katasterbezirke (
    bez_Name VARCHAR(10),
    kat_Katasterbezirk VARCHAR(10),
    kat_KatasterbezirkName VARCHAR(50),
    migration_batch_id INTEGER,
    migration_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE migration_staging.raw_eingangsnummer (
    enr_ID VARCHAR(36),
    enr_Bezirk VARCHAR(10),
    enr_Nummer INTEGER,
    enr_Jahr INTEGER,
    migration_batch_id INTEGER,
    migration_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE migration_staging.raw_katasterbezirk (
    kat_ID VARCHAR(36),
    kat_bez_ID VARCHAR(36),
    kat_Katasterbezirk VARCHAR(10),
    kat_KatasterbezirkName VARCHAR(50),
    migration_batch_id INTEGER,
    migration_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE migration_staging.raw_kennungen (
    Kenn_ID VARCHAR(36),
    Kenn_Name VARCHAR(50),
    Kenn_Domaene VARCHAR(50),
    Kenn_pers_ID VARCHAR(36),
    migration_batch_id INTEGER,
    migration_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE migration_staging.raw_mischenfelder (
    misch_ID VARCHAR(36),
    misch_Datenbankfeld VARCHAR(50),
    misch_Dokumentfeld VARCHAR(50),
    misch_Kommentar VARCHAR(100),
    migration_batch_id INTEGER,
    migration_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE migration_staging.raw_personen (
    Pers_ID VARCHAR(36),
    Pers_Anrede VARCHAR(10),
    Pers_Vorname VARCHAR(50),
    Pers_Nachname VARCHAR(50),
    Pers_Nummer VARCHAR(7),
    Pers_Organisationseinheit VARCHAR(10),
    Pers_Zimmer VARCHAR(10),
    Pers_Telefon VARCHAR(10),
    Pers_FAX VARCHAR(10),
    Pers_Email VARCHAR(50),
    Pers_Diktatzeichen VARCHAR(5),
    Pers_Unterschrift VARCHAR(50),
    Pers_Dienstbezeichnung VARCHAR(30),
    Pers_Grp_ID VARCHAR(36),
    Pers_istAdmin CHAR(1),
    Pers_darfAdministration CHAR(1),
    Pers_darfLeistungsgruppen CHAR(1),
    Pers_darfPrioUndSLA CHAR(1),
    Pers_darfKunden CHAR(1),
    Pers_Aktiv CHAR(1),
    migration_batch_id INTEGER,
    migration_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE migration_staging.raw_verlauf (
    verl_ID VARCHAR(36),
    verl_An_ID VARCHAR(36),
    verl_Art VARCHAR(4),
    verl_Datum VARCHAR(23),
    verl_Gemarkung VARCHAR(50),
    verl_Flur VARCHAR(20),
    verl_Parzelle VARCHAR(20),
    verl_Groesse VARCHAR(20),
    verl_Sachbearbeiter VARCHAR(100),
    verl_Hinweis VARCHAR(100),
    verl_Kommentar VARCHAR(255),
    migration_batch_id INTEGER,
    migration_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- =============================================================================
-- DATA CONVERSION FUNCTIONS
-- =============================================================================

-- Convert SQL Server datetime string to PostgreSQL timestamp
CREATE OR REPLACE FUNCTION migration_staging.convert_datetime(
    datetime_string TEXT
) RETURNS TIMESTAMP WITH TIME ZONE AS $$
BEGIN
    IF datetime_string IS NULL OR TRIM(datetime_string) = '' THEN
        RETURN NULL;
    END IF;
    
    -- Handle various SQL Server datetime formats
    BEGIN
        -- Try standard format first: YYYY-MM-DD HH:MM:SS.mmm
        RETURN datetime_string::TIMESTAMP WITH TIME ZONE;
    EXCEPTION WHEN OTHERS THEN
        BEGIN
            -- Try alternative format: DD.MM.YYYY HH:MM:SS
            RETURN TO_TIMESTAMP(datetime_string, 'DD.MM.YYYY HH24:MI:SS');
        EXCEPTION WHEN OTHERS THEN
            -- Log error and return NULL
            RAISE WARNING 'Could not convert datetime string: %', datetime_string;
            RETURN NULL;
        END;
    END;
END;
$$ LANGUAGE plpgsql;

-- Convert SQL Server GUID string to PostgreSQL UUID
CREATE OR REPLACE FUNCTION migration_staging.convert_guid(
    guid_string TEXT
) RETURNS UUID AS $$
BEGIN
    IF guid_string IS NULL OR TRIM(guid_string) = '' THEN
        RETURN NULL;
    END IF;
    
    BEGIN
        RETURN guid_string::UUID;
    EXCEPTION WHEN OTHERS THEN
        RAISE WARNING 'Could not convert GUID string: %', guid_string;
        RETURN NULL;
    END;
END;
$$ LANGUAGE plpgsql;

-- Convert SQL Server bit/char to PostgreSQL boolean
CREATE OR REPLACE FUNCTION migration_staging.convert_boolean(
    bit_char_value TEXT
) RETURNS BOOLEAN AS $$
BEGIN
    IF bit_char_value IS NULL THEN
        RETURN NULL;
    END IF;
    
    CASE UPPER(TRIM(bit_char_value))
        WHEN '1' THEN RETURN TRUE;
        WHEN '0' THEN RETURN FALSE;
        WHEN 'Y' THEN RETURN TRUE;
        WHEN 'N' THEN RETURN FALSE;
        WHEN 'J' THEN RETURN TRUE; -- German "Ja"
        WHEN 'TRUE' THEN RETURN TRUE;
        WHEN 'FALSE' THEN RETURN FALSE;
        ELSE RETURN NULL;
    END CASE;
END;
$$ LANGUAGE plpgsql;

-- Parse German birth date format to DATE
CREATE OR REPLACE FUNCTION migration_staging.convert_birth_date(
    birth_date_string TEXT
) RETURNS DATE AS $$
BEGIN
    IF birth_date_string IS NULL OR TRIM(birth_date_string) = '' THEN
        RETURN NULL;
    END IF;
    
    BEGIN
        -- Try DD.MM.YYYY format
        RETURN TO_DATE(birth_date_string, 'DD.MM.YYYY');
    EXCEPTION WHEN OTHERS THEN
        BEGIN
            -- Try YYYY-MM-DD format
            RETURN birth_date_string::DATE;
        EXCEPTION WHEN OTHERS THEN
            RAISE WARNING 'Could not convert birth date string: %', birth_date_string;
            RETURN NULL;
        END;
    END;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- DATA VALIDATION FUNCTIONS
-- =============================================================================

-- Validate email address
CREATE OR REPLACE FUNCTION migration_staging.validate_email(
    email_address TEXT
) RETURNS TEXT AS $$
BEGIN
    IF email_address IS NULL OR TRIM(email_address) = '' THEN
        RETURN NULL;
    END IF;
    
    IF email_address ~* '^[A-Za-z0-9._%-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$' THEN
        RETURN TRIM(email_address);
    ELSE
        RAISE WARNING 'Invalid email address format: %', email_address;
        RETURN NULL;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Validate and format phone number
CREATE OR REPLACE FUNCTION migration_staging.validate_phone(
    phone_number TEXT
) RETURNS TEXT AS $$
BEGIN
    IF phone_number IS NULL OR TRIM(phone_number) = '' THEN
        RETURN NULL;
    END IF;
    
    -- Remove common formatting characters
    phone_number := REGEXP_REPLACE(phone_number, '[^\+0-9\-\s\(\)]', '', 'g');
    
    IF LENGTH(phone_number) >= 3 AND LENGTH(phone_number) <= 50 THEN
        RETURN TRIM(phone_number);
    ELSE
        RAISE WARNING 'Invalid phone number format: %', phone_number;
        RETURN NULL;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Validate postal code (German format)
CREATE OR REPLACE FUNCTION migration_staging.validate_postal_code(
    postal_code TEXT
) RETURNS TEXT AS $$
BEGIN
    IF postal_code IS NULL OR TRIM(postal_code) = '' THEN
        RETURN NULL;
    END IF;
    
    postal_code := TRIM(postal_code);
    
    IF postal_code ~ '^[0-9]{5}$' THEN
        RETURN postal_code;
    ELSE
        RAISE WARNING 'Invalid postal code format: %', postal_code;
        RETURN NULL;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- MIGRATION LOG TABLE
-- =============================================================================

CREATE TABLE migration_staging.migration_log (
    id BIGSERIAL PRIMARY KEY,
    batch_id INTEGER NOT NULL,
    table_name VARCHAR(50) NOT NULL,
    operation VARCHAR(20) NOT NULL, -- 'EXTRACT', 'TRANSFORM', 'LOAD', 'VALIDATE'
    status VARCHAR(20) NOT NULL,    -- 'SUCCESS', 'ERROR', 'WARNING'
    message TEXT,
    records_processed INTEGER DEFAULT 0,
    records_success INTEGER DEFAULT 0,
    records_error INTEGER DEFAULT 0,
    started_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMP WITH TIME ZONE,
    duration_seconds INTEGER
);

-- Function to log migration steps
CREATE OR REPLACE FUNCTION migration_staging.log_migration_step(
    p_batch_id INTEGER,
    p_table_name VARCHAR(50),
    p_operation VARCHAR(20),
    p_status VARCHAR(20),
    p_message TEXT DEFAULT NULL,
    p_records_processed INTEGER DEFAULT 0,
    p_records_success INTEGER DEFAULT 0,
    p_records_error INTEGER DEFAULT 0
) RETURNS BIGINT AS $$
DECLARE
    v_log_id BIGINT;
BEGIN
    INSERT INTO migration_staging.migration_log (
        batch_id, table_name, operation, status, message,
        records_processed, records_success, records_error
    ) VALUES (
        p_batch_id, p_table_name, p_operation, p_status, p_message,
        p_records_processed, p_records_success, p_records_error
    ) RETURNING id INTO v_log_id;
    
    RETURN v_log_id;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- INDEXES FOR STAGING TABLES
-- =============================================================================

-- Indexes for performance during migration
CREATE INDEX idx_raw_aktenzeichen_batch ON migration_staging.raw_aktenzeichen(migration_batch_id);
CREATE INDEX idx_raw_antrag_batch ON migration_staging.raw_antrag(migration_batch_id);
CREATE INDEX idx_raw_bezirk_batch ON migration_staging.raw_bezirk(migration_batch_id);
CREATE INDEX idx_raw_eingangsnummer_batch ON migration_staging.raw_eingangsnummer(migration_batch_id);
CREATE INDEX idx_raw_katasterbezirk_batch ON migration_staging.raw_katasterbezirk(migration_batch_id);
CREATE INDEX idx_raw_kennungen_batch ON migration_staging.raw_kennungen(migration_batch_id);
CREATE INDEX idx_raw_mischenfelder_batch ON migration_staging.raw_mischenfelder(migration_batch_id);
CREATE INDEX idx_raw_personen_batch ON migration_staging.raw_personen(migration_batch_id);
CREATE INDEX idx_raw_verlauf_batch ON migration_staging.raw_verlauf(migration_batch_id);

-- Indexes for migration log
CREATE INDEX idx_migration_log_batch_table ON migration_staging.migration_log(batch_id, table_name);
CREATE INDEX idx_migration_log_status ON migration_staging.migration_log(status);

-- =============================================================================
-- COMMENTS
-- =============================================================================

COMMENT ON SCHEMA migration_staging IS 'Staging schema for SQL Server to PostgreSQL migration';
COMMENT ON FUNCTION migration_staging.convert_datetime(TEXT) IS 'Converts SQL Server datetime strings to PostgreSQL timestamps';
COMMENT ON FUNCTION migration_staging.convert_guid(TEXT) IS 'Converts SQL Server GUID strings to PostgreSQL UUIDs';
COMMENT ON FUNCTION migration_staging.convert_boolean(TEXT) IS 'Converts SQL Server bit/char values to PostgreSQL booleans';
COMMENT ON FUNCTION migration_staging.validate_email(TEXT) IS 'Validates and formats email addresses';
COMMENT ON FUNCTION migration_staging.validate_phone(TEXT) IS 'Validates and formats phone numbers';
COMMENT ON FUNCTION migration_staging.validate_postal_code(TEXT) IS 'Validates German postal codes';
COMMENT ON TABLE migration_staging.migration_log IS 'Tracks migration progress and issues';