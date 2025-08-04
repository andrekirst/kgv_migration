using KGV.Domain.Entities;
using KGV.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KGV.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Antrag entity
/// </summary>
public class AntragConfiguration : IEntityTypeConfiguration<Antrag>
{
    public void Configure(EntityTypeBuilder<Antrag> builder)
    {
        builder.ToTable("antraege");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        // Properties
        builder.Property(x => x.AktenzeichenValue)
            .HasColumnName("aktenzeichen")
            .HasMaxLength(20);

        builder.Property(x => x.WartelistenNr32)
            .HasColumnName("wartelisten_nr_32")
            .HasMaxLength(20);

        builder.Property(x => x.WartelistenNr33)
            .HasColumnName("wartelisten_nr_33")
            .HasMaxLength(20);

        builder.Property(x => x.Anrede)
            .HasColumnName("anrede")
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(x => x.Titel)
            .HasColumnName("titel")
            .HasMaxLength(50);

        builder.Property(x => x.Vorname)
            .HasColumnName("vorname")
            .HasMaxLength(50);

        builder.Property(x => x.Nachname)
            .HasColumnName("nachname")
            .HasMaxLength(50);

        builder.Property(x => x.Anrede2)
            .HasColumnName("anrede2")
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(x => x.Titel2)
            .HasColumnName("titel2")
            .HasMaxLength(50);

        builder.Property(x => x.Vorname2)
            .HasColumnName("vorname2")
            .HasMaxLength(50);

        builder.Property(x => x.Nachname2)
            .HasColumnName("nachname2")
            .HasMaxLength(50);

        builder.Property(x => x.Briefanrede)
            .HasColumnName("briefanrede")
            .HasMaxLength(150);

        // Address, Phone, and Email are configured via value converters in DbContext

        builder.Property(x => x.Bewerbungsdatum)
            .HasColumnName("bewerbungsdatum")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Bestaetigungsdatum)
            .HasColumnName("bestaetigungsdatum")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.AktuellesAngebot)
            .HasColumnName("aktuelles_angebot")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Loeschdatum)
            .HasColumnName("loeschdatum")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Wunsch)
            .HasColumnName("wunsch")
            .HasMaxLength(600);

        builder.Property(x => x.Vermerk)
            .HasColumnName("vermerk")
            .HasMaxLength(2000);

        builder.Property(x => x.Aktiv)
            .HasColumnName("aktiv")
            .HasDefaultValue(true);

        builder.Property(x => x.DeaktiviertAm)
            .HasColumnName("deaktiviert_am")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Geburtstag)
            .HasColumnName("geburtstag")
            .HasMaxLength(100);

        builder.Property(x => x.Geburtstag2)
            .HasColumnName("geburtstag2")
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasDefaultValue(AntragStatus.NeuEingegangen)
            .HasMaxLength(20);

        // Base entity properties
        ConfigureBaseEntity(builder);

        // Relationships
        builder.HasMany(x => x.Verlauf)
            .WithOne(x => x.Antrag)
            .HasForeignKey(x => x.AntragId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.AktenzeichenValue)
            .HasDatabaseName("ix_antraege_aktenzeichen");

        builder.HasIndex(x => x.Nachname)
            .HasDatabaseName("ix_antraege_nachname");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_antraege_status");

        builder.HasIndex(x => x.Bewerbungsdatum)
            .HasDatabaseName("ix_antraege_bewerbungsdatum");

        builder.HasIndex(x => x.Aktiv)
            .HasDatabaseName("ix_antraege_aktiv");

        // Composite index for common queries
        builder.HasIndex(x => new { x.Status, x.Aktiv, x.Bewerbungsdatum })
            .HasDatabaseName("ix_antraege_status_aktiv_bewerbungsdatum");

        // Full text search index for German content
        builder.HasIndex(x => new { x.Vorname, x.Nachname, x.Vermerk })
            .HasDatabaseName("ix_antraege_fulltext")
            .HasMethod("gin")
            .IsTsVectorExpressionIndex("german");
    }

    private static void ConfigureBaseEntity<T>(EntityTypeBuilder<T> builder) where T : class
    {
        builder.Property("CreatedAt")
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property("UpdatedAt")
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property("CreatedBy")
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property("UpdatedBy")
            .HasColumnName("updated_by")
            .HasMaxLength(100);

        builder.Property("IsDeleted")
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property("DeletedAt")
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        builder.Property("DeletedBy")
            .HasColumnName("deleted_by")
            .HasMaxLength(100);

        builder.Property("RowVersion")
            .HasColumnName("row_version")
            .IsRowVersion();

        // Index for soft delete
        builder.HasIndex("IsDeleted")
            .HasDatabaseName($"ix_{builder.Metadata.GetTableName()}_is_deleted");
    }
}