using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.GetRoomLobby;

public sealed record LobbyPlayerResult(
    RoomPlayerId PlayerId,
    string DisplayName,
    RoomPlayerRole Role,
    bool IsReady);

public sealed record GetRoomLobbyResult(
    RoomCode RoomCode,
    IReadOnlyList<LobbyPlayerResult> Players,
    RoomStatus Status);
