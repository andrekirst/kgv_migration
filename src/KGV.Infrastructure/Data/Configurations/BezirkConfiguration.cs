using KGV.Domain.Entities;
using KGV.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KGV.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Bezirk entity
/// Updated for PostgreSQL 16 best practices with German column naming
/// </summary>
public class BezirkConfiguration : IEntityTypeConfiguration<Bezirk>
{
    public void Configure(EntityTypeBuilder<Bezirk> builder)
    {
        builder.ToTable("bezirke");
        builder.HasComment("Bezirke - Administrative Verwaltungsbezirke mit erweiterten Funktionen für Parzellenverwaltung und Flächenberechnung");

        // Primary key
        builder.HasKey(x => x.Id)
            .HasName("pk_bezirke");
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .ValueGeneratedOnAdd()
            .HasComment("Eindeutige ID des Bezirks");

        // Properties
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(10)")
            .HasMaxLength(10)
            .IsRequired()
            .HasComment("Name/Bezeichner des Bezirks");

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false)
            .HasComment("Vollständiger Anzeigename des Bezirks");

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false)
            .HasComment("Beschreibung des Bezirks");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired()
            .HasComment("Ob der Bezirk derzeit aktiv ist");

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .IsRequired()
            .HasComment("Sortierreihenfolge für die Anzeige von Bezirken");

        // Additional properties for district management
        builder.Property(x => x.Flaeche)
            .HasColumnName("flaeche")
            .HasColumnType("numeric(10,2)")
            .HasPrecision(10, 2)
            .IsRequired(false)
            .HasComment("Gesamtfläche des Bezirks in Quadratmetern");

        builder.Property(x => x.AnzahlParzellen)
            .HasColumnName("anzahl_parzellen")
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .IsRequired()
            .HasComment("Anzahl der Parzellen in diesem Bezirk");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("integer")
            .HasConversion<int>()
            .HasDefaultValue(BezirkStatus.Active)
            .IsRequired()
            .HasComment("Status des Bezirks (0=Inaktiv, 1=Aktiv, 2=Gesperrt, 3=Umstrukturierung, 4=Archiviert)");

        // Base entity properties with German column names
        ConfigureBaseEntity(builder);

        // Relationships
        builder.HasMany(x => x.Katasterbezirke)
            .WithOne(x => x.Bezirk)
            .HasForeignKey(x => x.BezirkId)
            .HasConstraintName("fk_bezirke_katasterbezirke_bezirk_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Aktenzeichen)
            .WithOne()
            .HasForeignKey("BezirkId")
            .HasConstraintName("fk_bezirke_aktenzeichen_bezirk_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Eingangsnummern)
            .WithOne()
            .HasForeignKey("BezirkId")
            .HasConstraintName("fk_bezirke_eingangsnummern_bezirk_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship to Parzellen
        builder.HasMany(x => x.Parzellen)
            .WithOne(p => p.Bezirk)
            .HasForeignKey(p => p.BezirkId)
            .HasConstraintName("fk_bezirke_parzellen_bezirk_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("uk_bezirke_name")
            .HasFilter("ist_geloescht = false");

        builder.HasIndex(x => new { x.Status, x.IsActive })
            .HasDatabaseName("ix_bezirke_status_active")
            .HasFilter("ist_geloescht = false");

        builder.HasIndex(x => new { x.SortOrder, x.Name })
            .HasDatabaseName("ix_bezirke_sort_order")
            .HasFilter("ist_geloescht = false");

        builder.HasIndex(x => x.AnzahlParzellen)
            .HasDatabaseName("ix_bezirke_anzahl_parzellen")
            .IsDescending()
            .HasFilter("ist_geloescht = false");

        // Check constraints for business rules
        ConfigureCheckConstraints(builder);

        // Global query filter for soft delete
        builder.HasQueryFilter(x => !x.IsDeleted);
    }

    /// <summary>
    /// Configure PostgreSQL check constraints for business rules
    /// </summary>
    private static void ConfigureCheckConstraints(EntityTypeBuilder<Bezirk> builder)
    {
        builder.HasCheckConstraint("ck_bezirke_flaeche_positive", "flaeche IS NULL OR flaeche > 0.00");
        builder.HasCheckConstraint("ck_bezirke_anzahl_parzellen_non_negative", "anzahl_parzellen >= 0");
        builder.HasCheckConstraint("ck_bezirke_status_valid", "status >= 0 AND status <= 4");
        builder.HasCheckConstraint("ck_bezirke_name_not_empty", "LENGTH(TRIM(name)) > 0");
    }

    /// <summary>
    /// Configure base entity properties with German column names
    /// </summary>
    private static void ConfigureBaseEntity(EntityTypeBuilder<Bezirk> builder)
    {
        builder.Property(x => x.CreatedAt)
            .HasColumnName("erstellt_am")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired()
            .HasComment("Erstellungszeitpunkt");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("geaendert_am")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired()
            .HasComment("Letzter Änderungszeitpunkt");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("erstellt_von")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .HasDefaultValue("system")
            .IsRequired()
            .HasComment("Benutzer der den Datensatz erstellt hat");

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("geaendert_von")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .HasDefaultValue("system")
            .IsRequired()
            .HasComment("Benutzer der den Datensatz zuletzt geändert hat");

        builder.Property(x => x.IsDeleted)
            .HasColumnName("ist_geloescht")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("Soft-Delete Flag");

        builder.Property(x => x.DeletedAt)
            .HasColumnName("geloescht_am")
            .HasColumnType("timestamptz")
            .IsRequired(false)
            .HasComment("Löschzeitpunkt für Soft-Delete");

        builder.Property(x => x.DeletedBy)
            .HasColumnName("geloescht_von")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false)
            .HasComment("Benutzer der den Datensatz gelöscht hat");

        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bytea")
            .IsRowVersion()
            .ValueGeneratedOnAddOrUpdate()
            .IsRequired()
            .HasComment("Versionsnummer für optimistische Parallelitätskontrolle");

        // Index for soft delete queries
        builder.HasIndex(x => x.IsDeleted)
            .HasDatabaseName("ix_bezirke_ist_geloescht");

        builder.HasIndex(x => new { x.DeletedAt, x.DeletedBy })
            .HasDatabaseName("ix_bezirke_soft_deleted")
            .IsDescending(true, false)
            .HasFilter("ist_geloescht = true");
    }
}