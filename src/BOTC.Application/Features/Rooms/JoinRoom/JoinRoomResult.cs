using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.JoinRoom;

public sealed record JoinRoomResult(
    RoomCode RoomCode,
    PlayerId PlayerId,
    string Name);

