using KGV.Domain.Common;
using KGV.Domain.Entities;
using KGV.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace KGV.Infrastructure.Data;

/// <summary>
/// Entity Framework database context for the KGV application
/// </summary>
public class KgvDbContext : DbContext
{
    public KgvDbContext(DbContextOptions<KgvDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Bezirke (Districts)
    /// </summary>
    public DbSet<Bezirk> Bezirke { get; set; } = null!;

    /// <summary>
    /// Antraege (Applications)
    /// </summary>
    public DbSet<Antrag> Antraege { get; set; } = null!;

    /// <summary>
    /// Katasterbezirke (Cadastral Districts)
    /// </summary>
    public DbSet<Katasterbezirk> Katasterbezirke { get; set; } = null!;

    /// <summary>
    /// Bezirke-Katasterbezirke Junction Table
    /// </summary>
    public DbSet<BezirkeKatasterbezirke> BezirkeKatasterbezirke { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Bezirk entity
        modelBuilder.Entity<Bezirk>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(10);
            
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100);
            
            entity.Property(e => e.Description)
                .HasMaxLength(500);
            
            entity.Property(e => e.Flaeche)
                .HasPrecision(18, 2);
            
            entity.HasIndex(e => e.Name)
                .IsUnique();
        });

        // Configure Antrag entity
        modelBuilder.Entity<Antrag>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Configure value objects as owned entities
            entity.OwnsOne(e => e.Adresse, address =>
            {
                address.Property(a => a.Strasse).HasColumnName("Strasse").HasMaxLength(200);
                address.Property(a => a.PLZ).HasColumnName("PLZ").HasMaxLength(5);
                address.Property(a => a.Ort).HasColumnName("Ort").HasMaxLength(100);
            });

            entity.OwnsOne(e => e.Telefon, phone =>
            {
                phone.Property(p => p.Value).HasColumnName("Telefon").HasMaxLength(50);
            });

            entity.OwnsOne(e => e.MobilTelefon, phone =>
            {
                phone.Property(p => p.Value).HasColumnName("MobilTelefon").HasMaxLength(50);
            });

            entity.OwnsOne(e => e.GeschTelefon, phone =>
            {
                phone.Property(p => p.Value).HasColumnName("GeschTelefon").HasMaxLength(50);
            });

            entity.OwnsOne(e => e.MobilTelefon2, phone =>
            {
                phone.Property(p => p.Value).HasColumnName("MobilTelefon2").HasMaxLength(50);
            });

            entity.OwnsOne(e => e.EMail, email =>
            {
                email.Property(e => e.Value).HasColumnName("EMail").HasMaxLength(200);
            });

            // Configure string properties
            entity.Property(e => e.Vorname).HasMaxLength(50);
            entity.Property(e => e.Nachname).HasMaxLength(50);
            entity.Property(e => e.Titel).HasMaxLength(50);
            entity.Property(e => e.Vorname2).HasMaxLength(50);
            entity.Property(e => e.Nachname2).HasMaxLength(50);
            entity.Property(e => e.Titel2).HasMaxLength(50);
            entity.Property(e => e.Briefanrede).HasMaxLength(150);
            entity.Property(e => e.WartelistenNr32).HasMaxLength(20);
            entity.Property(e => e.WartelistenNr33).HasMaxLength(20);
            entity.Property(e => e.Wunsch).HasMaxLength(600);
            entity.Property(e => e.Vermerk).HasMaxLength(2000);
            entity.Property(e => e.Geburtstag).HasMaxLength(100);
            entity.Property(e => e.Geburtstag2).HasMaxLength(100);

            // Configure enums
            entity.Property(e => e.Anrede).HasConversion<string>();
            entity.Property(e => e.Anrede2).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        // Configure Katasterbezirk entity
        modelBuilder.Entity<Katasterbezirk>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.KatasterbezirkCode)
                .IsRequired()
                .HasMaxLength(10);
            
            entity.Property(e => e.KatasterbezirkName)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Description)
                .HasMaxLength(500);

            // Configure relationship with Bezirk
            entity.HasOne(e => e.Bezirk)
                .WithMany(b => b.Katasterbezirke)
                .HasForeignKey(e => e.BezirkId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.BezirkId, e.KatasterbezirkCode })
                .IsUnique();
        });

        // Configure BezirkeKatasterbezirke junction entity
        modelBuilder.Entity<BezirkeKatasterbezirke>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.BezirkName)
                .IsRequired()
                .HasMaxLength(10);
            
            entity.Property(e => e.KatasterbezirkCode)
                .IsRequired()
                .HasMaxLength(10);
            
            entity.Property(e => e.KatasterbezirkName)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Beschreibung)
                .HasMaxLength(500);

            // Configure relationships
            entity.HasOne(e => e.Bezirk)
                .WithMany(b => b.BezirkeKatasterbezirke)
                .HasForeignKey(e => e.BezirkId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Katasterbezirk)
                .WithMany(k => k.BezirkeKatasterbezirke)
                .HasForeignKey(e => e.KatasterbezirkId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique constraint on legacy fields
            entity.HasIndex(e => new { e.BezirkName, e.KatasterbezirkCode })
                .IsUnique();
        });

        // Configure BaseEntity properties for all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Configure PostgreSQL optimistic concurrency with xmin system column
                modelBuilder.Entity(entityType.ClrType)
                    .Property<uint>("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            }
        }
    }

    /// <summary>
    /// Override SaveChanges to automatically set audit fields
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Override SaveChanges to automatically set audit fields
    /// </summary>
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// Updates audit fields for tracked entities
    /// </summary>
    private void UpdateAuditFields()
    {
        var now = DateTime.UtcNow;
        var currentUser = "system";

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = currentUser;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUser;
                    break;
            }
        }
    }
}