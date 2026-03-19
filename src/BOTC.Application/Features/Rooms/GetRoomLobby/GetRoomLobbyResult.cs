using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.GetRoomLobby;

public sealed record GetRoomLobbyResult(
    RoomCode RoomCode,
    string HostDisplayName,
    RoomStatus Status);

