using BOTC.Domain.Events;

namespace BOTC.Application.Abstractions.Events;

/// <summary>
/// Abstraction for a handler that reacts to a specific domain event type.
/// </summary>
/// <typeparam name="TDomainEvent">The type of domain event this handler processes.</typeparam>
public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : DomainEvent
{
    /// <summary>
    /// Handles the domain event.
    /// </summary>
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}

