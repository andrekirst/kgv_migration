-- =====================================================================================
-- KGV Bezirksverwaltung (District Management) - PostgreSQL 16 Deployment Script
-- =====================================================================================
-- This script implements comprehensive district and plot management for KGV system
-- Following PostgreSQL 16 best practices with German localization and EF Core 9 support
-- 
-- Author: KGV Migration System
-- Date: 2025-08-05
-- Version: 1.0
-- Database: PostgreSQL 16+
-- =====================================================================================

BEGIN;

-- Enable required PostgreSQL extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "unaccent";

-- =====================================================================================
-- 1. CREATE PARZELLEN TABLE
-- =====================================================================================

CREATE TABLE IF NOT EXISTS parzellen (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nummer VARCHAR(20) NOT NULL,
    bezirk_id UUID NOT NULL,
    flaeche NUMERIC(10,2) NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    preis NUMERIC(10,2),
    vergeben_am TIMESTAMPTZ,
    beschreibung VARCHAR(1000),
    besonderheiten VARCHAR(500),
    has_wasser BOOLEAN NOT NULL DEFAULT false,
    has_strom BOOLEAN NOT NULL DEFAULT false,
    prioritaet INTEGER NOT NULL DEFAULT 0,
    erstellt_am TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    geaendert_am TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    erstellt_von VARCHAR(100) NOT NULL DEFAULT 'system',
    geaendert_von VARCHAR(100) NOT NULL DEFAULT 'system',
    ist_geloescht BOOLEAN NOT NULL DEFAULT false,
    geloescht_am TIMESTAMPTZ,
    geloescht_von VARCHAR(100),
    row_version BYTEA NOT NULL DEFAULT '\\x00000001'::bytea
);

-- Add table comment
COMMENT ON TABLE parzellen IS 'Parzellen (Gartenparzellen) - Einzelne Gartenparzellen innerhalb eines Bezirks mit vollständiger Verwaltung und Ausstattungsinformationen';

-- Add column comments
COMMENT ON COLUMN parzellen.id IS 'Eindeutige ID der Parzelle';
COMMENT ON COLUMN parzellen.nummer IS 'Parzellennummer innerhalb des Bezirks';
COMMENT ON COLUMN parzellen.bezirk_id IS 'Referenz zum übergeordneten Bezirk';
COMMENT ON COLUMN parzellen.flaeche IS 'Fläche der Parzelle in Quadratmetern';
COMMENT ON COLUMN parzellen.status IS 'Status der Parzelle (0=Verfügbar, 1=Reserviert, 2=Vergeben, etc.)';
COMMENT ON COLUMN parzellen.preis IS 'Preis oder Pachtkosten für die Parzelle';
COMMENT ON COLUMN parzellen.vergeben_am IS 'Datum der Zuteilung falls vergeben';
COMMENT ON COLUMN parzellen.beschreibung IS 'Zusätzliche Notizen oder Beschreibung der Parzelle';
COMMENT ON COLUMN parzellen.besonderheiten IS 'Besondere Merkmale oder Eigenschaften der Parzelle';
COMMENT ON COLUMN parzellen.has_wasser IS 'Verfügbarkeit von Wasseranschluss';
COMMENT ON COLUMN parzellen.has_strom IS 'Verfügbarkeit von Stromanschluss';
COMMENT ON COLUMN parzellen.prioritaet IS 'Prioritätsstufe für Zuteilung (höhere Zahlen = höhere Priorität)';
COMMENT ON COLUMN parzellen.erstellt_am IS 'Erstellungszeitpunkt';
COMMENT ON COLUMN parzellen.geaendert_am IS 'Letzter Änderungszeitpunkt';
COMMENT ON COLUMN parzellen.erstellt_von IS 'Benutzer der den Datensatz erstellt hat';
COMMENT ON COLUMN parzellen.geaendert_von IS 'Benutzer der den Datensatz zuletzt geändert hat';
COMMENT ON COLUMN parzellen.ist_geloescht IS 'Soft-Delete Flag';
COMMENT ON COLUMN parzellen.geloescht_am IS 'Löschzeitpunkt für Soft-Delete';
COMMENT ON COLUMN parzellen.geloescht_von IS 'Benutzer der den Datensatz gelöscht hat';
COMMENT ON COLUMN parzellen.row_version IS 'Versionsnummer für optimistische Parallelitätskontrolle';

-- =====================================================================================
-- 2. UPDATE BEZIRKE TABLE
-- =====================================================================================

-- Add new columns to bezirke table if they don't exist
DO $$ 
BEGIN 
    -- Add Flaeche column
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'bezirke' AND column_name = 'flaeche'
    ) THEN
        ALTER TABLE bezirke ADD COLUMN flaeche NUMERIC(10,2);
        COMMENT ON COLUMN bezirke.flaeche IS 'Gesamtfläche des Bezirks in Quadratmetern';
    END IF;

    -- Add AnzahlParzellen column
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'bezirke' AND column_name = 'anzahl_parzellen'
    ) THEN
        ALTER TABLE bezirke ADD COLUMN anzahl_parzellen INTEGER NOT NULL DEFAULT 0;
        COMMENT ON COLUMN bezirke.anzahl_parzellen IS 'Anzahl der Parzellen in diesem Bezirk';
    END IF;

    -- Add Status column
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'bezirke' AND column_name = 'status'
    ) THEN
        ALTER TABLE bezirke ADD COLUMN status INTEGER NOT NULL DEFAULT 1;
        COMMENT ON COLUMN bezirke.status IS 'Status des Bezirks (0=Inaktiv, 1=Aktiv, 2=Gesperrt, 3=Umstrukturierung, 4=Archiviert)';
    END IF;

    -- Add additional audit fields if they don't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'bezirke' AND column_name = 'erstellt_am'
    ) THEN
        ALTER TABLE bezirke ADD COLUMN erstellt_am TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP;
        ALTER TABLE bezirke ADD COLUMN geaendert_am TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP;
        ALTER TABLE bezirke ADD COLUMN erstellt_von VARCHAR(100) NOT NULL DEFAULT 'system';
        ALTER TABLE bezirke ADD COLUMN geaendert_von VARCHAR(100) NOT NULL DEFAULT 'system';
        ALTER TABLE bezirke ADD COLUMN ist_geloescht BOOLEAN NOT NULL DEFAULT false;
        ALTER TABLE bezirke ADD COLUMN geloescht_am TIMESTAMPTZ;
        ALTER TABLE bezirke ADD COLUMN geloescht_von VARCHAR(100);
        ALTER TABLE bezirke ADD COLUMN row_version BYTEA NOT NULL DEFAULT '\\x00000001'::bytea;
    END IF;
END $$;

-- Update table comment
COMMENT ON TABLE bezirke IS 'Bezirke - Administrative Verwaltungsbezirke mit erweiterten Funktionen für Parzellenverwaltung und Flächenberechnung';

-- =====================================================================================
-- 3. ADD CONSTRAINTS
-- =====================================================================================

-- Parzellen constraints
DO $$ 
BEGIN 
    -- Foreign key constraint
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'fk_parzellen_bezirke_bezirk_id'
    ) THEN
        ALTER TABLE parzellen ADD CONSTRAINT fk_parzellen_bezirke_bezirk_id 
        FOREIGN KEY (bezirk_id) REFERENCES bezirke(id) ON DELETE RESTRICT;
    END IF;

    -- Check constraints for Parzellen
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints 
        WHERE constraint_name = 'ck_parzellen_flaeche_positive'
    ) THEN
        ALTER TABLE parzellen ADD CONSTRAINT ck_parzellen_flaeche_positive CHECK (flaeche > 0.00);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints 
        WHERE constraint_name = 'ck_parzellen_preis_non_negative'
    ) THEN
        ALTER TABLE parzellen ADD CONSTRAINT ck_parzellen_preis_non_negative CHECK (preis IS NULL OR preis >= 0.00);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints 
        WHERE constraint_name = 'ck_parzellen_status_valid'
    ) THEN
        ALTER TABLE parzellen ADD CONSTRAINT ck_parzellen_status_valid CHECK (status >= 0 AND status <= 6);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints 
        WHERE constraint_name = 'ck_parzellen_prioritaet_valid'
    ) THEN
        ALTER TABLE parzellen ADD CONSTRAINT ck_parzellen_prioritaet_valid CHECK (prioritaet >= 0);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints 
        WHERE constraint_name = 'ck_parzellen_nummer_not_empty'
    ) THEN
        ALTER TABLE parzellen ADD CONSTRAINT ck_parzellen_nummer_not_empty CHECK (LENGTH(TRIM(nummer)) > 0);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints 
        WHERE constraint_name = 'ck_parzellen_vergeben_am_logic'
    ) THEN
        ALTER TABLE parzellen ADD CONSTRAINT ck_parzellen_vergeben_am_logic 
        CHECK ((status = 2 AND vergeben_am IS NOT NULL) OR (status != 2 AND vergeben_am IS NULL));
    END IF;

    -- Check constraints for Bezirke
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints 
        WHERE constraint_name = 'ck_bezirke_flaeche_positive'
    ) THEN
        ALTER TABLE bezirke ADD CONSTRAINT ck_bezirke_flaeche_positive CHECK (flaeche IS NULL OR flaeche > 0.00);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints 
        WHERE constraint_name = 'ck_bezirke_anzahl_parzellen_non_negative'
    ) THEN
        ALTER TABLE bezirke ADD CONSTRAINT ck_bezirke_anzahl_parzellen_non_negative CHECK (anzahl_parzellen >= 0);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints 
        WHERE constraint_name = 'ck_bezirke_status_valid'
    ) THEN
        ALTER TABLE bezirke ADD CONSTRAINT ck_bezirke_status_valid CHECK (status >= 0 AND status <= 4);
    END IF;
END $$;

-- =====================================================================================
-- 4. CREATE INDEXES (CONCURRENTLY for zero-downtime)
-- =====================================================================================

-- Unique constraint for plot number within district
CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS uk_parzellen_bezirk_nummer 
ON parzellen (bezirk_id, nummer) WHERE ist_geloescht = false;

-- Performance indexes for Parzellen
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_status 
ON parzellen (status) WHERE ist_geloescht = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_bezirk_id 
ON parzellen (bezirk_id) WHERE ist_geloescht = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_bezirk_status 
ON parzellen (bezirk_id, status) WHERE ist_geloescht = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_flaeche 
ON parzellen (flaeche) WHERE ist_geloescht = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_preis 
ON parzellen (preis) WHERE ist_geloescht = false AND preis IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_vergeben_am 
ON parzellen (vergeben_am) WHERE ist_geloescht = false AND vergeben_am IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_prioritaet_desc 
ON parzellen (prioritaet DESC, nummer) WHERE ist_geloescht = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_utilities 
ON parzellen (has_wasser, has_strom, status) WHERE ist_geloescht = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_erstellt_am 
ON parzellen (erstellt_am DESC) WHERE ist_geloescht = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_geaendert_am 
ON parzellen (geaendert_am DESC) WHERE ist_geloescht = false;

-- Full-text search index for German content
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_beschreibung_fts 
ON parzellen USING gin(to_tsvector('german', COALESCE(beschreibung, '') || ' ' || COALESCE(besonderheiten, ''))) 
WHERE ist_geloescht = false AND (beschreibung IS NOT NULL OR besonderheiten IS NOT NULL);

-- Composite index for availability queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_availability 
ON parzellen (bezirk_id, status, flaeche, has_wasser, has_strom) 
WHERE ist_geloescht = false AND status IN (0, 1);

-- Soft delete index
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_soft_deleted 
ON parzellen (geloescht_am DESC, geloescht_von) WHERE ist_geloescht = true;

-- Bezirke indexes
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_bezirke_status_active 
ON bezirke (status, is_active) WHERE ist_geloescht = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_bezirke_sort_order 
ON bezirke (sort_order, name) WHERE ist_geloescht = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_bezirke_anzahl_parzellen 
ON bezirke (anzahl_parzellen DESC) WHERE ist_geloescht = false;

-- =====================================================================================
-- 5. CREATE FUNCTIONS AND TRIGGERS
-- =====================================================================================

-- Function for automatic timestamp updates
CREATE OR REPLACE FUNCTION update_modified_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.geaendert_am = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Function for plot count maintenance
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

-- Function for business rule validation
CREATE OR REPLACE FUNCTION validate_parzelle_business_rules()
RETURNS TRIGGER AS $$
BEGIN
    -- Validate plot number format
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
        AND (status = 1 OR status = 3)
    ) THEN
        RAISE EXCEPTION 'Bezirk existiert nicht oder ist nicht aktiv: %', NEW.bezirk_id;
    END IF;

    -- Normalize text fields
    NEW.nummer = UPPER(TRIM(NEW.nummer));
    NEW.beschreibung = TRIM(NEW.beschreibung);
    NEW.besonderheiten = TRIM(NEW.besonderheiten);

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Function for data quality maintenance
CREATE OR REPLACE FUNCTION maintain_data_quality()
RETURNS void AS $$
BEGIN
    -- Update plot counts for all districts
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

-- Create triggers
DO $$ 
BEGIN 
    -- Timestamp update triggers
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'tr_parzellen_update_timestamp') THEN
        CREATE TRIGGER tr_parzellen_update_timestamp
            BEFORE UPDATE ON parzellen
            FOR EACH ROW
            EXECUTE FUNCTION update_modified_timestamp();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'tr_bezirke_update_timestamp') THEN
        CREATE TRIGGER tr_bezirke_update_timestamp
            BEFORE UPDATE ON bezirke
            FOR EACH ROW
            EXECUTE FUNCTION update_modified_timestamp();
    END IF;

    -- Plot count maintenance trigger
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'tr_parzellen_maintain_count') THEN
        CREATE TRIGGER tr_parzellen_maintain_count
            AFTER INSERT OR UPDATE OR DELETE ON parzellen
            FOR EACH ROW
            EXECUTE FUNCTION maintain_bezirk_plot_count();
    END IF;

    -- Business rule validation trigger
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'tr_parzellen_validate_rules') THEN
        CREATE TRIGGER tr_parzellen_validate_rules
            BEFORE INSERT OR UPDATE ON parzellen
            FOR EACH ROW
            EXECUTE FUNCTION validate_parzelle_business_rules();
    END IF;
END $$;

-- =====================================================================================
-- 6. CREATE EXTENDED STATISTICS
-- =====================================================================================

DO $$ 
BEGIN 
    -- Create extended statistics for query optimization
    IF NOT EXISTS (
        SELECT 1 FROM pg_statistic_ext 
        WHERE stxname = 'st_parzellen_bezirk_status_flaeche'
    ) THEN
        CREATE STATISTICS st_parzellen_bezirk_status_flaeche (dependencies) 
        ON bezirk_id, status, flaeche FROM parzellen;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_statistic_ext 
        WHERE stxname = 'st_parzellen_utilities_status'
    ) THEN
        CREATE STATISTICS st_parzellen_utilities_status (dependencies) 
        ON has_wasser, has_strom, status FROM parzellen;
    END IF;
END $$;

-- =====================================================================================
-- 7. CREATE VIEWS
-- =====================================================================================

-- Complete plot information view
CREATE OR REPLACE VIEW v_parzellen_complete AS
SELECT 
    p.id,
    p.nummer,
    p.bezirk_id,
    b.name AS bezirk_name,
    b.display_name AS bezirk_display_name,
    p.flaeche,
    p.status,
    CASE p.status 
        WHEN 0 THEN 'Verfügbar'
        WHEN 1 THEN 'Reserviert'
        WHEN 2 THEN 'Vergeben'
        WHEN 3 THEN 'Nicht verfügbar'
        WHEN 4 THEN 'In Entwicklung'
        WHEN 5 THEN 'Stillgelegt'
        WHEN 6 THEN 'Genehmigung ausstehend'
        ELSE 'Unbekannt'
    END AS status_name,
    p.preis,
    p.vergeben_am,
    p.beschreibung,
    p.besonderheiten,
    p.has_wasser,
    p.has_strom,
    p.prioritaet,
    p.erstellt_am,
    p.geaendert_am,
    p.erstellt_von,
    p.geaendert_von,
    CONCAT(b.name, '-', p.nummer) AS vollstaendige_nummer,
    CASE 
        WHEN p.has_wasser AND p.has_strom THEN 'Vollausstattung'
        WHEN p.has_wasser THEN 'Nur Wasser'
        WHEN p.has_strom THEN 'Nur Strom'
        ELSE 'Keine Anschlüsse'
    END AS ausstattung,
    CASE 
        WHEN p.preis IS NOT NULL THEN p.preis * 12
        ELSE NULL
    END AS jahreskosten
FROM parzellen p
INNER JOIN bezirke b ON p.bezirk_id = b.id
WHERE p.ist_geloescht = false
AND b.ist_geloescht = false;

-- District statistics view
CREATE OR REPLACE VIEW v_bezirke_statistics AS
SELECT 
    b.id,
    b.name,
    b.display_name,
    b.flaeche,
    b.anzahl_parzellen,
    COUNT(p.id) AS tatsaechliche_parzellen,
    COUNT(CASE WHEN p.status = 0 THEN 1 END) AS verfuegbare_parzellen,
    COUNT(CASE WHEN p.status = 1 THEN 1 END) AS reservierte_parzellen,
    COUNT(CASE WHEN p.status = 2 THEN 1 END) AS vergebene_parzellen,
    COUNT(CASE WHEN p.has_wasser THEN 1 END) AS parzellen_mit_wasser,
    COUNT(CASE WHEN p.has_strom THEN 1 END) AS parzellen_mit_strom,
    AVG(p.flaeche) AS durchschnittliche_parzellengroesse,
    AVG(p.preis) AS durchschnittlicher_preis,
    SUM(p.flaeche) AS gesamte_parzellen_flaeche,
    CASE 
        WHEN b.flaeche IS NOT NULL AND SUM(p.flaeche) IS NOT NULL 
        THEN ROUND((SUM(p.flaeche) / b.flaeche * 100)::numeric, 2)
        ELSE NULL
    END AS flaechen_auslastung_prozent
FROM bezirke b
LEFT JOIN parzellen p ON b.id = p.bezirk_id AND p.ist_geloescht = false
WHERE b.ist_geloescht = false
GROUP BY b.id, b.name, b.display_name, b.flaeche, b.anzahl_parzellen;

-- Add view comments
COMMENT ON VIEW v_parzellen_complete IS 'Vollständige Parzellen-Ansicht mit Bezirksinformationen und berechneten Feldern für Reports und Anzeigen';
COMMENT ON VIEW v_bezirke_statistics IS 'Statistiken und Kennzahlen für jeden Bezirk mit Parzellen-Auslastung und Flächennutzung';

-- =====================================================================================
-- 8. SEED SAMPLE DATA (Optional - for development/testing)
-- =====================================================================================

-- Insert sample districts if they don't exist
INSERT INTO bezirke (id, name, display_name, description, sort_order, flaeche, status, is_active, erstellt_am, geaendert_am, erstellt_von, geaendert_von, ist_geloescht, row_version)
SELECT * FROM (VALUES 
    ('11111111-1111-1111-1111-111111111111'::uuid, 'A', 'Bezirk A - Hauptbereich', 'Hauptbereich der Kleingartenanlage mit bester Infrastruktur', 1, 15000.00, 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'system', 'system', false, '\\x00000001'::bytea),
    ('22222222-2222-2222-2222-222222222222'::uuid, 'B', 'Bezirk B - Neubaubereich', 'Neu erschlossener Bereich mit modernen Parzellen', 2, 12000.00, 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'system', 'system', false, '\\x00000001'::bytea),
    ('33333333-3333-3333-3333-333333333333'::uuid, 'C', 'Bezirk C - Erweiterung', 'Erweiterungsbereich in Planung', 3, 8000.00, 3, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'system', 'system', false, '\\x00000001'::bytea),
    ('44444444-4444-4444-4444-444444444444'::uuid, 'D', 'Bezirk D - Ruhebereich', 'Ruhiger Bereich für erfahrene Gärtner', 4, 10000.00, 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'system', 'system', false, '\\x00000001'::bytea)
) AS t(id, name, display_name, description, sort_order, flaeche, status, is_active, erstellt_am, geaendert_am, erstellt_von, geaendert_von, ist_geloescht, row_version)
WHERE NOT EXISTS (SELECT 1 FROM bezirke WHERE id = t.id);

-- =====================================================================================
-- 9. FINAL STEPS
-- =====================================================================================

-- Update table statistics for query optimization
ANALYZE parzellen;
ANALYZE bezirke;

-- Run data quality maintenance
SELECT maintain_data_quality();

-- Display deployment summary
DO $$
BEGIN
    RAISE NOTICE '=============================================================================';
    RAISE NOTICE 'KGV Bezirksverwaltung Deployment completed successfully!';
    RAISE NOTICE '=============================================================================';
    RAISE NOTICE 'Tables created: parzellen (with % constraints)', (SELECT COUNT(*) FROM information_schema.check_constraints WHERE constraint_schema = 'public' AND constraint_name LIKE 'ck_parzellen_%');
    RAISE NOTICE 'Indexes created: % indexes', (SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'public' AND tablename IN ('parzellen', 'bezirke'));
    RAISE NOTICE 'Triggers created: % triggers', (SELECT COUNT(*) FROM pg_trigger WHERE tgname LIKE 'tr_%');
    RAISE NOTICE 'Views created: 2 reporting views';
    RAISE NOTICE 'Sample districts: % districts inserted', (SELECT COUNT(*) FROM bezirke WHERE erstellt_von = 'system');
    RAISE NOTICE '=============================================================================';
    RAISE NOTICE 'System ready for use!';
END $$;

COMMIT;

-- =====================================================================================
-- END OF DEPLOYMENT SCRIPT
-- =====================================================================================