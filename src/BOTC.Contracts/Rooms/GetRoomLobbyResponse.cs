namespace BOTC.Contracts.Rooms;

public sealed record GetRoomLobbyResponse(
    string RoomCode,
    IReadOnlyList<LobbyPlayerResponse> Players,
    RoomStatusContract Status);
