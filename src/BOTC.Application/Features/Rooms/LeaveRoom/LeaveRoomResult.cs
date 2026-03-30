using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.LeaveRoom;

public sealed record LeaveRoomResult(
    RoomCode RoomCode,
    RoomPlayerId PlayerId,
    bool RoomWasRemoved,
    RoomPlayerId? NewHostPlayerId);

