using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KGV.Infrastructure.Migrations;

/// <summary>
/// Migration to create the Parzellen (Plots) table for district management
/// Implements PostgreSQL 16 best practices with optimized indexing and constraints
/// </summary>
public partial class CreateParzellenTable : Migration
{
    /// <summary>
    /// Apply the migration - Create Parzellen table with PostgreSQL 16 optimizations
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create the Parzellen table with PostgreSQL 16 optimized schema
        migrationBuilder.CreateTable(
            name: "parzellen",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()", comment: "Eindeutige ID der Parzelle"),
                nummer = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, comment: "Parzellennummer innerhalb des Bezirks"),
                bezirk_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Referenz zum übergeordneten Bezirk"),
                flaeche = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, comment: "Fläche der Parzelle in Quadratmetern"),
                status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Status der Parzelle (0=Verfügbar, 1=Reserviert, 2=Vergeben, etc.)"),
                preis = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true, comment: "Preis oder Pachtkosten für die Parzelle"),
                vergeben_am = table.Column<DateTime>(type: "timestamptz", nullable: true, comment: "Datum der Zuteilung falls vergeben"),
                beschreibung = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, comment: "Zusätzliche Notizen oder Beschreibung der Parzelle"),
                besonderheiten = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, comment: "Besondere Merkmale oder Eigenschaften der Parzelle"),
                has_wasser = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Verfügbarkeit von Wasseranschluss"),
                has_strom = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Verfügbarkeit von Stromanschluss"),
                prioritaet = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Prioritätsstufe für Zuteilung (höhere Zahlen = höhere Priorität)"),
                erstellt_am = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Erstellungszeitpunkt"),
                geaendert_am = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Letzter Änderungszeitpunkt"),
                erstellt_von = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: "system", comment: "Benutzer der den Datensatz erstellt hat"),
                geaendert_von = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: "system", comment: "Benutzer der den Datensatz zuletzt geändert hat"),
                ist_geloescht = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Soft-Delete Flag"),
                geloescht_am = table.Column<DateTime>(type: "timestamptz", nullable: true, comment: "Löschzeitpunkt für Soft-Delete"),
                geloescht_von = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, comment: "Benutzer der den Datensatz gelöscht hat"),
                row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, comment: "Versionsnummer für optimistische Parallelitätskontrolle")
            },
            constraints: table =>
            {
                // Primary key constraint
                table.PrimaryKey("pk_parzellen", x => x.id);
                
                // Foreign key constraint to Bezirke table with proper cascade behavior
                table.ForeignKey(
                    name: "fk_parzellen_bezirke_bezirk_id",
                    column: x => x.bezirk_id,
                    principalTable: "bezirke",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict); // Prevent accidental deletion of districts with plots
                
                // Check constraints for business rules following PostgreSQL 16 best practices
                table.CheckConstraint("ck_parzellen_flaeche_positive", "flaeche > 0.00");
                table.CheckConstraint("ck_parzellen_preis_non_negative", "preis IS NULL OR preis >= 0.00");
                table.CheckConstraint("ck_parzellen_status_valid", "status >= 0 AND status <= 6");
                table.CheckConstraint("ck_parzellen_prioritaet_valid", "prioritaet >= 0");
                table.CheckConstraint("ck_parzellen_nummer_not_empty", "LENGTH(TRIM(nummer)) > 0");
                table.CheckConstraint("ck_parzellen_vergeben_am_logic", 
                    "(status = 2 AND vergeben_am IS NOT NULL) OR (status != 2 AND vergeben_am IS NULL)");
            },
            comment: "Parzellen (Gartenparzellen) - Einzelne Gartenparzellen innerhalb eines Bezirks mit vollständiger Verwaltung und Ausstattungsinformationen");

        // Create unique constraint for plot number within district (compound uniqueness)
        migrationBuilder.CreateIndex(
            name: "uk_parzellen_bezirk_nummer",
            table: "parzellen",
            columns: new[] { "bezirk_id", "nummer" },
            unique: true,
            filter: "ist_geloescht = false"); // Unique only for non-deleted records
    }

    /// <summary>
    /// Rollback the migration - Drop Parzellen table
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop the Parzellen table (this will also drop all indexes and constraints)
        migrationBuilder.DropTable(
            name: "parzellen");
    }
}