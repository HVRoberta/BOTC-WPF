namespace BOTC.Contracts.Rooms;

public static class RoomLobbyHubContract
{
    public const string HubRoute = "/hubs/room-lobby";
    public const string JoinLobbyGroupMethod = "JoinLobbyGroup";
    public const string LeaveLobbyGroupMethod = "LeaveLobbyGroup";
    public const string LobbyUpdatedEvent = "LobbyUpdated";
    public const string LobbyClosedEvent = "LobbyClosed";
}
