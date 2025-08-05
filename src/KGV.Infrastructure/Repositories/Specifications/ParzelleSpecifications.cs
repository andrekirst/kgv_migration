using KGV.Domain.Entities;
using KGV.Domain.Enums;
using KGV.Infrastructure.Repositories.Base;

namespace KGV.Infrastructure.Repositories.Specifications;

/// <summary>
/// Specification classes for Parzelle (Plot) queries
/// </summary>
public static class ParzelleSpecifications
{
    /// <summary>
    /// Gets plots by district ID
    /// </summary>
    public class ByBezirk : BaseSpecification<Parzelle>
    {
        public ByBezirk(Guid bezirkId)
            : base(p => p.BezirkId == bezirkId)
        {
            AddInclude(p => p.Bezirk);
            AddOrderBy(p => p.Nummer);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets plots by status
    /// </summary>
    public class ByStatus : BaseSpecification<Parzelle>
    {
        public ByStatus(ParzellenStatus status)
            : base(p => p.Status == status)
        {
            AddInclude(p => p.Bezirk);
            AddOrderBy(p => p.Bezirk.Name);
            AddOrderBy(p => p.Nummer);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets available plots (Available or optionally Reserved)
    /// </summary>
    public class Available : BaseSpecification<Parzelle>
    {
        public Available(bool includeReserved = false)
            : base(includeReserved 
                ? p => p.Status == ParzellenStatus.Available || p.Status == ParzellenStatus.Reserved
                : p => p.Status == ParzellenStatus.Available)
        {
            AddInclude(p => p.Bezirk);
            AddOrderBy(p => p.Prioritaet);
            AddOrderBy(p => p.Bezirk.Name);
            AddOrderBy(p => p.Nummer);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets plots by district and plot number
    /// </summary>
    public class ByBezirkAndNummer : BaseSpecification<Parzelle>
    {
        public ByBezirkAndNummer(Guid bezirkId, string nummer)
            : base(p => p.BezirkId == bezirkId && p.Nummer == nummer.ToUpper())
        {
            AddInclude(p => p.Bezirk);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets plots by utility requirements
    /// </summary>
    public class ByUtilities : BaseSpecification<Parzelle>
    {
        public ByUtilities(bool hasWasser, bool hasStrom)
            : base(p => p.HasWasser == hasWasser && p.HasStrom == hasStrom)
        {
            AddInclude(p => p.Bezirk);
            AddOrderBy(p => p.Bezirk.Name);
            AddOrderBy(p => p.Nummer);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets plots within area range
    /// </summary>
    public class ByAreaRange : BaseSpecification<Parzelle>
    {
        public ByAreaRange(decimal minArea, decimal maxArea)
            : base(p => p.Flaeche >= minArea && p.Flaeche <= maxArea)
        {
            AddInclude(p => p.Bezirk);
            AddOrderBy(p => p.Flaeche);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets plots within price range
    /// </summary>
    public class ByPriceRange : BaseSpecification<Parzelle>
    {
        public ByPriceRange(decimal minPrice, decimal maxPrice)
            : base(p => p.Preis.HasValue && p.Preis >= minPrice && p.Preis <= maxPrice)
        {
            AddInclude(p => p.Bezirk);
            AddOrderBy(p => p.Preis);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets recently assigned plots
    /// </summary>
    public class RecentlyAssigned : BaseSpecification<Parzelle>
    {
        public RecentlyAssigned(int days = 30)
            : base(p => p.Status == ParzellenStatus.Assigned && 
                       p.VergebenAm.HasValue && 
                       p.VergebenAm >= DateTime.UtcNow.AddDays(-days))
        {
            AddInclude(p => p.Bezirk);
            AddOrderByDescending(p => p.VergebenAm);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets plots needing attention (old updates)
    /// </summary>
    public class NeedingAttention : BaseSpecification<Parzelle>
    {
        public NeedingAttention(int daysSinceLastUpdate = 365)
            : base(p => p.UpdatedAt < DateTime.UtcNow.AddDays(-daysSinceLastUpdate) ||
                       (p.Status == ParzellenStatus.UnderDevelopment && 
                        p.UpdatedAt < DateTime.UtcNow.AddDays(-90)))
        {
            AddInclude(p => p.Bezirk);
            AddOrderBy(p => p.UpdatedAt);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets plots for statistics calculation
    /// </summary>
    public class ForStatistics : BaseSpecification<Parzelle>
    {
        public ForStatistics(Guid? bezirkId = null)
            : base(bezirkId.HasValue ? p => p.BezirkId == bezirkId.Value : null)
        {
            AddInclude(p => p.Bezirk);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Complex filtering specification for plots
    /// </summary>
    public class ComplexFilter : BaseSpecification<Parzelle>
    {
        public ComplexFilter(
            Guid? bezirkId = null,
            ParzellenStatus? status = null,
            bool? hasWasser = null,
            bool? hasStrom = null,
            decimal? minFlaeche = null,
            decimal? maxFlaeche = null,
            decimal? minPreis = null,
            decimal? maxPreis = null,
            string? searchTerm = null)
        {
            // Build the criteria based on provided filters
            if (bezirkId.HasValue)
                AddCriteria(p => p.BezirkId == bezirkId.Value);

            if (status.HasValue)
                AddCriteria(p => p.Status == status.Value);

            if (hasWasser.HasValue)
                AddCriteria(p => p.HasWasser == hasWasser.Value);

            if (hasStrom.HasValue)
                AddCriteria(p => p.HasStrom == hasStrom.Value);

            if (minFlaeche.HasValue)
                AddCriteria(p => p.Flaeche >= minFlaeche.Value);

            if (maxFlaeche.HasValue)
                AddCriteria(p => p.Flaeche <= maxFlaeche.Value);

            if (minPreis.HasValue)
                AddCriteria(p => p.Preis.HasValue && p.Preis >= minPreis.Value);

            if (maxPreis.HasValue)
                AddCriteria(p => p.Preis.HasValue && p.Preis <= maxPreis.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var upperSearchTerm = searchTerm.ToUpper();
                AddCriteria(p => p.Nummer.Contains(upperSearchTerm) ||
                               p.Bezirk.Name.Contains(upperSearchTerm));
            }

            AddInclude(p => p.Bezirk);
            AddOrderBy(p => p.Bezirk.Name);
            AddOrderBy(p => p.Nummer);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets plots with pagination
    /// </summary>
    public class Paged : BaseSpecification<Parzelle>
    {
        public Paged(int page, int pageSize, Guid? bezirkId = null, ParzellenStatus? status = null)
        {
            if (bezirkId.HasValue)
                AddCriteria(p => p.BezirkId == bezirkId.Value);

            if (status.HasValue)
                AddCriteria(p => p.Status == status.Value);

            AddInclude(p => p.Bezirk);
            AddOrderBy(p => p.Bezirk.Name);
            AddOrderBy(p => p.Nummer);
            ApplyPaging((page - 1) * pageSize, pageSize);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Checks if a plot number exists in a district (excluding specific ID)
    /// </summary>
    public class NumberExistsInDistrict : BaseSpecification<Parzelle>
    {
        public NumberExistsInDistrict(Guid bezirkId, string nummer, Guid? excludeId = null)
            : base(excludeId.HasValue 
                ? p => p.BezirkId == bezirkId && p.Nummer == nummer.ToUpper() && p.Id != excludeId.Value
                : p => p.BezirkId == bezirkId && p.Nummer == nummer.ToUpper())
        {
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets the highest plot number in a district for generating next number
    /// </summary>
    public class HighestNumberInDistrict : BaseSpecification<Parzelle>
    {
        public HighestNumberInDistrict(Guid bezirkId)
            : base(p => p.BezirkId == bezirkId)
        {
            AddOrderByDescending(p => p.Nummer);
            ApplyPaging(0, 1);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets plots by full number format (BezirkName-PlotNumber)
    /// </summary>
    public class ByFullNumber : BaseSpecification<Parzelle>
    {
        public ByFullNumber(string fullNumber)
        {
            var parts = fullNumber.Split('-');
            if (parts.Length == 2)
            {
                var bezirkName = parts[0].ToUpper();
                var plotNumber = parts[1].ToUpper();
                AddCriteria(p => p.Bezirk.Name == bezirkName && p.Nummer == plotNumber);
            }
            else
            {
                // Invalid format, return no results
                AddCriteria(p => false);
            }

            AddInclude(p => p.Bezirk);
            ApplyNoTracking();
        }
    }
}