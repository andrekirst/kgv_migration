using System.Linq.Expressions;
using KGV.Domain.Common;
using KGV.Domain.Entities;
using KGV.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
    /// Antraege (Applications)
    /// </summary>
    public DbSet<Antrag> Antraege { get; set; } = null!;

    /// <summary>
    /// Bezirke (Districts)
    /// </summary>
    public DbSet<Bezirk> Bezirke { get; set; } = null!;

    /// <summary>
    /// Katasterbezirke (Cadastral districts)
    /// </summary>
    public DbSet<Katasterbezirk> Katasterbezirke { get; set; } = null!;

    /// <summary>
    /// Aktenzeichen (File references)
    /// </summary>
    public DbSet<AktenzeichenEntity> Aktenzeichen { get; set; } = null!;

    /// <summary>
    /// Eingangsnummern (Entry numbers)
    /// </summary>
    public DbSet<Eingangsnummer> Eingangsnummern { get; set; } = null!;

    /// <summary>
    /// Personen (People/Users)
    /// </summary>
    public DbSet<Person> Personen { get; set; } = null!;

    /// <summary>
    /// Verlauf (History entries)
    /// </summary>
    public DbSet<Verlauf> Verlauf { get; set; } = null!;

    /// <summary>
    /// Parzellen (Garden plots)
    /// </summary>
    public DbSet<Parzelle> Parzellen { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KgvDbContext).Assembly);

        // Global query filters for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Equal(
                    Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
                    Expression.Constant(false));
                var lambda = Expression.Lambda(body, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        // Configure value converters for value objects
        ConfigureValueObjects(modelBuilder);

        // Configure PostgreSQL-specific features
        ConfigurePostgreSqlFeatures(modelBuilder);
    }

    /// <summary>
    /// Configures value object conversions
    /// </summary>
    private static void ConfigureValueObjects(ModelBuilder modelBuilder)
    {
        // Address value object conversion
        var addressConverter = new ValueConverter<Address?, string?>(
            v => v != null ? $"{v.Strasse}|{v.PLZ}|{v.Ort}" : null,
            v => !string.IsNullOrEmpty(v) ? 
                ParseAddress(v) : null);

        var phoneConverter = new ValueConverter<PhoneNumber?, string?>(
            v => v != null ? v.Value : null,
            v => !string.IsNullOrEmpty(v) ? new PhoneNumber(v) : null);

        var emailConverter = new ValueConverter<Email?, string?>(
            v => v != null ? v.Value : null,
            v => !string.IsNullOrEmpty(v) ? new Email(v) : null);

        // Apply converters to Antrag entity
        modelBuilder.Entity<Antrag>(entity =>
        {
            entity.Property(e => e.Adresse)
                .HasConversion(addressConverter)
                .HasMaxLength(200);

            entity.Property(e => e.Telefon)
                .HasConversion(phoneConverter)
                .HasMaxLength(50);

            entity.Property(e => e.MobilTelefon)
                .HasConversion(phoneConverter)
                .HasMaxLength(50);

            entity.Property(e => e.GeschTelefon)
                .HasConversion(phoneConverter)
                .HasMaxLength(50);

            entity.Property(e => e.MobilTelefon2)
                .HasConversion(phoneConverter)
                .HasMaxLength(50);

            entity.Property(e => e.EMail)
                .HasConversion(emailConverter)
                .HasMaxLength(100);
        });

        // Apply converters to Person entity
        modelBuilder.Entity<Person>(entity =>
        {
            entity.Property(e => e.Telefon)
                .HasConversion(phoneConverter)
                .HasMaxLength(50);

            entity.Property(e => e.FAX)
                .HasConversion(phoneConverter)
                .HasMaxLength(50);

            entity.Property(e => e.Email)
                .HasConversion(emailConverter)
                .HasMaxLength(100);
        });
    }

    /// <summary>
    /// Configures PostgreSQL-specific features
    /// </summary>
    private static void ConfigurePostgreSqlFeatures(ModelBuilder modelBuilder)
    {
        // Configure UUID generation
        modelBuilder.HasPostgresExtension("uuid-ossp");

        // Configure text search for German content
        modelBuilder.HasPostgresExtension("unaccent");
    }

    /// <summary>
    /// Parses address string back to Address value object
    /// </summary>
    private static Address ParseAddress(string addressString)
    {
        var parts = addressString.Split('|');
        if (parts.Length != 3)
            throw new InvalidOperationException($"Invalid address format: {addressString}");

        return new Address(parts[0], parts[1], parts[2]);
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
        // In a real application, you would get the current user from a service
        var currentUser = "system"; // This should come from ICurrentUserService

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

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = currentUser;
                    break;
            }
        }
    }
}