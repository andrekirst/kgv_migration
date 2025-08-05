using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KGV.Infrastructure.Migrations;

/// <summary>
/// Migration to seed sample data for Parzellen and update Bezirke
/// Provides realistic test data for development and testing environments
/// </summary>
public partial class SeedParzellenSampleData : Migration
{
    /// <summary>
    /// Apply the migration - Insert sample data for Parzellen
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // First, ensure we have sample districts if they don't exist
        migrationBuilder.Sql(@"
            INSERT INTO bezirke (id, name, display_name, description, sort_order, flaeche, status, is_active, erstellt_am, geaendert_am, erstellt_von, geaendert_von, ist_geloescht, row_version)
            SELECT * FROM (VALUES 
                ('11111111-1111-1111-1111-111111111111'::uuid, 'A', 'Bezirk A - Hauptbereich', 'Hauptbereich der Kleingartenanlage mit bester Infrastruktur', 1, 15000.00, 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
                ('22222222-2222-2222-2222-222222222222'::uuid, 'B', 'Bezirk B - Neubaubereich', 'Neu erschlossener Bereich mit modernen Parzellen', 2, 12000.00, 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
                ('33333333-3333-3333-3333-333333333333'::uuid, 'C', 'Bezirk C - Erweiterung', 'Erweiterungsbereich in Planung', 3, 8000.00, 3, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
                ('44444444-4444-4444-4444-444444444444'::uuid, 'D', 'Bezirk D - Ruhebereich', 'Ruhiger Bereich für erfahrene Gärtner', 4, 10000.00, 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea)
            ) AS t(id, name, display_name, description, sort_order, flaeche, status, is_active, erstellt_am, geaendert_am, erstellt_von, geaendert_von, ist_geloescht, row_version)
            WHERE NOT EXISTS (SELECT 1 FROM bezirke WHERE id = t.id);
        ");

        // Insert comprehensive sample data for Parzellen
        migrationBuilder.Sql(@"
            INSERT INTO parzellen (
                id, nummer, bezirk_id, flaeche, status, preis, vergeben_am, beschreibung, besonderheiten, 
                has_wasser, has_strom, prioritaet, erstellt_am, geaendert_am, erstellt_von, geaendert_von, 
                ist_geloescht, row_version
            ) VALUES 
            -- Bezirk A Parzellen (Premium-Bereich)
            ('a1111111-1111-1111-1111-111111111111'::uuid, 'A-001', '11111111-1111-1111-1111-111111111111'::uuid, 350.00, 2, 45.00, '2024-03-15 10:30:00+01', 'Sonnige Lage mit Südausrichtung', 'Obstbäume vorhanden, Gewächshaus erlaubt', true, true, 3, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('a1111111-1111-1111-1111-111111111112'::uuid, 'A-002', '11111111-1111-1111-1111-111111111111'::uuid, 420.00, 0, 50.00, NULL, 'Große Parzelle mit altem Baumbestand', 'Historischer Apfelbaum, denkmalgeschützt', true, true, 5, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('a1111111-1111-1111-1111-111111111113'::uuid, 'A-003', '11111111-1111-1111-1111-111111111111'::uuid, 380.00, 1, 47.00, NULL, 'Eckparzelle mit zwei Zufahrten', 'Besonders geeignet für Familien', true, true, 4, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('a1111111-1111-1111-1111-111111111114'::uuid, 'A-004', '11111111-1111-1111-1111-111111111111'::uuid, 300.00, 2, 42.00, '2024-01-20 14:15:00+01', 'Kompakte Parzelle für Anfänger', 'Ideal für Gemüseanbau', true, false, 2, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('a1111111-1111-1111-1111-111111111115'::uuid, 'A-005', '11111111-1111-1111-1111-111111111111'::uuid, 450.00, 0, 55.00, NULL, 'Premium-Parzelle am Teich', 'Wasserblick, sehr ruhige Lage', true, true, 8, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            
            -- Bezirk B Parzellen (Neubaubereich)
            ('b2222222-2222-2222-2222-222222222221'::uuid, 'B-001', '22222222-2222-2222-2222-222222222222'::uuid, 320.00, 0, 38.00, NULL, 'Neue Parzelle mit moderner Ausstattung', 'LED-Beleuchtung, moderne Wasserleitungen', true, true, 3, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('b2222222-2222-2222-2222-222222222222'::uuid, 'B-002', '22222222-2222-2222-2222-222222222222'::uuid, 290.00, 1, 35.00, NULL, 'Standardparzelle im Neubaugebiet', 'Optimaler Schnitt für Selbstversorger', true, true, 2, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('b2222222-2222-2222-2222-222222222223'::uuid, 'B-003', '22222222-2222-2222-2222-222222222222'::uuid, 340.00, 2, 40.00, '2024-06-01 09:00:00+02', 'Parzelle mit Playground-Nähe', 'Familienfreundlich, Spielplatz in 50m', true, true, 4, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('b2222222-2222-2222-2222-222222222224'::uuid, 'B-004', '22222222-2222-2222-2222-222222222222'::uuid, 310.00, 0, 37.00, NULL, 'Parzelle mit Morgensonne', 'Ostausrichtung, frühe Sonneneinstrahlung', true, false, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            
            -- Bezirk C Parzellen (Erweiterungsbereich)
            ('c3333333-3333-3333-3333-333333333331'::uuid, 'C-001', '33333333-3333-3333-3333-333333333333'::uuid, 280.00, 6, 30.00, NULL, 'Parzelle in Entwicklung', 'Genehmigung für Baumpflanzung ausstehend', false, false, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('c3333333-3333-3333-3333-333333333332'::uuid, 'C-002', '33333333-3333-3333-3333-333333333333'::uuid, 300.00, 4, 32.00, NULL, 'Baustelle - Infrastruktur wird ausgebaut', 'Wasser- und Stromanschluss in Arbeit', false, false, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('c3333333-3333-3333-3333-333333333333'::uuid, 'C-003', '33333333-3333-3333-3333-333333333333'::uuid, 250.00, 0, 28.00, NULL, 'Günstige Parzelle für Einsteiger', 'Einfache Ausstattung, Potenzial vorhanden', false, false, 2, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            
            -- Bezirk D Parzellen (Ruhebereich)
            ('d4444444-4444-4444-4444-444444444441'::uuid, 'D-001', '44444444-4444-4444-4444-444444444444'::uuid, 500.00, 2, 60.00, '2023-12-10 16:30:00+01', 'Große ruhige Parzelle', 'Alter Baumbestand, sehr privat', true, true, 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('d4444444-4444-4444-4444-444444444442'::uuid, 'D-002', '44444444-4444-4444-4444-444444444444'::uuid, 380.00, 0, 48.00, NULL, 'Naturnahe Parzelle', 'Wildblumenweide, Bienenfreundlich', true, false, 3, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('d4444444-4444-4444-4444-444444444443'::uuid, 'D-003', '44444444-4444-4444-4444-444444444444'::uuid, 420.00, 1, 52.00, NULL, 'Reserviert für Seniorengärtner', 'Erhöhte Beete geplant, barrierefrei', true, true, 7, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('d4444444-4444-4444-4444-444444444444'::uuid, 'D-004', '44444444-4444-4444-4444-444444444444'::uuid, 360.00, 3, NULL, NULL, 'Temporarily nicht verfügbar', 'Bodensanierung erforderlich', true, true, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            
            -- Additional test cases for edge scenarios
            ('e5555555-5555-5555-5555-555555555551'::uuid, 'A-010', '11111111-1111-1111-1111-111111111111'::uuid, 200.00, 5, NULL, NULL, 'Stillgelegte Parzelle', 'Altlasten entfernt, Neubepflanzung geplant', false, false, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea),
            ('e5555555-5555-5555-5555-555555555552'::uuid, 'B-020', '22222222-2222-2222-2222-222222222222'::uuid, 150.00, 0, 25.00, NULL, 'Mini-Parzelle für Kräuter', 'Speziell für Kräutergarten konzipiert', true, false, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'migration', 'migration', false, '\\x00000001'::bytea);
        ");

        // Update table statistics after data insertion
        migrationBuilder.Sql("ANALYZE parzellen;");
        migrationBuilder.Sql("ANALYZE bezirke;");

        // Create a view for easy access to plot information with district details
        migrationBuilder.Sql(@"
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
        ");

        // Add comment to the view
        migrationBuilder.Sql(@"
            COMMENT ON VIEW v_parzellen_complete IS 'Vollständige Parzellen-Ansicht mit Bezirksinformationen und berechneten Feldern für Reports und Anzeigen';
        ");

        // Create a summary statistics view
        migrationBuilder.Sql(@"
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
        ");

        migrationBuilder.Sql(@"
            COMMENT ON VIEW v_bezirke_statistics IS 'Statistiken und Kennzahlen für jeden Bezirk mit Parzellen-Auslastung und Flächennutzung';
        ");
    }

    /// <summary>
    /// Rollback the migration - Remove sample data and views
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop views
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_bezirke_statistics;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS v_parzellen_complete;");

        // Delete sample data (in reverse order to respect foreign key constraints)
        migrationBuilder.Sql("DELETE FROM parzellen WHERE erstellt_von = 'migration';");
        migrationBuilder.Sql("DELETE FROM bezirke WHERE erstellt_von = 'migration';");
    }
}