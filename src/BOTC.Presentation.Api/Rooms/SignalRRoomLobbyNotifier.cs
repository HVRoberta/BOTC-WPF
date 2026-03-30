﻿using BOTC.Contracts.Rooms;
using Microsoft.AspNetCore.SignalR;

namespace BOTC.Presentation.Api.Rooms;

public sealed class SignalRRoomLobbyNotifier(IHubContext<RoomLobbyHub> hubContext) : IRoomLobbyNotifier
{
    public Task NotifyLobbyUpdatedAsync(string roomCode, CancellationToken cancellationToken)
    {
        return NotifyAsync(roomCode, RoomLobbyHubContract.LobbyUpdatedEvent, cancellationToken);
    }

    public Task NotifyLobbyClosedAsync(string roomCode, CancellationToken cancellationToken)
    {
        return NotifyAsync(roomCode, RoomLobbyHubContract.LobbyClosedEvent, cancellationToken);
    }

    private Task NotifyAsync(string roomCode, string eventName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        var normalizedRoomCode = RoomLobbyGroups.NormalizeRoomCode(roomCode);
        var groupName = RoomLobbyGroups.ForRoom(normalizedRoomCode);

        return hubContext.Clients
            .Group(groupName)
            .SendCoreAsync(eventName, [normalizedRoomCode], cancellationToken);
    }
}
