using KGV.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KGV.Infrastructure.Data;

/// <summary>
/// Simplified Entity Framework database context for the KGV application
/// </summary>
public class KgvDbContext : DbContext
{
    public KgvDbContext(DbContextOptions<KgvDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Bezirke (Districts) - Primary entity for minimal API
    /// </summary>
    public DbSet<Bezirk> Bezirke { get; set; } = null!;

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

        foreach (var entry in ChangeTracker.Entries<Bezirk>())
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