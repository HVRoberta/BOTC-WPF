using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.CreateRoom;

public sealed record CreateRoomResult(
    RoomId RoomId,
    RoomCode RoomCode,
    string RoomName,
    RoomStatus Status,
    PlayerId HostPlayerId,
    DateTime CreatedAtUtc);
