namespace BOTC.Contracts.Rooms;

public sealed record GetRoomLobbyResponse(
    string RoomCode,
    IReadOnlyCollection<LobbyPlayerResponse> Players,
    RoomStatusContract Status);
