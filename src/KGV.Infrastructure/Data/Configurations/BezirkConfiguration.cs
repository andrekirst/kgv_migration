using KGV.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KGV.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Bezirk entity
/// </summary>
public class BezirkConfiguration : IEntityTypeConfiguration<Bezirk>
{
    public void Configure(EntityTypeBuilder<Bezirk> builder)
    {
        builder.ToTable("bezirke");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        // Properties
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        // Base entity properties
        ConfigureBaseEntity(builder);

        // Relationships
        builder.HasMany(x => x.Katasterbezirke)
            .WithOne(x => x.Bezirk)
            .HasForeignKey(x => x.BezirkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Aktenzeichen)
            .WithOne()
            .HasForeignKey("BezirkId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Eingangsnummern)
            .WithOne()
            .HasForeignKey("BezirkId")
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("ix_bezirke_name_unique");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_bezirke_is_active");

        builder.HasIndex(x => x.SortOrder)
            .HasDatabaseName("ix_bezirke_sort_order");
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