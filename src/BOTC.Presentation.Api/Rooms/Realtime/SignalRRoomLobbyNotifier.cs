using BOTC.Application.Abstractions.Realtime;
using BOTC.Contracts.Rooms;
using Microsoft.AspNetCore.SignalR;

namespace BOTC.Presentation.Api.Rooms.Realtime;

/// <summary>
/// Sends SignalR group messages to lobby clients when room state changes.
/// Implements the Application-layer abstraction directly so no extra adapter layer is needed.
/// </summary>
public sealed class SignalRRoomLobbyNotifier(IHubContext<RoomLobbyHub> hubContext) : IRoomLobbyNotifier
{
    public Task NotifyLobbyUpdatedAsync(string roomCode, CancellationToken cancellationToken)
        => SendAsync(roomCode, RoomLobbyHubContract.LobbyUpdatedEvent, cancellationToken);

    public Task NotifyLobbyClosedAsync(string roomCode, CancellationToken cancellationToken)
        => SendAsync(roomCode, RoomLobbyHubContract.LobbyClosedEvent, cancellationToken);

    private Task SendAsync(string roomCode, string eventName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);

        var normalizedCode = RoomLobbyGroups.NormalizeRoomCode(roomCode);
        var groupName = RoomLobbyGroups.ForRoom(normalizedCode);

        return hubContext.Clients
            .Group(groupName)
            .SendCoreAsync(eventName, [normalizedCode], cancellationToken);
    }
}
