using BOTC.Domain.Events;

namespace BOTC.Application.Abstractions.Events;

/// <summary>
/// Abstraction for dispatching domain events to their registered handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all domain events to their registered handlers.
    /// Handlers are discovered and invoked based on the concrete event type.
    /// </summary>
    Task DispatchAsync(IReadOnlyCollection<DomainEvent> domainEvents, CancellationToken cancellationToken);
}

