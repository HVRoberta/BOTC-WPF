namespace BOTC.Presentation.Desktop.Features.Rooms.RoomLobby;

public sealed record LobbyPlayerItemViewModel(
    string PlayerId,
    string Name,
    bool IsHost,
    bool IsReady,
    bool IsCurrentUser,
    int SeatNumber)
{
    public string RoleLabel => IsHost ? "Host" : "Player";

    public string IdentityLabel => IsCurrentUser ? "You" : "Player";

    public string ReadyLabel => IsReady ? "Ready" : "Not ready";
}
