using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.CreateRoom;

public sealed record CreateRoomResult(
    RoomId RoomId,
    RoomCode RoomCode,
    RoomPlayerId HostPlayerId,
    string HostDisplayName,
    RoomStatus Status,
    DateTime CreatedAtUtc);
