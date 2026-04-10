using BOTC.Application.Abstractions.Events;
using BOTC.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Infrastructure.Eventing;

/// <summary>
/// Dispatches domain events to all registered <see cref="IDomainEventHandler{T}"/> implementations.
/// Uses dynamic dispatch to avoid reflection machinery while staying strongly typed per handler.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task DispatchAsync(IReadOnlyCollection<DomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            await DispatchSingleAsync((dynamic)domainEvent, cancellationToken);
        }
    }

    private async Task DispatchSingleAsync<T>(T domainEvent, CancellationToken cancellationToken)
        where T : DomainEvent
    {
        foreach (var handler in _serviceProvider.GetServices<IDomainEventHandler<T>>())
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}
