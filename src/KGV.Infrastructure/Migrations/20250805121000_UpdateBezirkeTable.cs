using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KGV.Infrastructure.Migrations;

/// <summary>
/// Migration to update the Bezirke table with additional columns for plot management
/// Enhances district management capabilities with plot count and area tracking
/// </summary>
public partial class UpdateBezirkeTable : Migration
{
    /// <summary>
    /// Apply the migration - Add missing columns to Bezirke table
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Check if columns already exist before adding them
        // This approach follows PostgreSQL 16 best practices for schema evolution
        
        // Add Flaeche column if it doesn't exist
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'flaeche'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN flaeche numeric(10,2) NULL;
                    COMMENT ON COLUMN bezirke.flaeche IS 'Gesamtfläche des Bezirks in Quadratmetern';
                END IF;
            END $$;
        ");

        // Add AnzahlParzellen column if it doesn't exist
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'anzahl_parzellen'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN anzahl_parzellen integer NOT NULL DEFAULT 0;
                    COMMENT ON COLUMN bezirke.anzahl_parzellen IS 'Anzahl der Parzellen in diesem Bezirk';
                END IF;
            END $$;
        ");

        // Add Status column if it doesn't exist
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'status'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN status integer NOT NULL DEFAULT 1;
                    COMMENT ON COLUMN bezirke.status IS 'Status des Bezirks (0=Inaktiv, 1=Aktiv, 2=Gesperrt, 3=Umstrukturierung, 4=Archiviert)';
                END IF;
            END $$;
        ");

        // Add SortOrder column if it doesn't exist
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'sort_order'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN sort_order integer NOT NULL DEFAULT 0;
                    COMMENT ON COLUMN bezirke.sort_order IS 'Sortierreihenfolge für die Anzeige von Bezirken';
                END IF;
            END $$;
        ");

        // Add DisplayName column if it doesn't exist
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'display_name'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN display_name varchar(100) NULL;
                    COMMENT ON COLUMN bezirke.display_name IS 'Vollständiger Anzeigename des Bezirks';
                END IF;
            END $$;
        ");

        // Add Description column if it doesn't exist
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'description'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN description varchar(500) NULL;
                    COMMENT ON COLUMN bezirke.description IS 'Beschreibung des Bezirks';
                END IF;
            END $$;
        ");

        // Add IsActive column if it doesn't exist
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'is_active'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN is_active boolean NOT NULL DEFAULT true;
                    COMMENT ON COLUMN bezirke.is_active IS 'Ob der Bezirk derzeit aktiv ist';
                END IF;
            END $$;
        ");

        // Ensure audit fields exist (following the same pattern as other entities)
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                -- Add CreatedAt if it doesn't exist
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'erstellt_am'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN erstellt_am timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP;
                    COMMENT ON COLUMN bezirke.erstellt_am IS 'Erstellungszeitpunkt';
                END IF;

                -- Add UpdatedAt if it doesn't exist
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'geaendert_am'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN geaendert_am timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP;
                    COMMENT ON COLUMN bezirke.geaendert_am IS 'Letzter Änderungszeitpunkt';
                END IF;

                -- Add CreatedBy if it doesn't exist
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'erstellt_von'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN erstellt_von varchar(100) NOT NULL DEFAULT 'system';
                    COMMENT ON COLUMN bezirke.erstellt_von IS 'Benutzer der den Datensatz erstellt hat';
                END IF;

                -- Add UpdatedBy if it doesn't exist
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'geaendert_von'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN geaendert_von varchar(100) NOT NULL DEFAULT 'system';
                    COMMENT ON COLUMN bezirke.geaendert_von IS 'Benutzer der den Datensatz zuletzt geändert hat';
                END IF;

                -- Add IsDeleted if it doesn't exist (soft delete support)
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'ist_geloescht'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN ist_geloescht boolean NOT NULL DEFAULT false;
                    COMMENT ON COLUMN bezirke.ist_geloescht IS 'Soft-Delete Flag';
                END IF;

                -- Add DeletedAt if it doesn't exist
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'geloescht_am'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN geloescht_am timestamptz NULL;
                    COMMENT ON COLUMN bezirke.geloescht_am IS 'Löschzeitpunkt für Soft-Delete';
                END IF;

                -- Add DeletedBy if it doesn't exist
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'geloescht_von'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN geloescht_von varchar(100) NULL;
                    COMMENT ON COLUMN bezirke.geloescht_von IS 'Benutzer der den Datensatz gelöscht hat';
                END IF;

                -- Add RowVersion if it doesn't exist (optimistic concurrency)
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'bezirke' AND column_name = 'row_version'
                ) THEN
                    ALTER TABLE bezirke ADD COLUMN row_version bytea NOT NULL DEFAULT '\\x00000000';
                    COMMENT ON COLUMN bezirke.row_version IS 'Versionsnummer für optimistische Parallelitätskontrolle';
                END IF;
            END $$;
        ");

        // Add check constraints for business rules (PostgreSQL 16 best practices)
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                -- Check constraint for positive area
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.check_constraints 
                    WHERE constraint_name = 'ck_bezirke_flaeche_positive'
                ) THEN
                    ALTER TABLE bezirke ADD CONSTRAINT ck_bezirke_flaeche_positive 
                    CHECK (flaeche IS NULL OR flaeche > 0.00);
                END IF;

                -- Check constraint for non-negative plot count
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.check_constraints 
                    WHERE constraint_name = 'ck_bezirke_anzahl_parzellen_non_negative'
                ) THEN
                    ALTER TABLE bezirke ADD CONSTRAINT ck_bezirke_anzahl_parzellen_non_negative 
                    CHECK (anzahl_parzellen >= 0);
                END IF;

                -- Check constraint for valid status values
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.check_constraints 
                    WHERE constraint_name = 'ck_bezirke_status_valid'
                ) THEN
                    ALTER TABLE bezirke ADD CONSTRAINT ck_bezirke_status_valid 
                    CHECK (status >= 0 AND status <= 4);
                END IF;

                -- Check constraint for non-empty name
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.check_constraints 
                    WHERE constraint_name = 'ck_bezirke_name_not_empty'
                ) THEN
                    ALTER TABLE bezirke ADD CONSTRAINT ck_bezirke_name_not_empty 
                    CHECK (LENGTH(TRIM(name)) > 0);
                END IF;
            END $$;
        ");

        // Update table comment for better documentation
        migrationBuilder.Sql(@"
            COMMENT ON TABLE bezirke IS 'Bezirke - Administrative Verwaltungsbezirke mit erweiterten Funktionen für Parzellenverwaltung und Flächenberechnung';
        ");
    }

    /// <summary>
    /// Rollback the migration - Remove added columns from Bezirke table
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop check constraints
        migrationBuilder.Sql("ALTER TABLE bezirke DROP CONSTRAINT IF EXISTS ck_bezirke_flaeche_positive;");
        migrationBuilder.Sql("ALTER TABLE bezirke DROP CONSTRAINT IF EXISTS ck_bezirke_anzahl_parzellen_non_negative;");
        migrationBuilder.Sql("ALTER TABLE bezirke DROP CONSTRAINT IF EXISTS ck_bezirke_status_valid;");
        migrationBuilder.Sql("ALTER TABLE bezirke DROP CONSTRAINT IF EXISTS ck_bezirke_name_not_empty;");

        // Drop added columns (in reverse order to avoid dependencies)
        migrationBuilder.DropColumn(name: "row_version", table: "bezirke");
        migrationBuilder.DropColumn(name: "geloescht_von", table: "bezirke");
        migrationBuilder.DropColumn(name: "geloescht_am", table: "bezirke");
        migrationBuilder.DropColumn(name: "ist_geloescht", table: "bezirke");
        migrationBuilder.DropColumn(name: "geaendert_von", table: "bezirke");
        migrationBuilder.DropColumn(name: "erstellt_von", table: "bezirke");
        migrationBuilder.DropColumn(name: "geaendert_am", table: "bezirke");
        migrationBuilder.DropColumn(name: "erstellt_am", table: "bezirke");
        migrationBuilder.DropColumn(name: "is_active", table: "bezirke");
        migrationBuilder.DropColumn(name: "description", table: "bezirke");
        migrationBuilder.DropColumn(name: "display_name", table: "bezirke");
        migrationBuilder.DropColumn(name: "sort_order", table: "bezirke");
        migrationBuilder.DropColumn(name: "status", table: "bezirke");
        migrationBuilder.DropColumn(name: "anzahl_parzellen", table: "bezirke");
        migrationBuilder.DropColumn(name: "flaeche", table: "bezirke");
    }
}