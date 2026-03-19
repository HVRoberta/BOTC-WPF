namespace BOTC.Contracts.Rooms;

public sealed record GetRoomLobbyResponse(
    string RoomCode,
    string HostDisplayName,
    RoomStatusContract Status);
