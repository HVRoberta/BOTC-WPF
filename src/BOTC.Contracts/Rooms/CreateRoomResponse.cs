namespace BOTC.Contracts.Rooms;

public sealed record CreateRoomResponse(
    string RoomId,
    string RoomCode,
    DateTime CreatedAtUtc);

