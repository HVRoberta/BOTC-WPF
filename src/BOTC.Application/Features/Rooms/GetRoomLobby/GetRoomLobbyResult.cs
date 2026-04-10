using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;

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
