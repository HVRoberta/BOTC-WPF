using BOTC.Domain.Events;

namespace BOTC.Domain;

/// <summary>
/// Base class for aggregate roots that raise domain events.
/// Aggregates should use the protected methods to raise and clear events.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _uncommittedEvents = new();

    /// <summary>
    /// Gets all domain events raised by this aggregate that have not yet been dispatched.
    /// </summary>
    public IReadOnlyCollection<DomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Clears all uncommitted events after they have been successfully dispatched.
    /// This method should only be called after events have been persisted or otherwise handled.
    /// </summary>
    public void ClearUncommittedEvents()
    {
        _uncommittedEvents.Clear();
    }

    /// <summary>
    /// Raises a domain event by adding it to the uncommitted events collection.
    /// Protected method for use by subclasses only.
    /// </summary>
    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _uncommittedEvents.Add(domainEvent);
    }
}

