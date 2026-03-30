namespace BOTC.Contracts.Rooms;

public sealed record CreateRoomResponse(
    string RoomId,
    string RoomCode,
    string PlayerId,
    DateTime CreatedAtUtc);
