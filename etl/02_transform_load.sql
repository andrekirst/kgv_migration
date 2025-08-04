-- =============================================================================
-- KGV Migration: Transform and Load Process
-- Version: 1.0
-- Description: Transform staging data and load into target schema
-- =============================================================================

-- =============================================================================
-- TRANSFORMATION FUNCTIONS
-- =============================================================================

-- Transform and load districts (Bezirke)
CREATE OR REPLACE FUNCTION migration_staging.transform_load_districts(
    p_batch_id INTEGER
) RETURNS INTEGER AS $$
DECLARE
    v_log_id BIGINT;
    v_processed INTEGER := 0;
    v_success INTEGER := 0;
    v_errors INTEGER := 0;
    v_district RECORD;
BEGIN
    -- Log start
    v_log_id := migration_staging.log_migration_step(
        p_batch_id, 'districts', 'TRANSFORM_LOAD', 'STARTED', 'Starting district transformation'
    );
    
    -- Transform and load districts
    FOR v_district IN 
        SELECT DISTINCT bez_ID, bez_Name 
        FROM migration_staging.raw_bezirk 
        WHERE migration_batch_id = p_batch_id 
          AND bez_Name IS NOT NULL
    LOOP
        BEGIN
            v_processed := v_processed + 1;
            
            INSERT INTO districts (uuid, name, description, is_active)
            VALUES (
                migration_staging.convert_guid(v_district.bez_ID),
                TRIM(v_district.bez_Name),
                'Migrated from legacy system',
                true
            )
            ON CONFLICT (name) DO UPDATE SET
                description = COALESCE(districts.description, 'Migrated from legacy system'),
                updated_at = NOW();
                
            v_success := v_success + 1;
            
        EXCEPTION WHEN OTHERS THEN
            v_errors := v_errors + 1;
            RAISE WARNING 'Error processing district %: %', v_district.bez_Name, SQLERRM;
        END;
    END LOOP;
    
    -- Update log
    UPDATE migration_staging.migration_log 
    SET status = CASE WHEN v_errors = 0 THEN 'SUCCESS' ELSE 'WARNING' END,
        message = FORMAT('Processed %s districts, %s successful, %s errors', v_processed, v_success, v_errors),
        records_processed = v_processed,
        records_success = v_success, 
        records_error = v_errors,
        completed_at = NOW(),
        duration_seconds = EXTRACT(EPOCH FROM (NOW() - started_at))
    WHERE id = v_log_id;
    
    RETURN v_success;
END;
$$ LANGUAGE plpgsql;

-- Transform and load cadastral districts
CREATE OR REPLACE FUNCTION migration_staging.transform_load_cadastral_districts(
    p_batch_id INTEGER
) RETURNS INTEGER AS $$
DECLARE
    v_log_id BIGINT;
    v_processed INTEGER := 0;
    v_success INTEGER := 0;
    v_errors INTEGER := 0;
    v_cadastral RECORD;
    v_district_id BIGINT;
BEGIN
    -- Log start
    v_log_id := migration_staging.log_migration_step(
        p_batch_id, 'cadastral_districts', 'TRANSFORM_LOAD', 'STARTED', 'Starting cadastral district transformation'
    );
    
    -- Transform and load cadastral districts
    FOR v_cadastral IN 
        SELECT DISTINCT kat_ID, kat_bez_ID, kat_Katasterbezirk, kat_KatasterbezirkName
        FROM migration_staging.raw_katasterbezirk 
        WHERE migration_batch_id = p_batch_id 
          AND kat_Katasterbezirk IS NOT NULL
    LOOP
        BEGIN
            v_processed := v_processed + 1;
            
            -- Find corresponding district
            SELECT id INTO v_district_id
            FROM districts 
            WHERE uuid = migration_staging.convert_guid(v_cadastral.kat_bez_ID);
            
            IF v_district_id IS NULL THEN
                -- Try to find by name from junction table
                SELECT d.id INTO v_district_id
                FROM districts d
                JOIN migration_staging.raw_bezirke_katasterbezirke bk 
                    ON d.name = bk.bez_Name
                WHERE bk.kat_Katasterbezirk = v_cadastral.kat_Katasterbezirk
                  AND bk.migration_batch_id = p_batch_id
                LIMIT 1;
            END IF;
            
            IF v_district_id IS NOT NULL THEN
                INSERT INTO cadastral_districts (uuid, district_id, code, name, is_active)
                VALUES (
                    migration_staging.convert_guid(v_cadastral.kat_ID),
                    v_district_id,
                    TRIM(v_cadastral.kat_Katasterbezirk),
                    TRIM(v_cadastral.kat_KatasterbezirkName),
                    true
                )
                ON CONFLICT (district_id, code) DO UPDATE SET
                    name = TRIM(v_cadastral.kat_KatasterbezirkName),
                    updated_at = NOW();
                    
                v_success := v_success + 1;
            ELSE
                RAISE WARNING 'No district found for cadastral district %', v_cadastral.kat_Katasterbezirk;
                v_errors := v_errors + 1;
            END IF;
            
        EXCEPTION WHEN OTHERS THEN
            v_errors := v_errors + 1;
            RAISE WARNING 'Error processing cadastral district %: %', v_cadastral.kat_Katasterbezirk, SQLERRM;
        END;
    END LOOP;
    
    -- Update log
    UPDATE migration_staging.migration_log 
    SET status = CASE WHEN v_errors = 0 THEN 'SUCCESS' ELSE 'WARNING' END,
        message = FORMAT('Processed %s cadastral districts, %s successful, %s errors', v_processed, v_success, v_errors),
        records_processed = v_processed,
        records_success = v_success, 
        records_error = v_errors,
        completed_at = NOW(),
        duration_seconds = EXTRACT(EPOCH FROM (NOW() - started_at))
    WHERE id = v_log_id;
    
    RETURN v_success;
END;
$$ LANGUAGE plpgsql;

-- Transform and load users (Personen)
CREATE OR REPLACE FUNCTION migration_staging.transform_load_users(
    p_batch_id INTEGER
) RETURNS INTEGER AS $$
DECLARE
    v_log_id BIGINT;
    v_processed INTEGER := 0;
    v_success INTEGER := 0;
    v_errors INTEGER := 0;
    v_user RECORD;
BEGIN
    -- Log start
    v_log_id := migration_staging.log_migration_step(
        p_batch_id, 'users', 'TRANSFORM_LOAD', 'STARTED', 'Starting users transformation'
    );
    
    -- Transform and load users
    FOR v_user IN 
        SELECT * FROM migration_staging.raw_personen 
        WHERE migration_batch_id = p_batch_id 
          AND Pers_Vorname IS NOT NULL 
          AND Pers_Nachname IS NOT NULL
    LOOP
        BEGIN
            v_processed := v_processed + 1;
            
            INSERT INTO users (
                uuid, salutation, first_name, last_name, employee_number,
                department, room, phone, fax, email, signature_code, signature_text, job_title,
                is_admin, can_administrate, can_manage_service_groups, 
                can_manage_priorities_sla, can_manage_customers, is_active
            ) VALUES (
                migration_staging.convert_guid(v_user.Pers_ID),
                TRIM(v_user.Pers_Anrede),
                TRIM(v_user.Pers_Vorname),
                TRIM(v_user.Pers_Nachname),
                TRIM(v_user.Pers_Nummer),
                TRIM(v_user.Pers_Organisationseinheit),
                TRIM(v_user.Pers_Zimmer),
                migration_staging.validate_phone(v_user.Pers_Telefon),
                migration_staging.validate_phone(v_user.Pers_FAX),
                migration_staging.validate_email(v_user.Pers_Email),
                TRIM(v_user.Pers_Diktatzeichen),
                TRIM(v_user.Pers_Unterschrift),
                TRIM(v_user.Pers_Dienstbezeichnung),
                migration_staging.convert_boolean(v_user.Pers_istAdmin),
                migration_staging.convert_boolean(v_user.Pers_darfAdministration),
                migration_staging.convert_boolean(v_user.Pers_darfLeistungsgruppen),
                migration_staging.convert_boolean(v_user.Pers_darfPrioUndSLA),
                migration_staging.convert_boolean(v_user.Pers_darfKunden),
                COALESCE(migration_staging.convert_boolean(v_user.Pers_Aktiv), true)
            )
            ON CONFLICT (employee_number) DO UPDATE SET
                salutation = EXCLUDED.salutation,
                first_name = EXCLUDED.first_name,
                last_name = EXCLUDED.last_name,
                department = EXCLUDED.department,
                room = EXCLUDED.room,
                phone = EXCLUDED.phone,
                fax = EXCLUDED.fax,
                email = EXCLUDED.email,
                signature_code = EXCLUDED.signature_code,
                signature_text = EXCLUDED.signature_text,
                job_title = EXCLUDED.job_title,
                is_admin = EXCLUDED.is_admin,
                can_administrate = EXCLUDED.can_administrate,
                can_manage_service_groups = EXCLUDED.can_manage_service_groups,
                can_manage_priorities_sla = EXCLUDED.can_manage_priorities_sla,
                can_manage_customers = EXCLUDED.can_manage_customers,
                is_active = EXCLUDED.is_active,
                updated_at = NOW();
                
            v_success := v_success + 1;
            
        EXCEPTION WHEN OTHERS THEN
            v_errors := v_errors + 1;
            RAISE WARNING 'Error processing user % %: %', v_user.Pers_Vorname, v_user.Pers_Nachname, SQLERRM;
        END;
    END LOOP;
    
    -- Update log
    UPDATE migration_staging.migration_log 
    SET status = CASE WHEN v_errors = 0 THEN 'SUCCESS' ELSE 'WARNING' END,
        message = FORMAT('Processed %s users, %s successful, %s errors', v_processed, v_success, v_errors),
        records_processed = v_processed,
        records_success = v_success, 
        records_error = v_errors,
        completed_at = NOW(),
        duration_seconds = EXTRACT(EPOCH FROM (NOW() - started_at))
    WHERE id = v_log_id;
    
    RETURN v_success;
END;
$$ LANGUAGE plpgsql;

-- Transform and load file references
CREATE OR REPLACE FUNCTION migration_staging.transform_load_file_references(
    p_batch_id INTEGER
) RETURNS INTEGER AS $$
DECLARE
    v_log_id BIGINT;
    v_processed INTEGER := 0;
    v_success INTEGER := 0;
    v_errors INTEGER := 0;
    v_file_ref RECORD;
BEGIN
    -- Log start
    v_log_id := migration_staging.log_migration_step(
        p_batch_id, 'file_references', 'TRANSFORM_LOAD', 'STARTED', 'Starting file references transformation'
    );
    
    -- Transform and load file references
    FOR v_file_ref IN 
        SELECT * FROM migration_staging.raw_aktenzeichen 
        WHERE migration_batch_id = p_batch_id 
          AND az_Bezirk IS NOT NULL 
          AND az_Nummer IS NOT NULL 
          AND az_Jahr IS NOT NULL
    LOOP
        BEGIN
            v_processed := v_processed + 1;
            
            INSERT INTO file_references (uuid, district_code, number, year, is_active)
            VALUES (
                migration_staging.convert_guid(v_file_ref.az_ID),
                TRIM(v_file_ref.az_Bezirk),
                v_file_ref.az_Nummer,
                v_file_ref.az_Jahr,
                true
            )
            ON CONFLICT (district_code, number, year) DO UPDATE SET
                updated_at = NOW();
                
            v_success := v_success + 1;
            
        EXCEPTION WHEN OTHERS THEN
            v_errors := v_errors + 1;
            RAISE WARNING 'Error processing file reference %-%-%: %', 
                v_file_ref.az_Bezirk, v_file_ref.az_Nummer, v_file_ref.az_Jahr, SQLERRM;
        END;
    END LOOP;
    
    -- Update log
    UPDATE migration_staging.migration_log 
    SET status = CASE WHEN v_errors = 0 THEN 'SUCCESS' ELSE 'WARNING' END,
        message = FORMAT('Processed %s file references, %s successful, %s errors', v_processed, v_success, v_errors),
        records_processed = v_processed,
        records_success = v_success, 
        records_error = v_errors,
        completed_at = NOW(),
        duration_seconds = EXTRACT(EPOCH FROM (NOW() - started_at))
    WHERE id = v_log_id;
    
    RETURN v_success;
END;
$$ LANGUAGE plpgsql;

-- Transform and load entry numbers
CREATE OR REPLACE FUNCTION migration_staging.transform_load_entry_numbers(
    p_batch_id INTEGER
) RETURNS INTEGER AS $$
DECLARE
    v_log_id BIGINT;
    v_processed INTEGER := 0;
    v_success INTEGER := 0;
    v_errors INTEGER := 0;
    v_entry_num RECORD;
BEGIN
    -- Log start
    v_log_id := migration_staging.log_migration_step(
        p_batch_id, 'entry_numbers', 'TRANSFORM_LOAD', 'STARTED', 'Starting entry numbers transformation'
    );
    
    -- Transform and load entry numbers
    FOR v_entry_num IN 
        SELECT * FROM migration_staging.raw_eingangsnummer 
        WHERE migration_batch_id = p_batch_id 
          AND enr_Bezirk IS NOT NULL 
          AND enr_Nummer IS NOT NULL 
          AND enr_Jahr IS NOT NULL
    LOOP
        BEGIN
            v_processed := v_processed + 1;
            
            INSERT INTO entry_numbers (uuid, district_code, number, year, is_active)
            VALUES (
                migration_staging.convert_guid(v_entry_num.enr_ID),
                TRIM(v_entry_num.enr_Bezirk),
                v_entry_num.enr_Nummer,
                v_entry_num.enr_Jahr,
                true
            )
            ON CONFLICT (district_code, number, year) DO UPDATE SET
                updated_at = NOW();
                
            v_success := v_success + 1;
            
        EXCEPTION WHEN OTHERS THEN
            v_errors := v_errors + 1;
            RAISE WARNING 'Error processing entry number %-%-%: %', 
                v_entry_num.enr_Bezirk, v_entry_num.enr_Nummer, v_entry_num.enr_Jahr, SQLERRM;
        END;
    END LOOP;
    
    -- Update log
    UPDATE migration_staging.migration_log 
    SET status = CASE WHEN v_errors = 0 THEN 'SUCCESS' ELSE 'WARNING' END,
        message = FORMAT('Processed %s entry numbers, %s successful, %s errors', v_processed, v_success, v_errors),
        records_processed = v_processed,
        records_success = v_success, 
        records_error = v_errors,
        completed_at = NOW(),
        duration_seconds = EXTRACT(EPOCH FROM (NOW() - started_at))
    WHERE id = v_log_id;
    
    RETURN v_success;
END;
$$ LANGUAGE plpgsql;

-- Transform and load applications (most complex transformation)
CREATE OR REPLACE FUNCTION migration_staging.transform_load_applications(
    p_batch_id INTEGER
) RETURNS INTEGER AS $$
DECLARE
    v_log_id BIGINT;
    v_processed INTEGER := 0;
    v_success INTEGER := 0;
    v_errors INTEGER := 0;
    v_app RECORD;
BEGIN
    -- Log start
    v_log_id := migration_staging.log_migration_step(
        p_batch_id, 'applications', 'TRANSFORM_LOAD', 'STARTED', 'Starting applications transformation'
    );
    
    -- Transform and load applications
    FOR v_app IN 
        SELECT * FROM migration_staging.raw_antrag 
        WHERE migration_batch_id = p_batch_id
    LOOP
        BEGIN
            v_processed := v_processed + 1;
            
            INSERT INTO applications (
                uuid, file_reference, waiting_list_number_32, waiting_list_number_33,
                salutation, title, first_name, last_name, birth_date,
                salutation_2, title_2, first_name_2, last_name_2, birth_date_2,
                letter_salutation, street, postal_code, city,
                phone, mobile_phone, mobile_phone_2, business_phone, email,
                application_date, confirmation_date, current_offer_date, deletion_date, deactivated_at,
                preferences, remarks, is_active
            ) VALUES (
                migration_staging.convert_guid(v_app.an_ID),
                TRIM(v_app.an_Aktenzeichen),
                TRIM(v_app.an_WartelistenNr32),
                TRIM(v_app.an_WartelistenNr33),
                TRIM(v_app.an_Anrede),
                TRIM(v_app.an_Titel),
                TRIM(v_app.an_Vorname),
                TRIM(v_app.an_Nachname),
                migration_staging.convert_birth_date(v_app.an_Geburtstag),
                TRIM(v_app.an_Anrede2),
                TRIM(v_app.an_Titel2),
                TRIM(v_app.an_Vorname2),
                TRIM(v_app.an_Nachname2),
                migration_staging.convert_birth_date(v_app.an_Geburtstag2),
                TRIM(v_app.an_Briefanrede),
                TRIM(v_app.an_Strasse),
                migration_staging.validate_postal_code(v_app.an_PLZ),
                TRIM(v_app.an_Ort),
                migration_staging.validate_phone(v_app.an_Telefon),
                migration_staging.validate_phone(v_app.an_MobilTelefon),
                migration_staging.validate_phone(v_app.an_MobilTelefon2),
                migration_staging.validate_phone(v_app.an_GeschTelefon),
                migration_staging.validate_email(v_app.an_EMail),
                migration_staging.convert_datetime(v_app.an_Bewerbungsdatum)::DATE,
                migration_staging.convert_datetime(v_app.an_Bestaetigungsdatum)::DATE,
                migration_staging.convert_datetime(v_app.an_AktuellesAngebot)::DATE,
                migration_staging.convert_datetime(v_app.an_Loeschdatum)::DATE,
                migration_staging.convert_datetime(v_app.an_DeaktiviertAm),
                TRIM(v_app.an_Wunsch),
                TRIM(v_app.an_Vermerk),
                COALESCE(migration_staging.convert_boolean(v_app.an_Aktiv), true)
            );
            
            v_success := v_success + 1;
            
        EXCEPTION WHEN OTHERS THEN
            v_errors := v_errors + 1;
            RAISE WARNING 'Error processing application % % (ID: %): %', 
                v_app.an_Vorname, v_app.an_Nachname, v_app.an_ID, SQLERRM;
        END;
    END LOOP;
    
    -- Update log
    UPDATE migration_staging.migration_log 
    SET status = CASE WHEN v_errors = 0 THEN 'SUCCESS' ELSE 'WARNING' END,
        message = FORMAT('Processed %s applications, %s successful, %s errors', v_processed, v_success, v_errors),
        records_processed = v_processed,
        records_success = v_success, 
        records_error = v_errors,
        completed_at = NOW(),
        duration_seconds = EXTRACT(EPOCH FROM (NOW() - started_at))
    WHERE id = v_log_id;
    
    RETURN v_success;
END;
$$ LANGUAGE plpgsql;

-- Transform and load application history
CREATE OR REPLACE FUNCTION migration_staging.transform_load_application_history(
    p_batch_id INTEGER
) RETURNS INTEGER AS $$
DECLARE
    v_log_id BIGINT;
    v_processed INTEGER := 0;
    v_success INTEGER := 0;
    v_errors INTEGER := 0;
    v_history RECORD;
    v_app_id BIGINT;
BEGIN
    -- Log start
    v_log_id := migration_staging.log_migration_step(
        p_batch_id, 'application_history', 'TRANSFORM_LOAD', 'STARTED', 'Starting application history transformation'
    );
    
    -- Transform and load application history
    FOR v_history IN 
        SELECT * FROM migration_staging.raw_verlauf 
        WHERE migration_batch_id = p_batch_id 
          AND verl_An_ID IS NOT NULL
    LOOP
        BEGIN
            v_processed := v_processed + 1;
            
            -- Find corresponding application
            SELECT id INTO v_app_id
            FROM applications 
            WHERE uuid = migration_staging.convert_guid(v_history.verl_An_ID);
            
            IF v_app_id IS NOT NULL THEN
                INSERT INTO application_history (
                    uuid, application_id, action_type, action_date,
                    gemarkung, flur, parcel, size_info,
                    case_worker, note, comment
                ) VALUES (
                    migration_staging.convert_guid(v_history.verl_ID),
                    v_app_id,
                    COALESCE(TRIM(v_history.verl_Art), 'UPD'),
                    COALESCE(migration_staging.convert_datetime(v_history.verl_Datum), NOW()),
                    TRIM(v_history.verl_Gemarkung),
                    TRIM(v_history.verl_Flur),
                    TRIM(v_history.verl_Parzelle),
                    TRIM(v_history.verl_Groesse),
                    TRIM(v_history.verl_Sachbearbeiter),
                    TRIM(v_history.verl_Hinweis),
                    TRIM(v_history.verl_Kommentar)
                );
                
                v_success := v_success + 1;
            ELSE
                RAISE WARNING 'No application found for history record %', v_history.verl_An_ID;
                v_errors := v_errors + 1;
            END IF;
            
        EXCEPTION WHEN OTHERS THEN
            v_errors := v_errors + 1;
            RAISE WARNING 'Error processing application history %: %', v_history.verl_ID, SQLERRM;
        END;
    END LOOP;
    
    -- Update log
    UPDATE migration_staging.migration_log 
    SET status = CASE WHEN v_errors = 0 THEN 'SUCCESS' ELSE 'WARNING' END,
        message = FORMAT('Processed %s history records, %s successful, %s errors', v_processed, v_success, v_errors),
        records_processed = v_processed,
        records_success = v_success, 
        records_error = v_errors,
        completed_at = NOW(),
        duration_seconds = EXTRACT(EPOCH FROM (NOW() - started_at))
    WHERE id = v_log_id;
    
    RETURN v_success;
END;
$$ LANGUAGE plpgsql;

-- Transform and load other entities (identifiers, field mappings)
CREATE OR REPLACE FUNCTION migration_staging.transform_load_misc_entities(
    p_batch_id INTEGER
) RETURNS INTEGER AS $$
DECLARE
    v_log_id BIGINT;
    v_processed INTEGER := 0;
    v_success INTEGER := 0;
    v_errors INTEGER := 0;
    v_record RECORD;
    v_user_id BIGINT;
BEGIN
    -- Log start
    v_log_id := migration_staging.log_migration_step(
        p_batch_id, 'misc_entities', 'TRANSFORM_LOAD', 'STARTED', 'Starting misc entities transformation'
    );
    
    -- Transform identifiers
    FOR v_record IN 
        SELECT * FROM migration_staging.raw_kennungen 
        WHERE migration_batch_id = p_batch_id
    LOOP
        BEGIN
            v_processed := v_processed + 1;
            
            -- Find user if exists
            v_user_id := NULL;
            IF v_record.Kenn_pers_ID IS NOT NULL THEN
                SELECT id INTO v_user_id
                FROM users 
                WHERE uuid = migration_staging.convert_guid(v_record.Kenn_pers_ID);
            END IF;
            
            INSERT INTO identifiers (uuid, name, domain, user_id, is_active)
            VALUES (
                migration_staging.convert_guid(v_record.Kenn_ID),
                TRIM(v_record.Kenn_Name),
                TRIM(v_record.Kenn_Domaene),
                v_user_id,
                true
            )
            ON CONFLICT (name, domain) DO UPDATE SET
                user_id = EXCLUDED.user_id,
                updated_at = NOW();
                
            v_success := v_success + 1;
            
        EXCEPTION WHEN OTHERS THEN
            v_errors := v_errors + 1;
            RAISE WARNING 'Error processing identifier %: %', v_record.Kenn_Name, SQLERRM;
        END;
    END LOOP;
    
    -- Transform field mappings
    FOR v_record IN 
        SELECT * FROM migration_staging.raw_mischenfelder 
        WHERE migration_batch_id = p_batch_id
    LOOP
        BEGIN
            v_processed := v_processed + 1;
            
            INSERT INTO field_mappings (uuid, database_field, document_field, comment, is_active)
            VALUES (
                migration_staging.convert_guid(v_record.misch_ID),
                TRIM(v_record.misch_Datenbankfeld),
                TRIM(v_record.misch_Dokumentfeld),
                TRIM(v_record.misch_Kommentar),
                true
            )
            ON CONFLICT (database_field) DO UPDATE SET
                document_field = EXCLUDED.document_field,
                comment = EXCLUDED.comment,
                updated_at = NOW();
                
            v_success := v_success + 1;
            
        EXCEPTION WHEN OTHERS THEN
            v_errors := v_errors + 1;
            RAISE WARNING 'Error processing field mapping %: %', v_record.misch_Datenbankfeld, SQLERRM;
        END;
    END LOOP;
    
    -- Update log
    UPDATE migration_staging.migration_log 
    SET status = CASE WHEN v_errors = 0 THEN 'SUCCESS' ELSE 'WARNING' END,
        message = FORMAT('Processed %s misc entities, %s successful, %s errors', v_processed, v_success, v_errors),
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
-- MASTER TRANSFORMATION ORCHESTRATOR
-- =============================================================================

-- Main function to orchestrate the entire transformation process
CREATE OR REPLACE FUNCTION migration_staging.run_full_migration(
    p_batch_id INTEGER
) RETURNS TABLE(
    entity VARCHAR(50),
    status VARCHAR(20),
    records_processed INTEGER,
    records_success INTEGER,
    records_error INTEGER,
    duration_seconds INTEGER
) AS $$
DECLARE
    v_start_time TIMESTAMP WITH TIME ZONE := NOW();
    v_results RECORD;
BEGIN
    RAISE NOTICE 'Starting full migration for batch %', p_batch_id;
    
    -- Execute transformations in dependency order
    PERFORM migration_staging.transform_load_districts(p_batch_id);
    PERFORM migration_staging.transform_load_cadastral_districts(p_batch_id);
    PERFORM migration_staging.transform_load_users(p_batch_id);
    PERFORM migration_staging.transform_load_file_references(p_batch_id);
    PERFORM migration_staging.transform_load_entry_numbers(p_batch_id);
    PERFORM migration_staging.transform_load_applications(p_batch_id);
    PERFORM migration_staging.transform_load_application_history(p_batch_id);
    PERFORM migration_staging.transform_load_misc_entities(p_batch_id);
    
    -- Return summary
    RETURN QUERY
    SELECT 
        CASE 
            WHEN table_name = 'districts' THEN 'Districts'
            WHEN table_name = 'cadastral_districts' THEN 'Cadastral Districts'
            WHEN table_name = 'users' THEN 'Users'
            WHEN table_name = 'file_references' THEN 'File References'
            WHEN table_name = 'entry_numbers' THEN 'Entry Numbers'
            WHEN table_name = 'applications' THEN 'Applications'
            WHEN table_name = 'application_history' THEN 'Application History'
            WHEN table_name = 'misc_entities' THEN 'Misc Entities'
            ELSE table_name
        END as entity,
        ml.status,
        ml.records_processed,
        ml.records_success,
        ml.records_error,
        ml.duration_seconds
    FROM migration_staging.migration_log ml
    WHERE ml.batch_id = p_batch_id
      AND ml.operation = 'TRANSFORM_LOAD'
    ORDER BY ml.completed_at;
    
    RAISE NOTICE 'Full migration completed for batch % in % seconds', 
        p_batch_id, EXTRACT(EPOCH FROM (NOW() - v_start_time));
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- COMMENTS
-- =============================================================================

COMMENT ON FUNCTION migration_staging.transform_load_districts(INTEGER) IS 'Transforms and loads district data from staging';
COMMENT ON FUNCTION migration_staging.transform_load_cadastral_districts(INTEGER) IS 'Transforms and loads cadastral district data from staging';
COMMENT ON FUNCTION migration_staging.transform_load_users(INTEGER) IS 'Transforms and loads user/personnel data from staging';
COMMENT ON FUNCTION migration_staging.transform_load_file_references(INTEGER) IS 'Transforms and loads file reference data from staging';
COMMENT ON FUNCTION migration_staging.transform_load_entry_numbers(INTEGER) IS 'Transforms and loads entry number data from staging';
COMMENT ON FUNCTION migration_staging.transform_load_applications(INTEGER) IS 'Transforms and loads application data from staging';
COMMENT ON FUNCTION migration_staging.transform_load_application_history(INTEGER) IS 'Transforms and loads application history from staging';
COMMENT ON FUNCTION migration_staging.transform_load_misc_entities(INTEGER) IS 'Transforms and loads identifiers and field mappings from staging';
COMMENT ON FUNCTION migration_staging.run_full_migration(INTEGER) IS 'Orchestrates the complete migration process for a batch';