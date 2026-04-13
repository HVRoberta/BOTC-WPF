using BOTC.Application.Abstractions.Events;
using BOTC.Domain.Users.Events;

namespace BOTC.Infrastructure.Eventing;

/// <summary>
/// Handles the UserCreatedDomainEvent.
/// No realtime notification is required at this stage; this handler serves as an extension point.
/// </summary>
internal sealed class UserCreatedEventHandler : IDomainEventHandler<UserCreatedDomainEvent>
{
    public Task HandleAsync(UserCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

