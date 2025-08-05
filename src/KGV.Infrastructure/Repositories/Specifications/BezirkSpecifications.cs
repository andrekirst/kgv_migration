using KGV.Domain.Entities;
using KGV.Domain.Enums;
using KGV.Infrastructure.Repositories.Base;

namespace KGV.Infrastructure.Repositories.Specifications;

/// <summary>
/// Specification classes for Bezirk (District) queries
/// </summary>
public static class BezirkSpecifications
{
    /// <summary>
    /// Gets districts by status
    /// </summary>
    public class ByStatus : BaseSpecification<Bezirk>
    {
        public ByStatus(BezirkStatus status)
            : base(b => b.Status == status)
        {
            AddOrderBy(b => b.Name);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets active districts only
    /// </summary>
    public class ActiveOnly : BaseSpecification<Bezirk>
    {
        public ActiveOnly()
            : base(b => b.Status == BezirkStatus.Active && b.IsActive)
        {
            AddOrderBy(b => b.SortOrder);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets districts that can accept new plots
    /// </summary>
    public class CanAcceptNewPlots : BaseSpecification<Bezirk>
    {
        public CanAcceptNewPlots()
            : base(b => b.Status == BezirkStatus.Active || b.Status == BezirkStatus.UnderRestructuring)
        {
            AddOrderBy(b => b.Name);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets districts with available plots
    /// </summary>
    public class WithAvailablePlots : BaseSpecification<Bezirk>
    {
        public WithAvailablePlots()
            : base(b => b.Status == BezirkStatus.Active && 
                       b.Parzellen.Any(p => p.Status == ParzellenStatus.Available || p.Status == ParzellenStatus.Reserved))
        {
            AddInclude(b => b.Parzellen);
            AddOrderBy(b => b.Name);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Search districts by name or display name
    /// </summary>
    public class SearchByName : BaseSpecification<Bezirk>
    {
        public SearchByName(string searchTerm)
            : base(b => b.Name.Contains(searchTerm.ToUpper()) || 
                       (b.DisplayName != null && b.DisplayName.Contains(searchTerm)))
        {
            AddOrderBy(b => b.Name);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets districts by name (exact match, case-insensitive)
    /// </summary>
    public class ByName : BaseSpecification<Bezirk>
    {
        public ByName(string name)
            : base(b => b.Name == name.ToUpper())
        {
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets districts needing attention (suspended, under restructuring, etc.)
    /// </summary>
    public class NeedingAttention : BaseSpecification<Bezirk>
    {
        public NeedingAttention()
            : base(b => b.Status == BezirkStatus.Suspended || 
                       b.Status == BezirkStatus.UnderRestructuring)
        {
            AddOrderBy(b => b.UpdatedAt);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets districts with their plots included
    /// </summary>
    public class WithParzellen : BaseSpecification<Bezirk>
    {
        public WithParzellen(bool includeInactive = false)
            : base(includeInactive ? null : b => b.IsActive)
        {
            AddInclude(b => b.Parzellen);
            AddOrderBy(b => b.Name);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets districts with their cadastral districts
    /// </summary>
    public class WithKatasterbezirke : BaseSpecification<Bezirk>
    {
        public WithKatasterbezirke()
        {
            AddInclude(b => b.Katasterbezirke);
            AddInclude(b => b.BezirkeKatasterbezirke);
            AddOrderBy(b => b.Name);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets districts ordered by plot count (descending)
    /// </summary>
    public class OrderedByPlotCount : BaseSpecification<Bezirk>
    {
        public OrderedByPlotCount(int take = 10)
        {
            AddOrderByDescending(b => b.AnzahlParzellen);
            ApplyPaging(0, take);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets districts with area information
    /// </summary>
    public class WithAreaInfo : BaseSpecification<Bezirk>
    {
        public WithAreaInfo()
            : base(b => b.Flaeche.HasValue && b.Flaeche > 0)
        {
            AddOrderByDescending(b => b.Flaeche);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets districts for statistics calculation
    /// </summary>
    public class ForStatistics : BaseSpecification<Bezirk>
    {
        public ForStatistics()
        {
            AddInclude(b => b.Parzellen);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Gets districts with pagination and optional filters
    /// </summary>
    public class Paged : BaseSpecification<Bezirk>
    {
        public Paged(int page, int pageSize, BezirkStatus? statusFilter = null, string? searchTerm = null)
        {
            // Apply status filter if provided
            if (statusFilter.HasValue)
            {
                AddCriteria(b => b.Status == statusFilter.Value);
            }

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var upperSearchTerm = searchTerm.ToUpper();
                AddCriteria(b => b.Name.Contains(upperSearchTerm) || 
                               (b.DisplayName != null && b.DisplayName.Contains(searchTerm)));
            }

            AddOrderBy(b => b.Name);
            ApplyPaging((page - 1) * pageSize, pageSize);
            ApplyNoTracking();
        }
    }

    /// <summary>
    /// Checks if a district name exists (excluding specific ID)
    /// </summary>
    public class NameExists : BaseSpecification<Bezirk>
    {
        public NameExists(string name, Guid? excludeId = null)
            : base(excludeId.HasValue 
                ? b => b.Name == name.ToUpper() && b.Id != excludeId.Value
                : b => b.Name == name.ToUpper())
        {
            ApplyNoTracking();
        }
    }
}