using BOTC.Contracts.Rooms;
using Microsoft.AspNetCore.SignalR;

namespace BOTC.Presentation.Api.Rooms;

public sealed class RoomLobbyHub : Hub
{
    public Task JoinLobbyGroup(string roomCode)
    {
        var groupName = RoomLobbyGroups.ForRoom(roomCode);
        return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public Task LeaveLobbyGroup(string roomCode)
    {
        var groupName = RoomLobbyGroups.ForRoom(roomCode);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}

