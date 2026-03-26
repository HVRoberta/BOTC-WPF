namespace BOTC.Presentation.Desktop.Rooms.RoomLobby;

public sealed record LobbyPlayerItemViewModel(
    string DisplayName,
    bool IsHost)
{
    public string RoleLabel => IsHost ? "Host" : "Player";
}

