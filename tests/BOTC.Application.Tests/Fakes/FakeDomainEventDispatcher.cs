using BOTC.Application.Abstractions.Events;
using BOTC.Domain.Events;

namespace BOTC.Application.Tests.Fakes;

/// <summary>
/// Fake implementation of IDomainEventDispatcher for unit testing.
/// This implementation does not dispatch events; it only records that dispatch was called.
/// </summary>
public sealed class FakeDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly List<DomainEvent> _dispatchedEvents = [];

    public IReadOnlyCollection<DomainEvent> DispatchedEvents => _dispatchedEvents.AsReadOnly();

    public Task DispatchAsync(IReadOnlyCollection<DomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        
        _dispatchedEvents.AddRange(domainEvents);
        return Task.CompletedTask;
    }
}

