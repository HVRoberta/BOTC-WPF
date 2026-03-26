using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.GetRoomLobby;

public sealed record LobbyPlayerResult(
    RoomPlayerId PlayerId,
    string DisplayName,
    RoomPlayerRole Role);

public sealed record GetRoomLobbyResult(
    RoomCode RoomCode,
    IReadOnlyCollection<LobbyPlayerResult> Players,
    RoomStatus Status);
