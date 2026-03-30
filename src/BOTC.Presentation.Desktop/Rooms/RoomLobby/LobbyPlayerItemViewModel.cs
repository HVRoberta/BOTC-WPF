namespace BOTC.Presentation.Desktop.Rooms.RoomLobby;

public sealed record LobbyPlayerItemViewModel(
    string PlayerId,
    string DisplayName,
    bool IsHost,
    bool IsCurrentUser,
    int SeatNumber)
{
    public string RoleLabel => IsHost ? "Host" : "Player";

    public string IdentityLabel => IsCurrentUser ? "You" : "Player";
}

