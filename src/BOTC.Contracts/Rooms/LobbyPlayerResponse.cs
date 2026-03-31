namespace BOTC.Contracts.Rooms;

public sealed record LobbyPlayerResponse(
    string PlayerId,
    string DisplayName,
    bool IsHost,
    bool IsReady);

