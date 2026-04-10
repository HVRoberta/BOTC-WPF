using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.LeaveRoom;

public sealed record LeaveRoomResult(
    RoomCode RoomCode,
    PlayerId PlayerId,
    bool RoomWasRemoved,
    PlayerId? NewHostPlayerId);

