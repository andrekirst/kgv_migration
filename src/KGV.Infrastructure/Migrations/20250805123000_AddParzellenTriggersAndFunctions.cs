using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KGV.Infrastructure.Migrations;

/// <summary>
/// Migration to add PostgreSQL triggers and functions for Parzellen table
/// Implements automatic timestamp updates, data validation, and business logic enforcement
/// </summary>
public partial class AddParzellenTriggersAndFunctions : Migration
{
    /// <summary>
    /// Apply the migration - Create triggers and functions for Parzellen business logic
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create function for automatic timestamp updates (PostgreSQL 16 best practice)
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION update_modified_timestamp()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.geaendert_am = CURRENT_TIMESTAMP;
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
        ");

        // Create trigger for automatic timestamp updates on Parzellen
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM pg_trigger 
                    WHERE tgname = 'tr_parzellen_update_timestamp'
                ) THEN
                    CREATE TRIGGER tr_parzellen_update_timestamp
                        BEFORE UPDATE ON parzellen
                        FOR EACH ROW
                        EXECUTE FUNCTION update_modified_timestamp();
                END IF;
            END $$;
        ");

        // Create trigger for automatic timestamp updates on Bezirke
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM pg_trigger 
                    WHERE tgname = 'tr_bezirke_update_timestamp'
                ) THEN
                    CREATE TRIGGER tr_bezirke_update_timestamp
                        BEFORE UPDATE ON bezirke
                        FOR EACH ROW
                        EXECUTE FUNCTION update_modified_timestamp();
                END IF;
            END $$;
        ");

        // Create function for plot count maintenance
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION maintain_bezirk_plot_count()
            RETURNS TRIGGER AS $$
            BEGIN
                -- Handle INSERT
                IF TG_OP = 'INSERT' THEN
                    UPDATE bezirke 
                    SET anzahl_parzellen = anzahl_parzellen + 1,
                        geaendert_am = CURRENT_TIMESTAMP
                    WHERE id = NEW.bezirk_id;
                    RETURN NEW;
                END IF;

                -- Handle UPDATE (district change)
                IF TG_OP = 'UPDATE' THEN
                    -- If district changed
                    IF OLD.bezirk_id != NEW.bezirk_id THEN
                        -- Decrease count in old district
                        UPDATE bezirke 
                        SET anzahl_parzellen = GREATEST(0, anzahl_parzellen - 1),
                            geaendert_am = CURRENT_TIMESTAMP
                        WHERE id = OLD.bezirk_id;
                        
                        -- Increase count in new district
                        UPDATE bezirke 
                        SET anzahl_parzellen = anzahl_parzellen + 1,
                            geaendert_am = CURRENT_TIMESTAMP
                        WHERE id = NEW.bezirk_id;
                    END IF;
                    RETURN NEW;
                END IF;

                -- Handle DELETE (including soft delete)
                IF TG_OP = 'DELETE' OR (TG_OP = 'UPDATE' AND NEW.ist_geloescht = true AND OLD.ist_geloescht = false) THEN
                    UPDATE bezirke 
                    SET anzahl_parzellen = GREATEST(0, anzahl_parzellen - 1),
                        geaendert_am = CURRENT_TIMESTAMP
                    WHERE id = COALESCE(OLD.bezirk_id, NEW.bezirk_id);
                    
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    ELSE
                        RETURN NEW;
                    END IF;
                END IF;

                -- Handle restore from soft delete
                IF TG_OP = 'UPDATE' AND OLD.ist_geloescht = true AND NEW.ist_geloescht = false THEN
                    UPDATE bezirke 
                    SET anzahl_parzellen = anzahl_parzellen + 1,
                        geaendert_am = CURRENT_TIMESTAMP
                    WHERE id = NEW.bezirk_id;
                    RETURN NEW;
                END IF;

                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
        ");

        // Create trigger for plot count maintenance
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM pg_trigger 
                    WHERE tgname = 'tr_parzellen_maintain_count'
                ) THEN
                    CREATE TRIGGER tr_parzellen_maintain_count
                        AFTER INSERT OR UPDATE OR DELETE ON parzellen
                        FOR EACH ROW
                        EXECUTE FUNCTION maintain_bezirk_plot_count();
                END IF;
            END $$;
        ");

        // Create function for business rule validation
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION validate_parzelle_business_rules()
            RETURNS TRIGGER AS $$
            BEGIN
                -- Validate plot number format (German standards)
                IF NEW.nummer !~ '^[A-Z0-9\-/]+$' THEN
                    RAISE EXCEPTION 'Parzellennummer muss aus Großbuchstaben, Zahlen und Bindestrichen bestehen: %', NEW.nummer;
                END IF;

                -- Validate assignment date logic
                IF NEW.status = 2 AND NEW.vergeben_am IS NULL THEN
                    NEW.vergeben_am = CURRENT_TIMESTAMP;
                ELSIF NEW.status != 2 AND NEW.vergeben_am IS NOT NULL THEN
                    NEW.vergeben_am = NULL;
                END IF;

                -- Validate district exists and is active
                IF NOT EXISTS (
                    SELECT 1 FROM bezirke 
                    WHERE id = NEW.bezirk_id 
                    AND ist_geloescht = false 
                    AND (status = 1 OR status = 3) -- Active or UnderRestructuring
                ) THEN
                    RAISE EXCEPTION 'Bezirk existiert nicht oder ist nicht aktiv: %', NEW.bezirk_id;
                END IF;

                -- Validate unique plot number within district (excluding soft-deleted)
                IF EXISTS (
                    SELECT 1 FROM parzellen 
                    WHERE bezirk_id = NEW.bezirk_id 
                    AND nummer = NEW.nummer 
                    AND id != NEW.id 
                    AND ist_geloescht = false
                ) THEN
                    RAISE EXCEPTION 'Parzellennummer bereits vorhanden in diesem Bezirk: %', NEW.nummer;
                END IF;

                -- Normalize text fields
                NEW.nummer = UPPER(TRIM(NEW.nummer));
                NEW.beschreibung = TRIM(NEW.beschreibung);
                NEW.besonderheiten = TRIM(NEW.besonderheiten);

                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
        ");

        // Create trigger for business rule validation
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM pg_trigger 
                    WHERE tgname = 'tr_parzellen_validate_rules'
                ) THEN
                    CREATE TRIGGER tr_parzellen_validate_rules
                        BEFORE INSERT OR UPDATE ON parzellen
                        FOR EACH ROW
                        EXECUTE FUNCTION validate_parzelle_business_rules();
                END IF;
            END $$;
        ");

        // Create function for audit logging
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION log_parzelle_changes()
            RETURNS TRIGGER AS $$
            DECLARE
                audit_data jsonb;
                operation_type text;
            BEGIN
                -- Determine operation type
                IF TG_OP = 'INSERT' THEN
                    operation_type = 'INSERT';
                    audit_data = row_to_json(NEW)::jsonb;
                ELSIF TG_OP = 'UPDATE' THEN
                    operation_type = 'UPDATE';
                    audit_data = jsonb_build_object(
                        'old', row_to_json(OLD)::jsonb,
                        'new', row_to_json(NEW)::jsonb
                    );
                ELSIF TG_OP = 'DELETE' THEN
                    operation_type = 'DELETE';
                    audit_data = row_to_json(OLD)::jsonb;
                END IF;

                -- Log significant changes (status changes, assignments, etc.)
                IF TG_OP = 'INSERT' OR 
                   (TG_OP = 'UPDATE' AND (
                       OLD.status != NEW.status OR 
                       OLD.vergeben_am IS DISTINCT FROM NEW.vergeben_am OR
                       OLD.preis IS DISTINCT FROM NEW.preis OR
                       OLD.ist_geloescht != NEW.ist_geloescht
                   )) OR 
                   TG_OP = 'DELETE' THEN
                    
                    -- Create audit log entry
                    -- In a real system, you would insert into an audit table
                    RAISE NOTICE 'AUDIT: % on parzellen table - ID: %, Operation: %, Data: %', 
                        CURRENT_TIMESTAMP, 
                        COALESCE(NEW.id, OLD.id), 
                        operation_type, 
                        audit_data;
                END IF;

                IF TG_OP = 'DELETE' THEN
                    RETURN OLD;
                ELSE
                    RETURN NEW;
                END IF;
            END;
            $$ LANGUAGE plpgsql;
        ");

        // Create trigger for audit logging
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM pg_trigger 
                    WHERE tgname = 'tr_parzellen_audit_log'
                ) THEN
                    CREATE TRIGGER tr_parzellen_audit_log
                        AFTER INSERT OR UPDATE OR DELETE ON parzellen
                        FOR EACH ROW
                        EXECUTE FUNCTION log_parzelle_changes();
                END IF;
            END $$;
        ");

        // Create function for data quality maintenance
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION maintain_data_quality()
            RETURNS void AS $$
            BEGIN
                -- Update plot counts for all districts (data consistency check)
                UPDATE bezirke 
                SET anzahl_parzellen = (
                    SELECT COUNT(*) 
                    FROM parzellen p 
                    WHERE p.bezirk_id = bezirke.id 
                    AND p.ist_geloescht = false
                ),
                geaendert_am = CURRENT_TIMESTAMP
                WHERE ist_geloescht = false;

                -- Clean up orphaned assignment dates
                UPDATE parzellen 
                SET vergeben_am = NULL,
                    geaendert_am = CURRENT_TIMESTAMP
                WHERE status != 2 AND vergeben_am IS NOT NULL;

                -- Set assignment dates for assigned plots without dates
                UPDATE parzellen 
                SET vergeben_am = CURRENT_TIMESTAMP,
                    geaendert_am = CURRENT_TIMESTAMP
                WHERE status = 2 AND vergeben_am IS NULL;

                RAISE NOTICE 'Data quality maintenance completed at %', CURRENT_TIMESTAMP;
            END;
            $$ LANGUAGE plpgsql;
        ");

        // Update table comments with trigger information
        migrationBuilder.Sql(@"
            COMMENT ON TABLE parzellen IS 'Parzellen (Gartenparzellen) - Einzelne Gartenparzellen mit automatischen Triggern für Zeitstempel, Datenvalidierung und Bezirkszählung';
        ");

        migrationBuilder.Sql(@"
            COMMENT ON TRIGGER tr_parzellen_update_timestamp ON parzellen IS 'Automatische Aktualisierung des geaendert_am Zeitstempels';
            COMMENT ON TRIGGER tr_parzellen_maintain_count ON parzellen IS 'Automatische Wartung der Parzellenzählung in Bezirken';
            COMMENT ON TRIGGER tr_parzellen_validate_rules ON parzellen IS 'Geschäftsregeln-Validierung für Parzellendaten';
            COMMENT ON TRIGGER tr_parzellen_audit_log ON parzellen IS 'Audit-Protokollierung für Parzellen-Änderungen';
        ");
    }

    /// <summary>
    /// Rollback the migration - Drop triggers and functions
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop triggers first (in reverse order)
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS tr_parzellen_audit_log ON parzellen;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS tr_parzellen_validate_rules ON parzellen;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS tr_parzellen_maintain_count ON parzellen;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS tr_parzellen_update_timestamp ON parzellen;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS tr_bezirke_update_timestamp ON bezirke;");

        // Drop functions
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS maintain_data_quality();");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS log_parzelle_changes();");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS validate_parzelle_business_rules();");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS maintain_bezirk_plot_count();");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_modified_timestamp();");
    }
}