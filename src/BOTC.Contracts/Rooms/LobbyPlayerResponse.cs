namespace BOTC.Contracts.Rooms;

public sealed record LobbyPlayerResponse(
    string PlayerId,
    string Name,
    bool IsHost,
    bool IsReady);

