using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.GetRoomLobby;

public sealed record LobbyPlayerResult(
    PlayerId PlayerId,
    string Name,
    PlayerRole Role,
    bool IsReady);

public sealed record GetRoomLobbyResult(
    RoomCode RoomCode,
    IReadOnlyList<LobbyPlayerResult> Players,
    RoomStatus Status);
