using KGV.Domain.Entities;
using KGV.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KGV.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Parzelle entity
/// Implements PostgreSQL 16 best practices with German column naming and optimized indexing
/// </summary>
public class ParzelleConfiguration : IEntityTypeConfiguration<Parzelle>
{
    public void Configure(EntityTypeBuilder<Parzelle> builder)
    {
        // Table configuration
        builder.ToTable("parzellen", schema: null);
        builder.HasComment("Parzellen (Gartenparzellen) - Einzelne Gartenparzellen innerhalb eines Bezirks mit vollständiger Verwaltung und Ausstattungsinformationen");

        // Primary key
        builder.HasKey(e => e.Id)
            .HasName("pk_parzellen");
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .ValueGeneratedOnAdd()
            .HasComment("Eindeutige ID der Parzelle");

        // Plot number configuration
        builder.Property(e => e.Nummer)
            .IsRequired()
            .HasColumnName("nummer")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .HasComment("Parzellennummer innerhalb des Bezirks");

        // Foreign key to Bezirk
        builder.Property(e => e.BezirkId)
            .IsRequired()
            .HasColumnName("bezirk_id")
            .HasColumnType("uuid")
            .HasComment("Referenz zum übergeordneten Bezirk");

        // Area configuration with precision
        builder.Property(e => e.Flaeche)
            .IsRequired()
            .HasColumnName("flaeche")
            .HasColumnType("numeric(10,2)")
            .HasPrecision(10, 2)
            .HasComment("Fläche der Parzelle in Quadratmetern");

        // Status enum configuration
        builder.Property(e => e.Status)
            .IsRequired()
            .HasColumnName("status")
            .HasColumnType("integer")
            .HasConversion<int>()
            .HasDefaultValue(ParzellenStatus.Available)
            .HasComment("Status der Parzelle (0=Verfügbar, 1=Reserviert, 2=Vergeben, etc.)");

        // Price configuration
        builder.Property(e => e.Preis)
            .HasColumnName("preis")
            .HasColumnType("numeric(10,2)")
            .HasPrecision(10, 2)
            .IsRequired(false)
            .HasComment("Preis oder Pachtkosten für die Parzelle");

        // Assignment date configuration
        builder.Property(e => e.VergebenAm)
            .HasColumnName("vergeben_am")
            .HasColumnType("timestamptz")
            .IsRequired(false)
            .HasComment("Datum der Zuteilung falls vergeben");

        // Description configuration
        builder.Property(e => e.Beschreibung)
            .HasColumnName("beschreibung")
            .HasColumnType("varchar(1000)")
            .HasMaxLength(1000)
            .IsRequired(false)
            .HasComment("Zusätzliche Notizen oder Beschreibung der Parzelle");

        // Special features configuration
        builder.Property(e => e.Besonderheiten)
            .HasColumnName("besonderheiten")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false)
            .HasComment("Besondere Merkmale oder Eigenschaften der Parzelle");

        // Utility access configuration
        builder.Property(e => e.HasWasser)
            .IsRequired()
            .HasColumnName("has_wasser")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .HasComment("Verfügbarkeit von Wasseranschluss");

        builder.Property(e => e.HasStrom)
            .IsRequired()
            .HasColumnName("has_strom")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .HasComment("Verfügbarkeit von Stromanschluss");

        // Priority configuration
        builder.Property(e => e.Prioritaet)
            .IsRequired()
            .HasColumnName("prioritaet")
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .HasComment("Prioritätsstufe für Zuteilung (höhere Zahlen = höhere Priorität)");

        // Audit fields configuration (inherited from BaseEntity)
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnName("erstellt_am")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("Erstellungszeitpunkt");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasColumnName("geaendert_am")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("Letzter Änderungszeitpunkt");

        builder.Property(e => e.CreatedBy)
            .IsRequired()
            .HasColumnName("erstellt_von")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .HasDefaultValue("system")
            .HasComment("Benutzer der den Datensatz erstellt hat");

        builder.Property(e => e.UpdatedBy)
            .IsRequired()
            .HasColumnName("geaendert_von")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .HasDefaultValue("system")
            .HasComment("Benutzer der den Datensatz zuletzt geändert hat");

        // Soft delete configuration
        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasColumnName("ist_geloescht")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .HasComment("Soft-Delete Flag");

        builder.Property(e => e.DeletedAt)
            .HasColumnName("geloescht_am")
            .HasColumnType("timestamptz")
            .IsRequired(false)
            .HasComment("Löschzeitpunkt für Soft-Delete");

        builder.Property(e => e.DeletedBy)
            .HasColumnName("geloescht_von")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false)
            .HasComment("Benutzer der den Datensatz gelöscht hat");

        // Row version for optimistic concurrency
        builder.Property(e => e.RowVersion)
            .IsRequired()
            .HasColumnName("row_version")
            .HasColumnType("bytea")
            .IsRowVersion()
            .ValueGeneratedOnAddOrUpdate()
            .HasComment("Versionsnummer für optimistische Parallelitätskontrolle");

        // Relationships
        builder.HasOne(e => e.Bezirk)
            .WithMany(b => b.Parzellen)
            .HasForeignKey(e => e.BezirkId)
            .HasConstraintName("fk_parzellen_bezirke_bezirk_id")
            .OnDelete(DeleteBehavior.Restrict) // Prevent accidental deletion of districts with plots
            .IsRequired();

        // Indexes for performance (created in separate migration for CONCURRENTLY support)
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_parzellen_status")
            .HasFilter("ist_geloescht = false");

        builder.HasIndex(e => e.BezirkId)
            .HasDatabaseName("ix_parzellen_bezirk_id")
            .HasFilter("ist_geloescht = false");

        builder.HasIndex(new[] { nameof(Parzelle.BezirkId), nameof(Parzelle.Nummer) })
            .HasDatabaseName("uk_parzellen_bezirk_nummer")
            .IsUnique()
            .HasFilter("ist_geloescht = false");

        builder.HasIndex(e => e.Flaeche)
            .HasDatabaseName("ix_parzellen_flaeche")
            .HasFilter("ist_geloescht = false");

        builder.HasIndex(e => e.Preis)
            .HasDatabaseName("ix_parzellen_preis")
            .HasFilter("ist_geloescht = false AND preis IS NOT NULL");

        builder.HasIndex(e => e.VergebenAm)
            .HasDatabaseName("ix_parzellen_vergeben_am")
            .HasFilter("ist_geloescht = false AND vergeben_am IS NOT NULL");

        builder.HasIndex(e => new { e.Prioritaet, e.Nummer })
            .HasDatabaseName("ix_parzellen_prioritaet_desc")
            .IsDescending(true, false)
            .HasFilter("ist_geloescht = false");

        builder.HasIndex(e => new { e.HasWasser, e.HasStrom, e.Status })
            .HasDatabaseName("ix_parzellen_utilities")
            .HasFilter("ist_geloescht = false");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_parzellen_erstellt_am")
            .IsDescending()
            .HasFilter("ist_geloescht = false");

        builder.HasIndex(e => e.UpdatedAt)
            .HasDatabaseName("ix_parzellen_geaendert_am")
            .IsDescending()
            .HasFilter("ist_geloescht = false");

        // Composite index for efficient plot availability queries
        builder.HasIndex(e => new { e.BezirkId, e.Status, e.Flaeche, e.HasWasser, e.HasStrom })
            .HasDatabaseName("ix_parzellen_availability")
            .HasFilter("ist_geloescht = false AND status IN (0, 1)");

        // Partial index for soft-deleted records
        builder.HasIndex(e => new { e.DeletedAt, e.DeletedBy })
            .HasDatabaseName("ix_parzellen_soft_deleted")
            .IsDescending(true, false)
            .HasFilter("ist_geloescht = true");

        // Global query filter for soft delete (configured in DbContext)
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Value converters and additional configurations
        ConfigureCheckConstraints(builder);
    }

    /// <summary>
    /// Configure PostgreSQL check constraints for business rules
    /// </summary>
    private static void ConfigureCheckConstraints(EntityTypeBuilder<Parzelle> builder)
    {
        // Check constraints for data validation (PostgreSQL specific)
        builder.HasCheckConstraint("ck_parzellen_flaeche_positive", "flaeche > 0.00");
        builder.HasCheckConstraint("ck_parzellen_preis_non_negative", "preis IS NULL OR preis >= 0.00");
        builder.HasCheckConstraint("ck_parzellen_status_valid", "status >= 0 AND status <= 6");
        builder.HasCheckConstraint("ck_parzellen_prioritaet_valid", "prioritaet >= 0");
        builder.HasCheckConstraint("ck_parzellen_nummer_not_empty", "LENGTH(TRIM(nummer)) > 0");
        builder.HasCheckConstraint("ck_parzellen_vergeben_am_logic", 
            "(status = 2 AND vergeben_am IS NOT NULL) OR (status != 2 AND vergeben_am IS NULL)");
    }
}