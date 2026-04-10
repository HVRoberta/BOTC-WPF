using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;

namespace BOTC.Application.Features.Rooms.CreateRoom;

public sealed record CreateRoomResult(
    RoomId RoomId,
    RoomCode RoomCode,
    string RoomName,
    RoomStatus Status,
    PlayerId HostPlayerId,
    DateTime CreatedAtUtc);
