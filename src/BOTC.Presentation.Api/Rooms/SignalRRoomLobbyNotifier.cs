using BOTC.Contracts.Rooms;
using Microsoft.AspNetCore.SignalR;

namespace BOTC.Presentation.Api.Rooms;

public sealed class SignalRRoomLobbyNotifier(IHubContext<RoomLobbyHub> hubContext) : IRoomLobbyNotifier
{
    public Task NotifyLobbyUpdatedAsync(string roomCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);

        var normalizedRoomCode = RoomLobbyGroups.NormalizeRoomCode(roomCode);
        var groupName = RoomLobbyGroups.ForRoom(normalizedRoomCode);

        return hubContext.Clients
            .Group(groupName)
            .SendCoreAsync(RoomLobbyHubContract.LobbyUpdatedEvent, [normalizedRoomCode], cancellationToken);
    }
}
