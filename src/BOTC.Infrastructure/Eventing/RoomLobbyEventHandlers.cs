using BOTC.Application.Abstractions.Events;
using BOTC.Application.Abstractions.Realtime;
using BOTC.Domain.Rooms.Events;

namespace BOTC.Infrastructure.Eventing;

/// <summary>
/// Handles all room-related domain events by pushing lobby state updates to connected clients via SignalR.
/// Four of the five room events result in a lobby snapshot push; a game start or room deletion closes the lobby.
/// </summary>

internal sealed class RoomCreatedEventHandler(IRoomLobbyNotifier notifier)
    : IDomainEventHandler<RoomCreatedDomainEvent>
{
    public Task HandleAsync(RoomCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
        => notifier.NotifyLobbyUpdatedAsync(domainEvent.RoomCode.Value, cancellationToken);
}

internal sealed class PlayerJoinedRoomEventHandler(IRoomLobbyNotifier notifier)
    : IDomainEventHandler<PlayerJoinedRoomDomainEvent>
{
    public Task HandleAsync(PlayerJoinedRoomDomainEvent domainEvent, CancellationToken cancellationToken)
        => notifier.NotifyLobbyUpdatedAsync(domainEvent.RoomCode.Value, cancellationToken);
}

internal sealed class PlayerReadyStateChangedEventHandler(IRoomLobbyNotifier notifier)
    : IDomainEventHandler<PlayerReadyStateChangedDomainEvent>
{
    public Task HandleAsync(PlayerReadyStateChangedDomainEvent domainEvent, CancellationToken cancellationToken)
        => notifier.NotifyLobbyUpdatedAsync(domainEvent.RoomCode.Value, cancellationToken);
}

/// <summary>
/// Sends LobbyClosed when the last player left (room deleted), LobbyUpdated otherwise.
/// </summary>
internal sealed class PlayerLeftRoomEventHandler(IRoomLobbyNotifier notifier)
    : IDomainEventHandler<PlayerLeftRoomDomainEvent>
{
    public Task HandleAsync(PlayerLeftRoomDomainEvent domainEvent, CancellationToken cancellationToken)
        => domainEvent.IsRoomDeleted
            ? notifier.NotifyLobbyClosedAsync(domainEvent.RoomCode.Value, cancellationToken)
            : notifier.NotifyLobbyUpdatedAsync(domainEvent.RoomCode.Value, cancellationToken);
}

/// <summary>
/// Closes the lobby for all clients when the game starts.
/// </summary>
internal sealed class GameStartedEventHandler(IRoomLobbyNotifier notifier)
    : IDomainEventHandler<GameStartedDomainEvent>
{
    public Task HandleAsync(GameStartedDomainEvent domainEvent, CancellationToken cancellationToken)
        => notifier.NotifyLobbyClosedAsync(domainEvent.RoomCode.Value, cancellationToken);
}

