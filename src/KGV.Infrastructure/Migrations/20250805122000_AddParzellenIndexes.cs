using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KGV.Infrastructure.Migrations;

/// <summary>
/// Migration to add performance-optimized indexes for the Parzellen table
/// Implements PostgreSQL 16 concurrent indexing best practices for zero-downtime deployment
/// </summary>
public partial class AddParzellenIndexes : Migration
{
    /// <summary>
    /// Apply the migration - Add performance indexes for Parzellen table using CONCURRENTLY
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Primary performance indexes using CONCURRENTLY for zero-downtime deployment
        // Following PostgreSQL 16 best practices for production environments

        // Index for queries filtering by status (very common operation)
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_status 
            ON parzellen (status) 
            WHERE ist_geloescht = false;
        ");

        // Index for queries filtering by district ID (foreign key performance)
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_bezirk_id 
            ON parzellen (bezirk_id) 
            WHERE ist_geloescht = false;
        ");

        // Composite index for district + status queries (common in plot selection)
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_bezirk_status 
            ON parzellen (bezirk_id, status) 
            WHERE ist_geloescht = false;
        ");

        // Index for area-based queries (searching plots by size range)
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_flaeche 
            ON parzellen (flaeche) 
            WHERE ist_geloescht = false;
        ");

        // Index for price-based queries (searching plots by price range)
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_preis 
            ON parzellen (preis) 
            WHERE ist_geloescht = false AND preis IS NOT NULL;
        ");

        // Index for assignment date queries (tracking when plots were assigned)
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_vergeben_am 
            ON parzellen (vergeben_am) 
            WHERE ist_geloescht = false AND vergeben_am IS NOT NULL;
        ");

        // Index for priority-based sorting (assignment priority management)
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_prioritaet_desc 
            ON parzellen (prioritaet DESC, nummer) 
            WHERE ist_geloescht = false;
        ");

        // Composite index for utility queries (water/electricity availability)
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_utilities 
            ON parzellen (has_wasser, has_strom, status) 
            WHERE ist_geloescht = false;
        ");

        // Index for audit trail queries (creation date)
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_erstellt_am 
            ON parzellen (erstellt_am DESC) 
            WHERE ist_geloescht = false;
        ");

        // Index for audit trail queries (modification date)
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_geaendert_am 
            ON parzellen (geaendert_am DESC) 
            WHERE ist_geloescht = false;
        ");

        // Full-text search index for description fields (German text search)
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_beschreibung_fts 
            ON parzellen USING gin(to_tsvector('german', COALESCE(beschreibung, '') || ' ' || COALESCE(besonderheiten, ''))) 
            WHERE ist_geloescht = false AND (beschreibung IS NOT NULL OR besonderheiten IS NOT NULL);
        ");

        // Partial index for soft-deleted records (for administrative queries)
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_soft_deleted 
            ON parzellen (geloescht_am DESC, geloescht_von) 
            WHERE ist_geloescht = true;
        ");

        // Composite index for efficient plot availability queries
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_parzellen_availability 
            ON parzellen (bezirk_id, status, flaeche, has_wasser, has_strom) 
            WHERE ist_geloescht = false AND status IN (0, 1);
        ");

        // Add corresponding indexes to Bezirke table for join performance
        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_bezirke_status_active 
            ON bezirke (status, is_active) 
            WHERE ist_geloescht = false;
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_bezirke_sort_order 
            ON bezirke (sort_order, name) 
            WHERE ist_geloescht = false;
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_bezirke_anzahl_parzellen 
            ON bezirke (anzahl_parzellen DESC) 
            WHERE ist_geloescht = false;
        ");

        // Create statistics for query optimization (PostgreSQL 16 extended statistics)
        migrationBuilder.Sql(@"
            DO $$ 
            BEGIN 
                -- Create extended statistics for correlated columns
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
        ");

        // Update statistics to optimize query planning
        migrationBuilder.Sql("ANALYZE parzellen;");
        migrationBuilder.Sql("ANALYZE bezirke;");
    }

    /// <summary>
    /// Rollback the migration - Drop performance indexes
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop extended statistics
        migrationBuilder.Sql("DROP STATISTICS IF EXISTS st_parzellen_bezirk_status_flaeche;");
        migrationBuilder.Sql("DROP STATISTICS IF EXISTS st_parzellen_utilities_status;");

        // Drop Bezirke indexes
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_bezirke_anzahl_parzellen;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_bezirke_sort_order;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_bezirke_status_active;");

        // Drop Parzellen indexes (in reverse order of creation)
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_availability;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_soft_deleted;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_beschreibung_fts;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_geaendert_am;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_erstellt_am;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_utilities;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_prioritaet_desc;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_vergeben_am;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_preis;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_flaeche;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_bezirk_status;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_bezirk_id;");
        migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_parzellen_status;");
    }
}