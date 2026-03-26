using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.JoinRoom;

public sealed record JoinRoomResult(
    RoomCode RoomCode,
    RoomPlayerId PlayerId,
    string DisplayName);

