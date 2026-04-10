namespace BOTC.Contracts.Rooms;

public sealed record LeaveRoomResponse(
    string RoomCode,
    string PlayerId,
    bool RoomWasRemoved,
    string? NewHostPlayerId);

