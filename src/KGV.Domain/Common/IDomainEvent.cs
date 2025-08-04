using MediatR;

namespace KGV.Domain.Common;

/// <summary>
/// Marker interface for domain events
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// When the event occurred
    /// </summary>
    DateTime OccurredOn { get; }
}